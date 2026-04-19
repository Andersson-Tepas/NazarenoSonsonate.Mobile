using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public event Action? OnStateChanged;

        private const double DistanciaMinimaMetros = 8.0;
        private static readonly TimeSpan IntervaloLectura = TimeSpan.FromSeconds(5);

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

                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Best,
                        TimeSpan.FromSeconds(10));

                    var location = await Geolocation.Default.GetLocationAsync(request);

                    if (location is null)
                    {
                        Estado = "SIN SEÑAL GPS";
                        Error = "No se pudo obtener la ubicación actual.";
                        NotifyStateChanged();

                        await Task.Delay(IntervaloLectura, cancellationToken);
                        continue;
                    }

                    if (_ultimaUbicacionEnviada is not null)
                    {
                        var distancia = Location.CalculateDistance(
                            _ultimaUbicacionEnviada,
                            location,
                            DistanceUnits.Kilometers) * 1000.0;

                        if (distancia < DistanciaMinimaMetros)
                        {
                            Estado = "ESPERANDO MOVIMIENTO...";
                            Error = null;
                            NotifyStateChanged();

                            await Task.Delay(IntervaloLectura, cancellationToken);
                            continue;
                        }
                    }

                    Estado = "ENVIANDO UBICACIÓN...";
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
                        Estado = "RASTREO ACTIVO";
                        Error = null;
                    }
                    else
                    {
                        Estado = "ERROR DE ENVÍO";
                        Error = "No se pudo enviar la ubicación al servidor.";
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