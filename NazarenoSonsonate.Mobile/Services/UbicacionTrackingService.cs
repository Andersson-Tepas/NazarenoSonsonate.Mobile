using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;

using Microsoft.Maui.Devices.Sensors;

#if ANDROID
using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using NazarenoSonsonate.Mobile.Platforms.Android.Services;
#endif

namespace NazarenoSonsonate.Mobile.Services
{
    public class UbicacionTrackingService
    {
        private readonly UbicacionService _ubicacionService;

        private CancellationTokenSource? _cts;
        private Task? _trackingTask;
        private Location? _ultimaUbicacionEnviada;

        public bool IsTracking { get; private set; }
        public int RecorridoId { get; private set; }
        public string TipoUnidad { get; private set; } = string.Empty;
        public DateTime? UltimaHoraEnvio { get; private set; }
        public string Estado { get; private set; } = "DETENIDO";
        public string? Error { get; private set; }
        public double? UltimaPrecisionMetros { get; private set; }

        public event Action? OnStateChanged;

        private const double DistanciaMinimaMetros = 8.0;
        private const double PrecisionMaximaAceptableMetros = 25.0;
        private const int ReintentosGpsPorCiclo = 3;

        private static readonly TimeSpan IntervaloLectura = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TiempoMaximoSinEnviar = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan TimeoutGpsPorIntento = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan EsperaEntreReintentosGps = TimeSpan.FromSeconds(2);

        public UbicacionTrackingService(UbicacionService ubicacionService)
        {
            _ubicacionService = ubicacionService;
        }

        public async Task StartAsync(int recorridoId, string tipoUnidad)
        {
            if (IsTracking)
            {
                if (RecorridoId == recorridoId && TipoUnidad == tipoUnidad)
                    return;

                return;
            }

            Estado = "SOLICITANDO PERMISOS...";
            Error = null;
            UltimaPrecisionMetros = null;
            NotifyStateChanged();

            var permiso = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (permiso != PermissionStatus.Granted)
                permiso = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (permiso != PermissionStatus.Granted)
            {
                Estado = "SIN PERMISO DE UBICACIÓN";
                Error = "Debes conceder permiso de ubicación antes de iniciar el rastreo.";
                IsTracking = false;
                NotifyStateChanged();
                return;
            }

            RecorridoId = recorridoId;
            TipoUnidad = tipoUnidad;
            Error = null;
            Estado = "INICIANDO...";
            IsTracking = true;
            _ultimaUbicacionEnviada = null;
            UltimaHoraEnvio = null;
            UltimaPrecisionMetros = null;

            StartForegroundService();
            NotifyStateChanged();

            _cts = new CancellationTokenSource();
            _trackingTask = RunTrackingAsync(_cts.Token);

            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_cts is not null)
            {
                try
                {
                    _cts.Cancel();
                }
                catch
                {
                }
            }

            if (_trackingTask is not null)
            {
                try
                {
                    await _trackingTask;
                }
                catch
                {
                }
            }

            _cts?.Dispose();
            _cts = null;
            _trackingTask = null;
            _ultimaUbicacionEnviada = null;
            UltimaPrecisionMetros = null;

            StopForegroundService();

            IsTracking = false;
            Estado = "DETENIDO";
            Error = null;
            NotifyStateChanged();
        }

        private async Task RunTrackingAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Estado = "LEYENDO GPS...";
                    Error = null;
                    NotifyStateChanged();

                    var permiso = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                    if (permiso != PermissionStatus.Granted)
                    {
                        Estado = "SIN PERMISO DE UBICACIÓN";
                        Error = "No se concedió permiso de ubicación.";
                        NotifyStateChanged();

                        await Task.Delay(IntervaloLectura, cancellationToken);
                        continue;
                    }

                    var location = await ObtenerUbicacionConMejorPrecisionAsync(cancellationToken);

                    if (location is null)
                    {
                        Estado = "ESPERANDO MEJOR PRECISIÓN GPS";
                        Error = "Aún no se obtuvo una ubicación confiable. Mantén el teléfono con vista al cielo o cerca de una ventana.";
                        NotifyStateChanged();

                        await Task.Delay(IntervaloLectura, cancellationToken);
                        continue;
                    }

                    var debeEnviarPorTiempo =
                        !UltimaHoraEnvio.HasValue ||
                        DateTime.Now - UltimaHoraEnvio.Value >= TiempoMaximoSinEnviar;

                    var debeEnviarPorDistancia = false;

                    if (_ultimaUbicacionEnviada is null)
                    {
                        debeEnviarPorDistancia = true;
                    }
                    else
                    {
                        var distancia = Location.CalculateDistance(
                            _ultimaUbicacionEnviada,
                            location,
                            DistanceUnits.Kilometers) * 1000.0;

                        debeEnviarPorDistancia = distancia >= DistanciaMinimaMetros;
                    }

                    if (!debeEnviarPorDistancia && !debeEnviarPorTiempo)
                    {
                        Estado = "ESPERANDO MOVIMIENTO...";
                        Error = UltimaPrecisionMetros.HasValue
                            ? $"GPS correcto. Precisión actual: {UltimaPrecisionMetros.Value:0} m."
                            : null;

                        NotifyStateChanged();

                        await Task.Delay(IntervaloLectura, cancellationToken);
                        continue;
                    }

                    Estado = "ENVIANDO UBICACIÓN...";
                    Error = UltimaPrecisionMetros.HasValue
                        ? $"Precisión GPS: {UltimaPrecisionMetros.Value:0} m."
                        : null;

                    NotifyStateChanged();

                    var nombreUnidad = TipoUnidad == "VirgenMaria"
                        ? "Anda Virgen María"
                        : "Anda Jesús Nazareno";

                    var enviado = await _ubicacionService.EnviarUbicacionAsync(
                        RecorridoId,
                        TipoUnidad,
                        location,
                        nombreUnidad,
                        $"Ubicación en vivo de {nombreUnidad}");

                    if (enviado)
                    {
                        _ultimaUbicacionEnviada = location;
                        UltimaHoraEnvio = DateTime.Now;
                        UltimaPrecisionMetros = location.Accuracy;

                        Estado = "RASTREO ACTIVO";
                        Error = UltimaPrecisionMetros.HasValue
                            ? $"Última precisión enviada: {UltimaPrecisionMetros.Value:0} m."
                            : null;
                    }
                    else
                    {
                        Estado = "ERROR DE ENVÍO";
                        Error = "No se pudo enviar la ubicación. Puede ser por conexión o precisión GPS insuficiente.";
                    }

                    NotifyStateChanged();
                    await Task.Delay(IntervaloLectura, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Estado = "ERROR";
                Error = ex.Message;
                NotifyStateChanged();
            }
            finally
            {
                IsTracking = false;
                _ultimaUbicacionEnviada = null;

                StopForegroundService();

                if (Estado != "ERROR")
                    Estado = "DETENIDO";

                NotifyStateChanged();
            }
        }

        private async Task<Location?> ObtenerUbicacionConMejorPrecisionAsync(CancellationToken cancellationToken)
        {
            Location? mejorUbicacion = null;

            for (var intento = 1; intento <= ReintentosGpsPorCiclo; intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Estado = intento == 1
                    ? "LEYENDO GPS..."
                    : $"MEJORANDO PRECISIÓN GPS ({intento}/{ReintentosGpsPorCiclo})...";

                Error = mejorUbicacion?.Accuracy is not null
                    ? $"Precisión actual: {mejorUbicacion.Accuracy.Value:0} m. Esperando máximo {PrecisionMaximaAceptableMetros:0} m."
                    : "Buscando señal GPS precisa...";

                NotifyStateChanged();

                Location? location = null;

                try
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Best,
                        TimeoutGpsPorIntento);

                    location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);
                }
                catch
                {
                    location = null;
                }

                if (location is not null)
                {
                    UltimaPrecisionMetros = location.Accuracy;

                    if (EsMejorUbicacion(location, mejorUbicacion))
                        mejorUbicacion = location;

                    if (EsUbicacionConfiable(location))
                        return location;
                }

                if (intento < ReintentosGpsPorCiclo)
                    await Task.Delay(EsperaEntreReintentosGps, cancellationToken);
            }

            UltimaPrecisionMetros = mejorUbicacion?.Accuracy;

            if (mejorUbicacion is not null && EsUbicacionConfiable(mejorUbicacion))
                return mejorUbicacion;

            return null;
        }

        private static bool EsMejorUbicacion(Location nueva, Location? actual)
        {
            if (actual is null)
                return true;

            if (!nueva.Accuracy.HasValue)
                return false;

            if (!actual.Accuracy.HasValue)
                return true;

            return nueva.Accuracy.Value < actual.Accuracy.Value;
        }

        private static bool EsUbicacionConfiable(Location location)
        {
            if (double.IsNaN(location.Latitude) || double.IsNaN(location.Longitude))
                return false;

            if (double.IsInfinity(location.Latitude) || double.IsInfinity(location.Longitude))
                return false;

            if (location.Latitude == 0 && location.Longitude == 0)
                return false;

            if (location.Latitude < -90 || location.Latitude > 90)
                return false;

            if (location.Longitude < -180 || location.Longitude > 180)
                return false;

            if (!location.Accuracy.HasValue)
                return false;

            if (location.Accuracy.Value > PrecisionMaximaAceptableMetros)
                return false;

            return true;
        }

#if ANDROID
        private void StartForegroundService()
        {
            try
            {
                var context = Platform.AppContext;
                var intent = new Intent(context, typeof(LocationForegroundService));
                intent.PutExtra("tipoUnidad", TipoUnidad == "VirgenMaria" ? "Virgen María" : "Jesús Nazareno");

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    context.StartForegroundService(intent);
                else
                    context.StartService(intent);
            }
            catch (Exception ex)
            {
                Error = $"No se pudo iniciar el servicio en segundo plano: {ex.Message}";
                Estado = "ERROR";
                NotifyStateChanged();
            }
        }

        private void StopForegroundService()
        {
            try
            {
                var context = Platform.AppContext;
                var intent = new Intent(context, typeof(LocationForegroundService));
                context.StopService(intent);
            }
            catch
            {
            }
        }
#else
        private void StartForegroundService() { }
        private void StopForegroundService() { }
#endif

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}