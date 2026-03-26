
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanhstreet.Models;
using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages;

public partial class MapPage : ContentPage
{
    private GpsService _gpsService;
    private string _currentLang = "vi"; // Mặc định là tiếng Việt
    private CancellationTokenSource _ttsCts; // Biến dùng để ngắt thuyết minh cũ
    private float _currentPitch = 1.0f; // Độ vang giọng đọc
    private string _currentTab = "All"; // Quản lý danh sách đang mở (All / Favorites)

    // --- CẬP NHẬT DANH SÁCH SONG NGỮ ---
    private readonly System.Collections.ObjectModel.ObservableCollection<PoiModel> _vinhKhanhPois = new()
    {
        new PoiModel {
            Name = "Ốc Oanh",
            Latitude = 10.7588, Longitude = 106.7052,
            Radius = 30,
            Rating = 4.4, ReviewCount = 649, ClosingTime = "23:00",
            PhoneNumber = "0901111222", CategoryVi = "Quán ốc",
            Description = "Chào mừng bạn đến với Ốc Oanh. Đây là địa điểm ẩm thực không thể bỏ qua tại Quận 4."
        },
        new PoiModel {
            Name = "Ốc Vũ",
            Latitude = 10.7585, Longitude = 106.7055,
            Radius = 30,
            Rating = 4.2, ReviewCount = 420, ClosingTime = "22:30",
            PhoneNumber = "0903333444", CategoryVi = "Quán ốc",
            Description = "Bạn đang đứng trước Ốc Vũ. Quán nổi tiếng với món sò dương nướng mỡ hành."
        },
        new PoiModel {
            Name = "Điểm Test",
            Latitude = 10.75865, Longitude = 106.70535,
            Radius = 50,
            Rating = 5.0, ReviewCount = 10, ClosingTime = "23:59",
            PhoneNumber = "18001008", CategoryVi = "Hệ thống",
            Description = "Đây là Điểm Test tự động từ hệ thống."
        }
    };

    public MapPage()
    {
        InitializeComponent();
        _gpsService = new GpsService();
        var vinhKhanh = new Location(10.7588, 106.7052);
        mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromKilometers(0.5)));

        // Gọi UpdateStaticUI ngay đầu để nó tự động load toàn bộ chữ (Tiếng Việt) và đổ vào Model
        _ = UpdateStaticUI();

        AddPinsToMap();
        lstQuánFull.ItemsSource = _vinhKhanhPois;
        StartAutoTracking();
    }

    // --- HÀM XỬ LÝ CHỌN NGÔN NGỮ ---
    private async void OnLanguageViTapped(object sender, EventArgs e)
    {
        _currentLang = "vi";
        lblVi.TextColor = Color.FromArgb("#2ECC71"); lblVi.FontAttributes = FontAttributes.Bold;
        lblEn.TextColor = Colors.Gray; lblEn.FontAttributes = FontAttributes.None;
        lblJa.TextColor = Colors.Gray; lblJa.FontAttributes = FontAttributes.None;
        lblZh.TextColor = Colors.Gray; lblZh.FontAttributes = FontAttributes.None;
        lblStatus.Text = "Đã chuyển sang Tiếng Việt";
        await UpdateStaticUI();
    }

    private async void OnLanguageEnTapped(object sender, EventArgs e)
    {
        _currentLang = "en";
        lblEn.TextColor = Color.FromArgb("#2ECC71"); lblEn.FontAttributes = FontAttributes.Bold;
        lblVi.TextColor = Colors.Gray; lblVi.FontAttributes = FontAttributes.None;
        lblJa.TextColor = Colors.Gray; lblJa.FontAttributes = FontAttributes.None;
        lblZh.TextColor = Colors.Gray; lblZh.FontAttributes = FontAttributes.None;
        lblStatus.Text = "Switched to English";
        await UpdateStaticUI();
    }

    private async void OnLanguageJaTapped(object sender, EventArgs e)
    {
        _currentLang = "ja";
        lblJa.TextColor = Color.FromArgb("#2ECC71"); lblJa.FontAttributes = FontAttributes.Bold;
        lblVi.TextColor = Colors.Gray; lblVi.FontAttributes = FontAttributes.None;
        lblEn.TextColor = Colors.Gray; lblEn.FontAttributes = FontAttributes.None;
        lblZh.TextColor = Colors.Gray; lblZh.FontAttributes = FontAttributes.None;
        lblStatus.Text = "日本語に切り替えました";
        await UpdateStaticUI();
    }

    private async void OnLanguageZhTapped(object sender, EventArgs e)
    {
        _currentLang = "zh-CN";
        lblZh.TextColor = Color.FromArgb("#2ECC71"); lblZh.FontAttributes = FontAttributes.Bold;
        lblVi.TextColor = Colors.Gray; lblVi.FontAttributes = FontAttributes.None;
        lblEn.TextColor = Colors.Gray; lblEn.FontAttributes = FontAttributes.None;
        lblJa.TextColor = Colors.Gray; lblJa.FontAttributes = FontAttributes.None;
        lblStatus.Text = "已切换为中文";
        await UpdateStaticUI();
    }

    private async Task UpdateStaticUI()
    {
        if (_currentLang == "en")
        {
            lblCoords.Text = "Tap on a map pin to listen to narration";
            lblTabQuanAn.Text = "Restaurants";
            lblTabYeuThich.Text = "Favorites";
            lblTabLichSu.Text = "History";
            lblTabXemThem.Text = "More";
        }
        else if (_currentLang == "ja")
        {
            lblCoords.Text = "マップのピンをタップして音声案内を聞く";
            lblTabQuanAn.Text = "レストラン";
            lblTabYeuThich.Text = "お気に入り";
            lblTabLichSu.Text = "履歴";
            lblTabXemThem.Text = "その他";
        }
        else if (_currentLang.StartsWith("zh"))
        {
            lblCoords.Text = "点击地图上的图钉以收听解说";
            lblTabQuanAn.Text = "餐厅";
            lblTabYeuThich.Text = "收藏";
            lblTabLichSu.Text = "历史";
            lblTabXemThem.Text = "更多";
        }
        else // vi
        {
            lblCoords.Text = "Nhấn vào ghim trên bản đồ để nghe thuyết minh";
            lblTabQuanAn.Text = "Quán ăn";
            lblTabYeuThich.Text = "Yêu thích";
            lblTabLichSu.Text = "Lịch sử";
            lblTabXemThem.Text = "Xem thêm";
        }
        
        UpdateListTitle();

        // Cập nhật màu nút bấm trong Side Menu
        btnMenuVi.BackgroundColor = _currentLang == "vi" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuVi.TextColor = _currentLang == "vi" ? Colors.White : Colors.Gray;

        btnMenuEn.BackgroundColor = _currentLang == "en" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuEn.TextColor = _currentLang == "en" ? Colors.White : Colors.Gray;

        btnMenuJa.BackgroundColor = _currentLang == "ja" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuJa.TextColor = _currentLang == "ja" ? Colors.White : Colors.Gray;

        btnMenuZh.BackgroundColor = _currentLang == "zh-CN" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuZh.TextColor = _currentLang == "zh-CN" ? Colors.White : Colors.Gray;
        
        // Cập nhật chữ trong Side Menu
        if (_currentLang == "en")
        {
            lblSettingsTitle.Text = "Settings";
            lblSettingsLang.Text = "Narration Language";
            lblSettingsPitch.Text = $"Voice Pitch: {_currentPitch:F1}";
            lblPitchLow.Text = "Deep";
            lblPitchHigh.Text = "High";
        }
        else if (_currentLang == "ja")
        {
            lblSettingsTitle.Text = "設定";
            lblSettingsLang.Text = "音声言語";
            lblSettingsPitch.Text = $"声域 (Pitch): {_currentPitch:F1}";
            lblPitchLow.Text = "低い";
            lblPitchHigh.Text = "高い";
        }
        else if (_currentLang.StartsWith("zh"))
        {
            lblSettingsTitle.Text = "设置";
            lblSettingsLang.Text = "语音语言";
            lblSettingsPitch.Text = $"音高 (Pitch): {_currentPitch:F1}";
            lblPitchLow.Text = "低沉";
            lblPitchHigh.Text = "高亢";
        }
        else 
        {
            lblSettingsTitle.Text = "Cài đặt";
            lblSettingsLang.Text = "Ngôn ngữ thuyết minh";
            lblSettingsPitch.Text = $"Độ vang giọng: {_currentPitch:F1}";
            lblPitchLow.Text = "Trầm";
            lblPitchHigh.Text = "Thanh";
        }

        // Cập nhật ngôn ngữ cho danh sách CollectionView
        foreach (var poi in _vinhKhanhPois)
        {
            string translated = poi.Description;
            
            // Text tĩnh của từng quán
            string category = poi.CategoryVi;
            string open = "Đang mở cửa";
            string close = "Đóng cửa vào";
            string play = "Phát";
            string call = "Gọi";
            string save = poi.IsFavorite ? "Đã lưu" : "Lưu";

            if (_currentLang == "en")
            {
                if (string.IsNullOrWhiteSpace(poi.DescriptionEn))
                {
                    var result = await GoogleTranslateService.TranslateAsync(poi.Description, "en", "vi");
                    if (result != null) poi.DescriptionEn = result;
                    await Task.Delay(200); // Tránh bị Google Block vì gọi nhiều request cùng lúc
                }
                translated = poi.DescriptionEn ?? poi.Description;
                
                open = "Open"; close = "Closes at"; play = "Play"; call = "Call"; save = poi.IsFavorite ? "Saved" : "Save";
                category = (poi.CategoryVi == "Quán ốc") ? "Seafood Restaurant" : "System";
            }
            else if (_currentLang == "ja")
            {
                if (string.IsNullOrWhiteSpace(poi.DescriptionJa))
                {
                    var result = await GoogleTranslateService.TranslateAsync(poi.Description, "ja", "vi");
                    if (result != null) poi.DescriptionJa = result;
                    await Task.Delay(200);
                }
                translated = poi.DescriptionJa ?? poi.Description;
                
                open = "営業中"; close = "営業時間終了"; play = "再生"; call = "電話"; save = poi.IsFavorite ? "保存済み" : "保存";
                category = (poi.CategoryVi == "Quán ốc") ? "シーフードレストラン" : "システム";
            }
            else if (_currentLang.StartsWith("zh"))
            {
                if (string.IsNullOrWhiteSpace(poi.DescriptionZh))
                {
                    var result = await GoogleTranslateService.TranslateAsync(poi.Description, "zh-CN", "vi");
                    if (result != null) poi.DescriptionZh = result;
                    await Task.Delay(200);
                }
                translated = poi.DescriptionZh ?? poi.Description;
                
                open = "营业中"; close = "打烊时间"; play = "播放"; call = "打电话"; save = poi.IsFavorite ? "已保存" : "保存";
                category = (poi.CategoryVi == "Quán ốc") ? "海鲜餐厅" : "系统";
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Kích hoạt INotifyPropertyChanged trên luồng UI
                poi.CurrentDisplayDescription = translated;
                poi.StrCategoryAndDistance = $"{category} · {4.3} km";
                poi.StrStatusOpen = open;
                poi.StrClosingTime = $"{close} {poi.ClosingTime}";
                poi.StrPlay = play;
                poi.StrCall = call;
                poi.StrSave = save;
            });
        }
    }

    // --- HÀM THUYẾT MINH ĐA NGÔN NGỮ ---
    private async Task PhatThuyetMinh(PoiModel poi)
    {
        // 1. Hủy tiến trình đọc cũ nếu nó đang đọc
        _ttsCts?.Cancel();
        // 2. Tạo một tiến trình hủy mới
        _ttsCts = new CancellationTokenSource();

        string noidung = poi.Description;

        // Tự động dịch tùy theo ngôn ngữ đang chọn
        if (_currentLang == "en")
        {
            if (string.IsNullOrWhiteSpace(poi.DescriptionEn))
            {
                var result = await GoogleTranslateService.TranslateAsync(poi.Description, "en", "vi");
                if (result != null) poi.DescriptionEn = result;
            }
            noidung = poi.DescriptionEn ?? poi.Description;
        }
        else if (_currentLang == "ja")
        {
            if (string.IsNullOrWhiteSpace(poi.DescriptionJa))
            {
                var result = await GoogleTranslateService.TranslateAsync(poi.Description, "ja", "vi");
                if (result != null) poi.DescriptionJa = result;
            }
            noidung = poi.DescriptionJa ?? poi.Description;
        }
        else if (_currentLang.StartsWith("zh"))
        {
            if (string.IsNullOrWhiteSpace(poi.DescriptionZh))
            {
                var result = await GoogleTranslateService.TranslateAsync(poi.Description, "zh-CN", "vi");
                if (result != null) poi.DescriptionZh = result;
            }
            noidung = poi.DescriptionZh ?? poi.Description;
        }

        if (string.IsNullOrWhiteSpace(noidung)) return;

        // Tìm giọng đọc phù hợp
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        // Đối phó với "zh-CN" vs "zh"
        var langSearch = _currentLang == "zh-CN" ? "zh" : _currentLang;
        var locale = locales.FirstOrDefault(l => l.Language.StartsWith(langSearch));

        var options = new SpeechOptions() { Locale = locale, Pitch = _currentPitch, Volume = 1.0f };
        
        try
        {
            // Sử dụng CancellationToken để có thể ngắt bất cứ lúc nào
            await TextToSpeech.Default.SpeakAsync(noidung, options, _ttsCts.Token);
        }
        catch (TaskCanceledException)
        {
            // Bỏ qua lỗi do người dùng chủ động bấm chuyển quán khác (Ngắt thành công)
        }
    }

    private void AddPinsToMap()
    {
        foreach (var poi in _vinhKhanhPois)
        {
            var pin = new Pin { Label = poi.Name, Location = new Location(poi.Latitude, poi.Longitude), Type = PinType.Place };
            pin.MarkerClicked += async (s, e) => {
                statusDot.Fill = Colors.Green;
                
                if (_currentLang == "vi") lblStatus.Text = $"Đang thuyết minh: {poi.Name}";
                else if (_currentLang == "en") lblStatus.Text = $"Narrating: {poi.Name}";
                else if (_currentLang == "ja") lblStatus.Text = $"説明中: {poi.Name}";
                else lblStatus.Text = $"解说中: {poi.Name}";
                
                await PhatThuyetMinh(poi);
            };
            mapVinhKhanh.Pins.Add(pin);
        }
    }

    private void OnShowRestaurantListTapped(object sender, EventArgs e)
    {
        if (_currentTab == "Favorites")
        {
            _currentTab = "All";
            lstQuánFull.ItemsSource = _vinhKhanhPois;
            frmDanhSach.IsVisible = true;
        }
        else
        {
            frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
        }

        UpdateListTitle();

        if (frmDanhSach.IsVisible)
        {
            if (_currentLang == "vi") lblStatus.Text = "Danh sách quán ăn";
            else if (_currentLang == "en") lblStatus.Text = "Restaurant List";
            else if (_currentLang == "ja") lblStatus.Text = "レストランリスト";
            else lblStatus.Text = "餐厅列表";
        }
    }

    private void OnShowFavoritesTapped(object sender, EventArgs e)
    {
        if (_currentTab == "All")
        {
            _currentTab = "Favorites";
            frmDanhSach.IsVisible = true;
        }
        else
        {
            frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
        }

        var favorites = _vinhKhanhPois.Where(p => p.IsFavorite).ToList();
        lstQuánFull.ItemsSource = favorites;

        UpdateListTitle();

        if (frmDanhSach.IsVisible)
        {
            if (_currentLang == "vi") lblStatus.Text = "Danh sách yêu thích";
            else if (_currentLang == "en") lblStatus.Text = "Favorites List";
            else if (_currentLang == "ja") lblStatus.Text = "お気に入りリスト";
            else lblStatus.Text = "收藏列表";
        }
    }

    private void UpdateListTitle()
    {
        if (_currentTab == "Favorites")
        {
            if (_currentLang == "en") lblListTitle.Text = "FAVORITE RESTAURANTS";
            else if (_currentLang == "ja") lblListTitle.Text = "お気に入りのレストラン";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = "收藏的餐厅";
            else lblListTitle.Text = "DANH SÁCH YÊU THÍCH";
        }
        else
        {
            if (_currentLang == "en") lblListTitle.Text = "RESTAURANTS ON VINH KHANH STREET";
            else if (_currentLang == "ja") lblListTitle.Text = "ヴィンカン通りのレストラン";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = "永庆街上的餐厅";
            else lblListTitle.Text = "QUÁN ĂN TRÊN PHỐ VĨNH KHÁNH";
        }
    }

    // --- HÀM CHO MENU CÀI ĐẶT (XEM THÊM) ---
    private async void OnXemThemTapped(object sender, EventArgs e)
    {
        bgOverlay.IsVisible = true;
        _ = bgOverlay.FadeTo(0.4, 250);
        await frmSettings.TranslateTo(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseSettingsTapped(object sender, EventArgs e)
    {
        _ = bgOverlay.FadeTo(0, 250);
        await frmSettings.TranslateTo(280, 0, 300, Easing.CubicIn);
        bgOverlay.IsVisible = false;
    }

    private void OnPitchValueChanged(object sender, ValueChangedEventArgs e)
    {
        _currentPitch = (float)e.NewValue;
        
        string baseText = (_currentLang == "vi") ? "Độ vang giọng" : 
                          (_currentLang == "en") ? "Voice Pitch" : 
                          (_currentLang == "ja") ? "声域 (Pitch)" : "音高 (Pitch)";

        lblSettingsPitch.Text = $"{baseText}: {_currentPitch:F1}";
    }

    // --- HÀM NÚT BẤM TRÊN THẺ QUÁN ĂN ---
    private async void OnCardPlayClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PoiModel poi)
        {
            await PhatThuyetMinh(poi);
        }
    }

    private void OnCardCallClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PoiModel poi)
        {
            if (Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.IsSupported)
                Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.Open(poi.PhoneNumber);
        }
    }

    private void OnCardSaveClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PoiModel poi)
        {
            poi.IsFavorite = !poi.IsFavorite;
            
            // Cập nhật lại nhãn "Lưu"/"Đã Lưu" lập tức tại chỗ
            string save = poi.IsFavorite ? "Đã lưu" : "Lưu";
            if (_currentLang == "en") save = poi.IsFavorite ? "Saved" : "Save";
            else if (_currentLang == "ja") save = poi.IsFavorite ? "保存済み" : "保存";
            else if (_currentLang.StartsWith("zh")) save = poi.IsFavorite ? "已保存" : "保存";
            
            poi.StrSave = save;
        }
    }

    private async void OnRestaurantSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PoiModel selectedPoi)
        {
            statusDot.Fill = Colors.Green;
            var location = new Location(selectedPoi.Latitude, selectedPoi.Longitude);
            mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));
            // Chỉ di chuyển tâm bản đồ tới địa điểm, việc phát âm thanh nhường cho nút Play ở thẻ
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