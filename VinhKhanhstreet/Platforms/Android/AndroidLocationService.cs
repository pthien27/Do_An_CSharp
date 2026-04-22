using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Android.Content.PM;

namespace VinhKhanhstreet.Platforms.Android
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class AndroidLocationService : Service
    {
        private const int NOTIFICATION_ID = 10001;
        private const string CHANNEL_ID = "vks_location_channel";

        public override IBinder OnBind(Intent intent)
        {
            return null; // Chúng ta không cần bind service này, chỉ cần nó Start là được
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();
            var notification = BuildNotification();
            
            // Kích hoạt Foreground Service -> Báo với Android OS: "Đừng giết tiến trình này!"
            StartForeground(NOTIFICATION_ID, notification);

            // Sticky: Nếu bị gián đoạn do thiếu RAM cục bộ, tự động khởi động lại
            return StartCommandResult.Sticky;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    "Vinh Khánh Street - Theo dõi Vị trí",
                    NotificationImportance.Low) // Low để không báo chuông mỗi giây
                {
                    Description = "Cần thiết để tự động phát thuyết minh POI khi bạn bỏ điện thoại vào túi."
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification BuildNotification()
        {
            var intent = new Intent(this, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("Vĩnh Khánh Street")
                .SetContentText("Đang chạy ngầm và quét POI xung quanh bạn...")
                .SetSmallIcon(Resource.Mipmap.appicon) // Icon app hiện tại
                .SetOngoing(true)
                .SetContentIntent(pendingIntent);

            return builder.Build();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
                StopForeground(true);
            }
        }
    }
}
