using System;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;

namespace VinhKhanhstreet.Services
{
    public class GpsService
    {
        // Cấu hình độ chính xác và tần suất quét
        public async Task StartTracking(Action<Location> onLocationChanged)
        {
            try
            {
                // Kiểm tra và xin quyền truy cập vị trí
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    // Vòng lặp lấy vị trí liên tục (Yêu cầu 1: GPS Tracking thời gian thực)
                    while (true)
                    {
                        // GeolocationAccuracy.High giúp phân biệt các quán sát nhau ở Vĩnh Khánh
                        var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                        var location = await Geolocation.Default.GetLocationAsync(request);

                        if (location != null)
                        {
                            // Trả vị trí về cho nơi gọi hàm này xử lý
                            onLocationChanged?.Invoke(location);
                        }

                        // Tối ưu pin: Nghỉ 10 giây giữa các lần quét (Yêu cầu 1)
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ví dụ: người dùng tắt GPS giữa chừng)
                System.Diagnostics.Debug.WriteLine($"GPS Error: {ex.Message}");
            }
        }
    }
}