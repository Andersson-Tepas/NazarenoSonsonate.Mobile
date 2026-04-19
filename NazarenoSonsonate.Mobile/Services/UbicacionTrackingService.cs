using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;

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

        public Task StartAsync(int recorridoId, string tipoUnidad)
        {
            if (IsTracking)
            {
                if (RecorridoId == recorridoId && TipoUnidad == tipoUnidad)
                    return Task.CompletedTask;

                return Task.CompletedTask;
            }

            RecorridoId = recorridoId;
            TipoUnidad = tipoUnidad;
            Error = null;
            Estado = "INICIANDO...";
            IsTracking = true;
            _ultimaUbicacionEnviada = null;

            NotifyStateChanged();

            _cts = new CancellationTokenSource();
            _trackingTask = RunTrackingAsync(_cts.Token);

            return Task.CompletedTask;
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
                        permiso = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

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

                if (Estado != "ERROR")
                    Estado = "DETENIDO";

                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}