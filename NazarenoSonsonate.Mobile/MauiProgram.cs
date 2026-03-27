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

        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri("http://192.168.1.30:5180/")
        });

        builder.Services.AddScoped<RecorridoService>();
        builder.Services.AddScoped<SignalRService>();
        builder.Services.AddScoped<UbicacionService>();

        return builder.Build();
    }
}