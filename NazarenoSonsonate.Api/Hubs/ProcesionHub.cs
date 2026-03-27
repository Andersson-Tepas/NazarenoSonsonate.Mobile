using Microsoft.AspNetCore.SignalR;
using NazarenoSonsonate.Shared.DTOs;

namespace NazarenoSonsonate.Api.Hubs
{
    public class ProcesionHub : Hub
    {
        public async Task EnviarUbicacion(UbicacionProcesionDto ubicacion)
        {
            await Clients.All.SendAsync("RecibirUbicacion", ubicacion);
        }
    }
}
