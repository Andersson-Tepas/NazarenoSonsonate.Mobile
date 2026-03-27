using Microsoft.AspNetCore.SignalR;
using NazarenoSonsonate.Shared.DTOs;

namespace NazarenoSonsonate.Api.Hubs
{
    public class ProcesionHub : Hub
    {
        private static string ObtenerGrupo(int recorridoId)
            => $"recorrido-{recorridoId}";

        public async Task UnirseRecorrido(int recorridoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ObtenerGrupo(recorridoId));
        }

        public async Task SalirRecorrido(int recorridoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ObtenerGrupo(recorridoId));
        }
    }
}
