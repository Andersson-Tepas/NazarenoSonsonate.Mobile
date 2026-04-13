using Microsoft.AspNetCore.SignalR.Client;
using NazarenoSonsonate.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.Mobile.Services
{
    public class SignalRService
    {
        private HubConnection? _hubConnection;

        public event Action<UbicacionProcesionDto>? UbicacionRecibida;

        public async Task IniciarAsync(string baseUrl)
        {
            try
            {
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
                // 🔥 NO reventar la app
            }
        }

        public async Task DetenerAsync()
        {
            if (_hubConnection is not null)
                await _hubConnection.DisposeAsync();
        }
    }
}
