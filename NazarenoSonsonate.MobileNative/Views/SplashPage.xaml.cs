using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NazarenoSonsonate.MobileNative.Services;

namespace NazarenoSonsonate.MobileNative.Views
{
    public partial class SplashPage : ContentPage
    {
        private readonly RecorridoService _recorridoService;
        private bool _navegando;

        public SplashPage(RecorridoService recorridoService)
        {
            InitializeComponent();
            _recorridoService = recorridoService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_navegando)
                return;

            _navegando = true;

            var preloadTask = _recorridoService.PreloadRecorridosAsync();
            var delayTask = Task.Delay(2000);

            await delayTask;

            await Shell.Current.GoToAsync(nameof(RecorridosPage));

            _ = preloadTask;
        }
    }
}
