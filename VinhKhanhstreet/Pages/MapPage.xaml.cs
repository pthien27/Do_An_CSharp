using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanhstreet.Models;
using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages;

public partial class MapPage : ContentPage
{
    private GpsService _gpsService;

    // --- DANH SÁCH QUÁN ĂN ---
    private readonly List<PoiModel> _vinhKhanhPois = new()
    {
        new PoiModel {
            Name = "Ốc Oanh",
            Latitude = 10.7588, Longitude = 106.7052,
            Radius = 30, Description = "Chào mừng bạn đến với ốc Oanh, quán ốc nổi tiếng nhất phố Vĩnh Khánh."
        },
        new PoiModel {
            Name = "Ốc Vũ",
            Latitude = 10.7585, Longitude = 106.7055,
            Radius = 30, Description = "Bạn đang ở gần ốc Vũ, nơi có món sò nướng mỡ hành cực kỳ thơm ngon."
        },
        new PoiModel {
            Name = "Điểm Test",
            Latitude = 10.78501, Longitude = 106.61489,
            Radius = 50, Description = "Hệ thống thuyết minh tự động phố ẩm thực Vĩnh Khánh đã sẵn sàng!"
        }
    };

    public MapPage()
    {
        InitializeComponent();
        _gpsService = new GpsService();

        // 1. Đặt mặc định bản đồ về Vĩnh Khánh ngay khi mở app
        var vinhKhanh = new Location(10.7588, 106.7052);
        mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromKilometers(0.5)));

        // 2. Cắm ghim các quán lên bản đồ
        AddPinsToMap();

        // 3. Nạp dữ liệu vào danh sách đứng (frmDanhSach)
        lstQuánFull.ItemsSource = _vinhKhanhPois;
    }

    private void AddPinsToMap()
    {
        foreach (var poi in _vinhKhanhPois)
        {
            var pin = new Pin
            {
                Label = poi.Name,
                Location = new Location(poi.Latitude, poi.Longitude),
                Type = PinType.Place
            };
            mapVinhKhanh.Pins.Add(pin);
        }
    }

    // --- XỬ LÝ KHI BẤM VÀO ICON "QUÁN ĂN" ---
    private void OnShowRestaurantListTapped(object sender, EventArgs e)
    {
        // Đảo ngược trạng thái ẩn/hiện của khung danh sách dưới đáy
        frmDanhSach.IsVisible = !frmDanhSach.IsVisible;

        if (frmDanhSach.IsVisible)
        {
            lblStatus.Text = "Danh sách quán ăn trên phố";

            // CẬP NHẬT: Hiện số lượng quán vào khung trắng Dashboard thay vì tọa độ
            lblCoords.Text = $"Tìm thấy {_vinhKhanhPois.Count} quán ăn nổi bật";
            lblCoords.TextColor = Colors.Green;
        }
        else
        {
            lblStatus.Text = "Hệ thống đã sẵn sàng";
            lblCoords.TextColor = Colors.Gray;
        }
    }

    // --- XỬ LÝ THEO DÕI GPS ---
    private async void OnStartGpsClicked(object sender, EventArgs e)
    {
        lblStatus.Text = "Đang kết nối vệ tinh...";
        btnStart.Text = "ĐANG THEO DÕI...";
        btnStart.BackgroundColor = Colors.Orange;

        await _gpsService.StartTracking((location) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 1. Cập nhật tọa độ thực tế (nhỏ) nếu không mở danh sách
                if (!frmDanhSach.IsVisible)
                {
                    lblCoords.Text = $"Vị trí: {location.Latitude:F5}, {location.Longitude:F5}";
                }

                // 2. Di chuyển tâm bản đồ theo người dùng
                mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));

                bool foundAny = false;

                // 3. Kiểm tra các điểm POI xung quanh
                foreach (var poi in _vinhKhanhPois)
                {
                    double distance = location.CalculateDistance(new Location(poi.Latitude, poi.Longitude), DistanceUnits.Kilometers) * 1000;

                    if (distance <= poi.Radius)
                    {
                        foundAny = true;

                        // Chống lặp âm thanh (10 giây cho Trung dễ test)
                        if ((DateTime.Now - poi.LastActivated).TotalSeconds > 10)
                        {
                            poi.LastActivated = DateTime.Now;
                            lblStatus.Text = $"📍 Đang ở: {poi.Name}";

                            // Phát âm thanh thuyết minh
                            await TextToSpeech.Default.SpeakAsync(poi.Description);
                        }
                    }
                }

                // Cập nhật màu chấm tròn: Xanh nếu ở gần quán, Đỏ nếu không
                statusDot.Fill = foundAny ? Colors.Green : Colors.Red;
            });
        });
    }
}