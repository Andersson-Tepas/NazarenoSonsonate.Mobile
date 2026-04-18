using Microsoft.Extensions.Logging;
using NazarenoSonsonate.Mobile.Services;

namespace NazarenoSonsonate.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        const string baseUrl = "http://192.168.1.25:5180/";

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        });

        builder.Services.AddSingleton<RecorridoService>();
        builder.Services.AddSingleton<UbicacionTrackingService>();
        builder.Services.AddSingleton<PuntoRutaCacheService>();
        builder.Services.AddSingleton<AdminService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddSingleton<UbicacionService>();

        return builder.Build();
    }
}