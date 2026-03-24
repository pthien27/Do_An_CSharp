
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanhstreet.Models;
using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages;

public partial class MapPage : ContentPage
{
    private GpsService _gpsService;
    private string _currentLang = "vi"; // Mặc định là tiếng Việt

    // --- CẬP NHẬT DANH SÁCH SONG NGỮ ---
    private readonly List<PoiModel> _vinhKhanhPois = new()
    {
        new PoiModel {
            Name = "Ốc Oanh",
            Latitude = 10.7588, Longitude = 106.7052,
            Radius = 30,
            Description = "Chào mừng bạn đến với Ốc Oanh. Đây là địa điểm ẩm thực không thể bỏ qua tại Quận 4. Phan Văn Thiện",
            DescriptionEn = "welcome to Oc Oanh. This is a must-visit culinary spot in District 4." // Thêm thuộc tính này vào Model nếu chưa có
        },
        new PoiModel {
            Name = "Ốc Vũ",
            Latitude = 10.7585, Longitude = 106.7055,
            Radius = 30,
            Description = "Bạn đang đứng trước Ốc Vũ. Quán nổi tiếng với món sò dương nướng mỡ hành.",
            DescriptionEn = "You are in front of Oc Vu. The restaurant is famous for grilled sentinel crab."
        },
        new PoiModel {
            Name = "Điểm Test",
          Latitude = 10.75865, Longitude = 106.70535,
        Radius = 50,
            Description = "123456789.",
            DescriptionEn = "The automatic narration system is ready."
        }
    };

    public MapPage()
    {
        InitializeComponent();
        _gpsService = new GpsService();
        var vinhKhanh = new Location(10.7588, 106.7052);
        mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromKilometers(0.5)));

        AddPinsToMap();
        lstQuánFull.ItemsSource = _vinhKhanhPois;
        StartAutoTracking();
    }

    // --- HÀM XỬ LÝ CHỌN NGÔN NGỮ ---
    private void OnLanguageViTapped(object sender, EventArgs e)
    {
        _currentLang = "vi";
        lblVi.TextColor = Color.FromArgb("#2ECC71"); lblVi.FontAttributes = FontAttributes.Bold;
        lblEn.TextColor = Colors.Gray; lblEn.FontAttributes = FontAttributes.None;
        lblStatus.Text = "Đã chuyển sang Tiếng Việt";
    }

    private void OnLanguageEnTapped(object sender, EventArgs e)
    {
        _currentLang = "en";
        lblEn.TextColor = Color.FromArgb("#2ECC71"); lblEn.FontAttributes = FontAttributes.Bold;
        lblVi.TextColor = Colors.Gray; lblVi.FontAttributes = FontAttributes.None;
        lblStatus.Text = "Switched to English";
    }

    // --- HÀM THUYẾT MINH ĐA NGÔN NGỮ ---
    private async Task PhatThuyetMinh(PoiModel poi)
    {
        string noidung = (_currentLang == "vi") ? poi.Description : poi.DescriptionEn;
        string localeStr = (_currentLang == "vi") ? "vi-VN" : "en-US";

        if (string.IsNullOrWhiteSpace(noidung)) return;

        // Tìm giọng đọc phù hợp
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var locale = locales.FirstOrDefault(l => l.Language.StartsWith(_currentLang));

        var options = new SpeechOptions() { Locale = locale, Pitch = 1.0f, Volume = 1.0f };
        await TextToSpeech.Default.SpeakAsync(noidung, options);
    }

    private void AddPinsToMap()
    {
        foreach (var poi in _vinhKhanhPois)
        {
            var pin = new Pin { Label = poi.Name, Location = new Location(poi.Latitude, poi.Longitude), Type = PinType.Place };
            pin.MarkerClicked += async (s, e) => {
                statusDot.Fill = Colors.Green;
                lblStatus.Text = (_currentLang == "vi") ? $"Đang thuyết minh: {poi.Name}" : $"Narrating: {poi.Name}";
                await PhatThuyetMinh(poi);
            };
            mapVinhKhanh.Pins.Add(pin);
        }
    }

    private void OnShowRestaurantListTapped(object sender, EventArgs e)
    {
        frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
        if (frmDanhSach.IsVisible)
        {
            lblStatus.Text = (_currentLang == "vi") ? "Danh sách quán ăn" : "Restaurant List";
        }
    }

    private async void OnRestaurantSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PoiModel selectedPoi)
        {
            statusDot.Fill = Colors.Green;
            var location = new Location(selectedPoi.Latitude, selectedPoi.Longitude);
            mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));
            await PhatThuyetMinh(selectedPoi);
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void StartAutoTracking()
    {
        await _gpsService.StartTracking((location) => {
            MainThread.BeginInvokeOnMainThread(async () => {
                mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));
                bool foundAny = false;
                foreach (var poi in _vinhKhanhPois)
                {
                    double distance = location.CalculateDistance(new Location(poi.Latitude, poi.Longitude), DistanceUnits.Kilometers) * 1000;
                    if (distance <= poi.Radius)
                    {
                        foundAny = true;
                        if ((DateTime.Now - poi.LastActivated).TotalSeconds > 15)
                        {
                            poi.LastActivated = DateTime.Now;
                            await PhatThuyetMinh(poi);
                        }
                    }
                }
                statusDot.Fill = foundAny ? Colors.Green : Colors.Red;
            });
        });
    }
}