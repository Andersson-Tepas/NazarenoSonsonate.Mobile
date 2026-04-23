using Microsoft.AspNetCore.SignalR.Client;
using NazarenoSonsonate.Shared.DTOs;

namespace NazarenoSonsonate.Mobile.Services
{
    public class SignalRService
    {
        private HubConnection? _hubConnection;

        public event Action<UbicacionProcesionDto>? UbicacionRecibida;
        public event Action<string>? EstadoConexionCambiado;

        public bool EstaConectado =>
            _hubConnection?.State == HubConnectionState.Connected;

        public async Task IniciarAsync(string baseUrl)
        {
            try
            {
                if (_hubConnection is not null)
                {
                    if (_hubConnection.State == HubConnectionState.Connected ||
                        _hubConnection.State == HubConnectionState.Connecting ||
                        _hubConnection.State == HubConnectionState.Reconnecting)
                    {
                        return;
                    }

                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{baseUrl}hubs/procesion")
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.Reconnecting += _ =>
                {
                    EstadoConexionCambiado?.Invoke("RECONECTANDO");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += _ =>
                {
                    EstadoConexionCambiado?.Invoke("CONECTADO");
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += _ =>
                {
                    EstadoConexionCambiado?.Invoke("DESCONECTADO");
                    return Task.CompletedTask;
                };

                _hubConnection.On<UbicacionProcesionDto>("RecibirUbicacion", ubicacion =>
                {
                    UbicacionRecibida?.Invoke(ubicacion);
                });

                await _hubConnection.StartAsync();
                EstadoConexionCambiado?.Invoke("CONECTADO");
            }
            catch (Exception ex)
            {
                EstadoConexionCambiado?.Invoke("ERROR");
                Console.WriteLine($"Error SignalR: {ex.Message}");
            }
        }

        public async Task DetenerAsync()
        {
            if (_hubConnection is null)
                return;

            try
            {
                await _hubConnection.DisposeAsync();
            }
            finally
            {
                _hubConnection = null;
                EstadoConexionCambiado?.Invoke("DESCONECTADO");
            }
        }
    }
}