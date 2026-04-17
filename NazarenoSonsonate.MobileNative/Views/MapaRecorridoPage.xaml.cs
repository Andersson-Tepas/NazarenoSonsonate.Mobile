using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using NazarenoSonsonate.MobileNative.Services;
using NazarenoSonsonate.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NazarenoSonsonate.MobileNative.Views
{
    [QueryProperty(nameof(RecorridoId), "id")]
    public partial class MapaRecorridoPage : ContentPage
    {
        private readonly RecorridoService _recorridoService;
        private readonly UbicacionService _ubicacionService;

        private RecorridoDto? _recorrido;
        private List<PuntoRutaDto> _puntos = new();

        public string RecorridoId { get; set; } = string.Empty;

        public MapaRecorridoPage(
            RecorridoService recorridoService,
            UbicacionService ubicacionService)
        {
            InitializeComponent();
            _recorridoService = recorridoService;
            _ubicacionService = ubicacionService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            try
            {
                MostrarOverlay("Cargando recorrido...");

                if (!int.TryParse(RecorridoId, out var id))
                {
                    MostrarOverlay("Recorrido inválido.");
                    return;
                }

                _recorrido = await _recorridoService.ObtenerRecorridoPorIdAsync(
                    id,
                    forzarRecarga: false,
                    permitirApi: true);

                if (_recorrido is null)
                {
                    MostrarOverlay("No se encontró el recorrido.");
                    return;
                }

                TituloLabel.Text = _recorrido.Nombre;
                EstadoLabel.Text = "UBICACIÓN EN VIVO";

                _puntos = _recorrido.PuntosRuta?.ToList() ?? new List<PuntoRutaDto>();

                if (_puntos.Count == 0)
                {
                    MostrarOverlay("Recorrido pendiente de publicación.");
                    return;
                }

                DibujarPuntos(_puntos);
                OcultarOverlay();
            }
            catch (Exception ex)
            {
                MostrarOverlay(ex.Message);
            }
        }

        private void DibujarPuntos(List<PuntoRutaDto> puntos)
        {
            Mapa.Pins.Clear();

            foreach (var punto in puntos)
            {
                Mapa.Pins.Add(new Pin
                {
                    Label = string.IsNullOrWhiteSpace(punto.Referencia) ? "Punto" : punto.Referencia,
                    Address = $"{punto.Tipo} {punto.Grupo}".Trim(),
                    Location = new Location(punto.Latitud, punto.Longitud)
                });
            }

            var primero = puntos.First();
            var center = new Location(primero.Latitud, primero.Longitud);
            Mapa.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(0.6)));
        }

        private void MostrarOverlay(string texto)
        {
            OverlayLabel.Text = texto;
            OverlayFrame.IsVisible = true;
        }

        private void OcultarOverlay()
        {
            OverlayFrame.IsVisible = false;
        }

        private void OnProcesionClicked(object sender, EventArgs e)
        {
            if (_puntos.Count > 0)
                DibujarPuntos(_puntos);
        }

        private async void OnMiUbicacionClicked(object sender, EventArgs e)
        {
            try
            {
                var permiso = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (permiso != PermissionStatus.Granted)
                    permiso = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (permiso != PermissionStatus.Granted)
                    return;

                var request = new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(10));

                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location is null)
                    return;

                var miUbicacion = new Location(location.Latitude, location.Longitude);

                Mapa.Pins.Add(new Pin
                {
                    Label = "Mi ubicación",
                    Location = miUbicacion
                });

                Mapa.MoveToRegion(MapSpan.FromCenterAndRadius(miUbicacion, Distance.FromKilometers(0.3)));
            }
            catch
            {
            }
        }

        private void OnCargadorClicked(object sender, EventArgs e)
        {
            var filtrados = _puntos
                .Where(x => string.Equals(x.Tipo?.Trim(), "cargador", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtrados.Count > 0)
                DibujarPuntos(filtrados);
        }

        private void OnCargadoraClicked(object sender, EventArgs e)
        {
            var filtrados = _puntos
                .Where(x => string.Equals(x.Tipo?.Trim(), "cargadora", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtrados.Count > 0)
                DibujarPuntos(filtrados);
        }

        private async void OnGruposClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Grupos", "Luego metemos el selector de grupos nativo.", "OK");
        }
    }
}
