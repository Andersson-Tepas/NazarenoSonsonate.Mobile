using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace NazarenoSonsonate.Mobile.Platforms.Android.Services
{
    [Service(
        Exported = false,
        ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation
    )]
    public class LocationForegroundService : Service
    {
        public const string ChannelId = "procesion_tracking_channel";
        public const int NotificationId = 1001;

        public override void OnCreate()
        {
            base.OnCreate();
            CreateNotificationChannel();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            // ⚡ IMPORTANTE: notificación inmediata (sin lógica previa)
            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Nazareno Sonsonate")
                .SetContentText("Iniciando rastreo...")
                .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyLocation)
                .SetOngoing(true)
                .Build();

            StartForeground(NotificationId, notification);

            // 🔥 luego ya haces lógica
            var unidad = intent?.GetStringExtra("tipoUnidad") ?? "Procesión";

            // actualizar notificación
            var updatedNotification = BuildNotification($"Rastreo activo: {unidad}");
            var manager = NotificationManagerCompat.From(this);
            manager.Notify(NotificationId, updatedNotification);

            return StartCommandResult.Sticky;
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override void OnDestroy()
        {
            StopForeground(StopForegroundFlags.Remove);
            base.OnDestroy();
        }

        private Notification BuildNotification(string contentText)
        {
            var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName);
            PendingIntent? pendingIntent = null;

            if (launchIntent is not null)
            {
                launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

                pendingIntent = PendingIntent.GetActivity(
                    this,
                    0,
                    launchIntent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            }

            var builder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Nazareno Sonsonate")
                .SetContentText(contentText)
                .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyLocation)
                .SetOngoing(true)
                .SetOnlyAlertOnce(true)
                .SetPriority((int)NotificationPriority.Low);

            if (pendingIntent is not null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            return builder.Build();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var channel = new NotificationChannel(
                ChannelId,
                "Rastreo de Procesión",
                NotificationImportance.Low)
            {
                Description = "Notificación del rastreo activo de la procesión"
            };

            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }
    }
}