using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhStreet.Services
{
    public class LocationService
    {
        private bool _isTracking;
        private CancellationTokenSource _cts;

        // Sự kiện để thông báo cho giao diện mỗi khi vị trí thay đổi
        public event Action<Location> LocationChanged;

        public async Task StartTracking()
        {
            if (_isTracking) return;

            // Kiểm tra quyền truy cập vị trí
            var status = await CheckAndRequestLocationPermission();
            if (status != PermissionStatus.Granted) return;

            _isTracking = true;
            _cts = new CancellationTokenSource();

            // Chạy vòng lặp lấy vị trí liên tục (Yêu cầu 1)
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Độ chính xác cao (High Accuracy) phù hợp cho phố nhỏ Vĩnh Khánh
                        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
                        var location = await Geolocation.Default.GetLocationAsync(request, _cts.Token);

                        if (location != null)
                        {
                            // Bắn sự kiện về cho giao diện hoặc POI Manager xử lý
                            LocationChanged?.Invoke(location);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Xử lý lỗi nếu GPS bị tắt đột ngột
                    }

                    // Tối ưu pin: Nghỉ 5 giây trước khi quét tiếp (có thể tăng lên tùy tốc độ di chuyển)
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }, _cts.Token);
        }

        public void StopTracking()
        {
            _isTracking = false;
            _cts?.Cancel();
        }

        private async Task<PermissionStatus> CheckAndRequestLocationPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status;
        }
    }
}