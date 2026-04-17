using Microsoft.Extensions.Logging;
using NazarenoSonsonate.MobileNative.Services;

namespace NazarenoSonsonate.MobileNative
{
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            const string baseUrl = "http://192.168.1.24:5180/";

            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            });

            builder.Services.AddSingleton<RecorridoService>();
            builder.Services.AddSingleton<PuntoRutaCacheService>();
            builder.Services.AddSingleton<AdminService>();
            builder.Services.AddScoped<SignalRService>();
            builder.Services.AddScoped<UbicacionService>();

            return builder.Build();
        }
    }
}