using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NazarenoSonsonate.MobileNative.Services;
using NazarenoSonsonate.Shared.DTOs;

namespace NazarenoSonsonate.MobileNative.Views
{
    public partial class RecorridosPage : ContentPage
    {
        private readonly RecorridoService _recorridoService;
        private readonly AdminService _adminService;

        private List<RecorridoItemViewModel> _recorridos = new();

        public RecorridosPage(RecorridoService recorridoService, AdminService adminService)
        {
            InitializeComponent();
            _recorridoService = recorridoService;
            _adminService = adminService;

            _adminService.OnChange += RefreshUI;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarRecorridosAsync();
        }

        private async Task CargarRecorridosAsync()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                ErrorLabel.IsVisible = false;
                EmptyLabel.IsVisible = false;
                RecorridosCollection.IsVisible = false;

                var recorridos = await _recorridoService.ObtenerRecorridosAsync(
                    forzarRecarga: false,
                    permitirApi: false);

                if (recorridos.Count == 0)
                {
                    recorridos = await _recorridoService.ObtenerRecorridosAsync(
                        forzarRecarga: true,
                        permitirApi: true);
                }

                _recorridos = recorridos.Select(r => new RecorridoItemViewModel
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion,
                    HoraSalida = r.HoraSalida,
                    ImagenDia = ObtenerImagenPorDia(r.Nombre),
                    MostrarBotonAdmin = _adminService.IsAdmin
                }).ToList();

                if (_recorridos.Count == 0)
                {
                    EmptyLabel.IsVisible = true;
                    return;
                }

                RecorridosCollection.ItemsSource = _recorridos;
                RecorridosCollection.IsVisible = true;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async void OnVerDetalleClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int id)
            {
                await Shell.Current.GoToAsync($"{nameof(MapaRecorridoPage)}?id={id}");
            }
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int id)
            {
                await DisplayAlert("Admin", $"Aquí abrirá el editor admin del recorrido {id}", "OK");
            }
        }

        private void RefreshUI()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await CargarRecorridosAsync();
            });
        }

        private string ObtenerImagenPorDia(string nombreRecorrido)
        {
            if (string.IsNullOrWhiteSpace(nombreRecorrido))
                return "default.jpg";

            var nombre = nombreRecorrido.ToLower();

            if (nombre.Contains("lunes")) return "lunes.jpg";
            if (nombre.Contains("martes")) return "martes.jpg";
            if (nombre.Contains("miércoles") || nombre.Contains("miercoles")) return "miercoles.jpg";
            if (nombre.Contains("jueves")) return "jueves.jpg";
            if (nombre.Contains("viernes")) return "viernes.jpg";

            return "default.jpg";
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _adminService.OnChange -= RefreshUI;
        }

        private class RecorridoItemViewModel
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string HoraSalida { get; set; } = string.Empty;
            public string ImagenDia { get; set; } = string.Empty;
            public bool MostrarBotonAdmin { get; set; }
        }
    }
}
