using Microsoft.AspNetCore.Components.WebView.Maui;

namespace NazarenoSonsonate.Mobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var blazorWebView = new BlazorWebView
            {
                HostPage = "wwwroot/index.html",
                StartPath = "/splashscreen"
            };

            blazorWebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(Components.Routes)
            });

            RootGrid.Children.Add(blazorWebView);
        }
    }
}