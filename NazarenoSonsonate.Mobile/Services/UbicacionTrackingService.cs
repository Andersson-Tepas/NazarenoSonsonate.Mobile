using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Mobile.Services
{
    public class UbicacionTrackingService
    {
        private readonly UbicacionService _ubicacionService;

        private CancellationTokenSource? _cts;
        private Task? _trackingTask;

        public bool IsTracking { get; private set; }
        public int RecorridoId { get; private set; }
        public string TipoUnidad { get; private set; } = string.Empty;
        public DateTime? UltimaHoraEnvio { get; private set; }
        public string Estado { get; private set; } = "DETENIDO";
        public string? Error { get; private set; }

        public event Action? OnStateChanged;

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
                    Estado = "ENVIANDO UBICACIÓN...";
                    NotifyStateChanged();

                    var nombreUnidad = TipoUnidad == "VirgenMaria"
                        ? "Anda Virgen María"
                        : "Anda Jesús Nazareno";

                    var enviado = await _ubicacionService.EnviarUbicacionActualAsync(
                        RecorridoId,
                        TipoUnidad,
                        nombreUnidad,
                        $"Ubicación en vivo de {nombreUnidad}");

                    if (enviado)
                    {
                        UltimaHoraEnvio = DateTime.Now;
                        Estado = "RASTREO ACTIVO";
                        Error = null;
                    }
                    else
                    {
                        Estado = "SIN PERMISO O SIN GPS";
                        Error = "No se pudo obtener o enviar la ubicación.";
                    }

                    NotifyStateChanged();

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
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
