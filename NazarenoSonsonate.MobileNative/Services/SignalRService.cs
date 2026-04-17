using Microsoft.AspNetCore.SignalR.Client;
using NazarenoSonsonate.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.MobileNative.Services
{
    public class SignalRService
    {
        private HubConnection? _hubConnection;

        public event Action<UbicacionProcesionDto>? UbicacionRecibida;

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

                _hubConnection.On<UbicacionProcesionDto>("RecibirUbicacion", ubicacion =>
                {
                    UbicacionRecibida?.Invoke(ubicacion);
                });

                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
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
            }
        }
    }
}
