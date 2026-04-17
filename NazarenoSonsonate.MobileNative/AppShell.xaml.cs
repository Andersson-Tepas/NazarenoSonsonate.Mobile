using NazarenoSonsonate.MobileNative.Views;

namespace NazarenoSonsonate.MobileNative
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(RecorridosPage), typeof(RecorridosPage));
            Routing.RegisterRoute(nameof(MapaRecorridoPage), typeof(MapaRecorridoPage));
        }
    }
}