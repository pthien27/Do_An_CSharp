
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
    private float _currentVolume = 1.0f; // Âm lượng giọng đọc
    private string _currentTab = "All"; // Quản lý danh sách đang mở (All / Favorites)
    private Microsoft.Maui.Controls.Maps.Circle _userScanCircle; // Vòng tròn quét GPS
    private bool _isSearchBarFocused = false;

    // --- CẬP NHẬT DANH SÁCH SONG NGỮ ---
    // Mảng sẽ được nạp từ SQLite thay vì Fix cứng
    private readonly System.Collections.ObjectModel.ObservableCollection<PoiModel> _vinhKhanhPois = new();
    private readonly DatabaseService _dbService = new DatabaseService();

    public MapPage()
    {
        InitializeComponent();

#if ANDROID
        Microsoft.Maui.Maps.Handlers.MapHandler.Mapper.AppendToMapping("HideLocationButton", (handler, view) =>
        {
            if (handler.PlatformView is Android.Gms.Maps.MapView mapView)
            {
                mapView.GetMapAsync(new CustomMapCallback());
            }
        });
#endif
        _gpsService = new GpsService();
        var vinhKhanh = new Location(10.7588, 106.7052);
        mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanh, Distance.FromKilometers(0.5)));

        lstQuánFull.ItemsSource = _vinhKhanhPois;

        // Tiến hành nạp dữ liệu từ SQLite lên và gắn pin vào Map
        _ = LoadDatabaseAndStartAsync();
    }

    private async Task LoadDatabaseAndStartAsync()
    {
        // 1. Kéo dữ liệu từ File DB lên
        var data = await _dbService.GetPoisAsync();
        
        // 2. Chuyển vào mảng giao diện
        _vinhKhanhPois.Clear();
        foreach (var item in data)
        {
            _vinhKhanhPois.Add(item);
        }

        // 3. Mới cập nhật ngôn ngữ (tiếng Việt mặc định)
        await UpdateStaticUI();

        // 4. Mới rải các cột mốc Pin lên Map (Vì phải chờ Data lên đủ)
        AddPinsToMap();

        // 5. Mới bật Radar quét khoảng cách
        StartAutoTracking();
    }

    // --- HÀM XỬ LÝ CHỌN NGÔN NGỮ ---
    private async void OnLanguageViTapped(object sender, EventArgs e)
    {
        _currentLang = "vi";
        lblStatus.Text = "Đã chuyển sang Tiếng Việt";
        await UpdateStaticUI();
    }

    private async void OnLanguageEnTapped(object sender, EventArgs e)
    {
        _currentLang = "en";
        lblStatus.Text = "Switched to English";
        await UpdateStaticUI();
    }

    private async void OnLanguageJaTapped(object sender, EventArgs e)
    {
        _currentLang = "ja";
        lblStatus.Text = "日本語に切り替えました";
        await UpdateStaticUI();
    }

    private async void OnLanguageZhTapped(object sender, EventArgs e)
    {
        _currentLang = "zh-CN";
        lblStatus.Text = "已切换为中文";
        await UpdateStaticUI();
    }

    private async Task UpdateStaticUI()
    {
        if (_currentLang == "en")
        {
            searchBar.Placeholder = "Search restaurants...";
            lblCoords.Text = "Tap on a map pin to listen to narration";
            lblTabQuanAn.Text = "Restaurants";
            lblTabYeuThich.Text = "Favorites";
            lblTabXemThem.Text = "More";
        }
        else if (_currentLang == "ja")
        {
            searchBar.Placeholder = "レストランを検索...";
            lblCoords.Text = "マップのピンをタップして音声案内を聞く";
            lblTabQuanAn.Text = "レストラン";
            lblTabYeuThich.Text = "お気に入り";
            lblTabXemThem.Text = "その他";
        }
        else if (_currentLang.StartsWith("zh"))
        {
            searchBar.Placeholder = "搜索餐厅...";
            lblCoords.Text = "点击地图上的图钉以收听解说";
            lblTabQuanAn.Text = "餐厅";
            lblTabYeuThich.Text = "收藏";
            lblTabXemThem.Text = "更多";
        }
        else // vi
        {
            searchBar.Placeholder = "Tìm quán ăn...";
            lblCoords.Text = "Nhấn vào ghim trên bản đồ để nghe thuyết minh";
            lblTabQuanAn.Text = "Quán ăn";
            lblTabYeuThich.Text = "Yêu thích";
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
            lblSettingsTheme.Text = "Dark Mode";
            lblSettingsLang.Text = "Narration Language";
            lblSettingsPitch.Text = $"Voice Pitch: {_currentPitch:F1}";
            lblPitchLow.Text = "Deep";
            lblPitchHigh.Text = "High";
            if (lblSettingsVolume != null) lblSettingsVolume.Text = $"Volume: {_currentVolume:P0}";
            if (lblVolumeLow != null) lblVolumeLow.Text = "Low";
            if (lblVolumeHigh != null) lblVolumeHigh.Text = "High";
        }
        else if (_currentLang == "ja")
        {
            lblSettingsTitle.Text = "設定";
            lblSettingsTheme.Text = "ダークモード";
            lblSettingsLang.Text = "音声言語";
            lblSettingsPitch.Text = $"声域 (Pitch): {_currentPitch:F1}";
            lblPitchLow.Text = "低い";
            lblPitchHigh.Text = "高い";
            if (lblSettingsVolume != null) lblSettingsVolume.Text = $"音量: {_currentVolume:P0}";
            if (lblVolumeLow != null) lblVolumeLow.Text = "小";
            if (lblVolumeHigh != null) lblVolumeHigh.Text = "大";
        }
        else if (_currentLang.StartsWith("zh"))
        {
            lblSettingsTitle.Text = "设置";
            lblSettingsTheme.Text = "深色模式";
            lblSettingsLang.Text = "语音语言";
            lblSettingsPitch.Text = $"音高 (Pitch): {_currentPitch:F1}";
            lblPitchLow.Text = "低沉";
            lblPitchHigh.Text = "高亢";
            if (lblSettingsVolume != null) lblSettingsVolume.Text = $"音量: {_currentVolume:P0}";
            if (lblVolumeLow != null) lblVolumeLow.Text = "小";
            if (lblVolumeHigh != null) lblVolumeHigh.Text = "大";
        }
        else 
        {
            lblSettingsTitle.Text = "Cài đặt";
            lblSettingsTheme.Text = "Giao diện tối (Dark mode)";
            lblSettingsLang.Text = "Ngôn ngữ thuyết minh";
            lblSettingsPitch.Text = $"Độ vang giọng: {_currentPitch:F1}";
            lblPitchLow.Text = "Trầm";
            lblPitchHigh.Text = "Thanh";
            if (lblSettingsVolume != null) lblSettingsVolume.Text = $"Âm lượng: {_currentVolume:P0}";
            if (lblVolumeLow != null) lblVolumeLow.Text = "Nhỏ";
            if (lblVolumeHigh != null) lblVolumeHigh.Text = "To";
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
                poi.TranslatedCategory = category;
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
        string ttsLang = _currentLang; // Mã ngôn ngữ dùng cho bộ loa (mặc định là ngôn ngữ đang chọn)

        // Tự động dịch tùy theo ngôn ngữ đang chọn
        if (_currentLang == "en")
        {
            if (string.IsNullOrWhiteSpace(poi.DescriptionEn))
            {
                var result = await GoogleTranslateService.TranslateAsync(poi.Description, "en", "vi");
                if (result != null) poi.DescriptionEn = result;
            }
            noidung = poi.DescriptionEn ?? poi.Description;
            // Nếu vẫn không có chữ Anh (do rớt mạng/lỗi dịch), bắt buộc loa phải đọc giọng Tiếng Việt
            if (string.IsNullOrWhiteSpace(poi.DescriptionEn)) ttsLang = "vi";
        }
        else if (_currentLang == "ja")
        {
            if (string.IsNullOrWhiteSpace(poi.DescriptionJa))
            {
                var result = await GoogleTranslateService.TranslateAsync(poi.Description, "ja", "vi");
                if (result != null) poi.DescriptionJa = result;
            }
            noidung = poi.DescriptionJa ?? poi.Description;
            if (string.IsNullOrWhiteSpace(poi.DescriptionJa)) ttsLang = "vi";
        }
        else if (_currentLang.StartsWith("zh"))
        {
            if (string.IsNullOrWhiteSpace(poi.DescriptionZh))
            {
                var result = await GoogleTranslateService.TranslateAsync(poi.Description, "zh-CN", "vi");
                if (result != null) poi.DescriptionZh = result;
            }
            noidung = poi.DescriptionZh ?? poi.Description;
            if (string.IsNullOrWhiteSpace(poi.DescriptionZh)) ttsLang = "vi";
        }

        if (string.IsNullOrWhiteSpace(noidung)) return;

        // Tìm giọng đọc phù hợp dựa trên cờ ttsLang (đã chống lỗi 100%)
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var langSearch = ttsLang == "zh-CN" ? "zh" : ttsLang;
        var locale = locales.FirstOrDefault(l => l.Language.StartsWith(langSearch));

        var options = new SpeechOptions() { Locale = locale, Pitch = _currentPitch, Volume = _currentVolume };
        
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
                
                // MỚI: Chỉ hiển thị thẻ thông tin của quán này trên danh sách
                lstQuánFull.ItemsSource = new List<PoiModel> { poi };
                if (searchBar != null) searchBar.Text = string.Empty;
                frmDanhSach.IsVisible = true;
                UpdateListTitle();
                
                ScrollListToTop();

                await PhatThuyetMinh(poi);
            };
            mapVinhKhanh.Pins.Add(pin);
        }
    }

    private void OnShowRestaurantListTapped(object sender, EventArgs e)
    {
        // Kiểm tra xem có đang đứng sát mục tiêu nào không (khoảng cách <= Radius)
        bool isNearAtLeastOne = _vinhKhanhPois.Any(p => p.DistanceInMeters <= p.Radius);
        
        // Nếu có, ưu tiên chuyển sang hiển thị các quán gần đây (<= Radius của quán đó)
        if (isNearAtLeastOne)
        {
            if (_currentTab != "Nearby")
            {
                _currentTab = "Nearby";
                var nearbyPois = _vinhKhanhPois.Where(p => p.DistanceInMeters <= p.Radius)
                                               .OrderBy(p => p.DistanceInMeters)
                                               .ToList();
                lstQuánFull.ItemsSource = nearbyPois;
                frmDanhSach.IsVisible = true;
                if (searchBar != null) searchBar.Text = string.Empty;
                ScrollListToTop();
            }
            else
            {
                frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
            }
        }
        else
        {
            // Logic Mặc định nếu không đứng gần quán nào
            if (_currentTab == "Favorites" || _currentTab == "Nearby" || lstQuánFull.ItemsSource != _vinhKhanhPois)
            {
                _currentTab = "All";
                lstQuánFull.ItemsSource = _vinhKhanhPois;
                frmDanhSach.IsVisible = true;
                if (searchBar != null) searchBar.Text = string.Empty;
                ScrollListToTop();
            }
            else
            {
                frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
            }
        }

        UpdateListTitle();
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
        ScrollListToTop();
    }

    private async void OnScanQRTapped(object sender, EventArgs e)
    {
        // Kiểm tra quyền Camera trước khi mở máy quét
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Quyền truy cập", "Bạn cần cấp quyền Camera để quét mã QR", "Đóng");
            return;
        }

        var qrPage = new QrScannerPage();
        qrPage.OnScanResult = (result) =>
        {
            // Kết quả quét được, ví dụ: "Ốc Oanh"
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var matchedPoi = _vinhKhanhPois.FirstOrDefault(p =>
                    (p.Name != null && p.Name.Equals(result, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Name != null && p.Name.IndexOf(result, StringComparison.OrdinalIgnoreCase) >= 0));

                if (matchedPoi != null)
                {
                    // Lọc hệ thống chỉ hiển thị đúng quán này
                    _currentTab = "QR"; // Tab ảo để UpdateListTitle nhận diện
                    lstQuánFull.ItemsSource = new System.Collections.Generic.List<PoiModel> { matchedPoi };
                    if (searchBar != null) searchBar.Text = string.Empty;
                    frmDanhSach.IsVisible = true;
                    UpdateListTitle();
                    ScrollListToTop();
                    
                    // Di chuyển trung tâm Bản đồ thẳng tới vị trí quán vừa quét
                    var location = new Location(matchedPoi.Latitude, matchedPoi.Longitude);
                    mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.2)));

                    // Phát thuyết minh luôn nếu chưa từng tự động phát
                    if (!matchedPoi.HasAutoPlayed)
                    {
                        matchedPoi.HasAutoPlayed = true;
                        _ = PhatThuyetMinh(matchedPoi);
                    }
                }
                else
                {
                    DisplayAlert("Không tìm thấy", $"Mã QR có nội dung: '{result}' không thuộc hệ thống nhà hàng của Vĩnh Khánh.", "Đóng");
                }
            });
        };

        await Navigation.PushModalAsync(qrPage);
    }

    private void ScrollListToTop()
    {
        MainThread.BeginInvokeOnMainThread(async () => {
            try {
                await Task.Delay(50); // Đợi Frame render xong
                var list = lstQuánFull.ItemsSource as System.Collections.IList;
                if (list != null && list.Count > 0)
                {
                    lstQuánFull.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
                }
            } catch { } // Bỏ qua lỗi an toàn
        });
    }

    private void UpdateListTitle()
    {
        // Ưu tiên hiển thị Tiêu đề Tìm Kiếm nếu có chữ trong ô Search
        if (searchBar != null && !string.IsNullOrWhiteSpace(searchBar.Text))
        {
            var resultsCount = (lstQuánFull.ItemsSource as System.Collections.IList)?.Count ?? 0;
            if (_currentLang == "en") lblListTitle.Text = $"SEARCH RESULTS ({resultsCount})";
            else if (_currentLang == "ja") lblListTitle.Text = $"検索結果 ({resultsCount})";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = $"搜索结果 ({resultsCount})";
            else lblListTitle.Text = $"KẾT QUẢ TÌM KIẾM ({resultsCount})";
            
            if (frmDanhSach.IsVisible) lblStatus.Text = lblListTitle.Text;
            return;
        }

        // Ưu tiên hiển thị Tiêu đề cho thao tác click Ghim Bản Đồ (Chỉ có 1 quán)
        var currentSource = lstQuánFull.ItemsSource as IList<PoiModel>;
        if (currentSource != null && currentSource.Count == 1 && currentSource != _vinhKhanhPois && string.IsNullOrWhiteSpace(searchBar?.Text))
        {
            var poi = currentSource[0];
            lblListTitle.Text = _currentLang == "vi" ? $"THÔNG TIN: {poi.Name.ToUpper()}" :
                                _currentLang == "en" ? $"DETAILS: {poi.Name.ToUpper()}" :
                                _currentLang == "ja" ? $"詳細: {poi.Name.ToUpper()}" : 
                                $"详情: {poi.Name.ToUpper()}";
            if (frmDanhSach.IsVisible) lblStatus.Text = lblListTitle.Text;
            return;
        }

        // Ưu tiên Nearby
        if (_currentTab == "Nearby")
        {
            if (_currentLang == "en") lblListTitle.Text = "NEARBY RESTAURANTS:";
            else if (_currentLang == "ja") lblListTitle.Text = "近くのレストラン:";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = "附近餐厅:";
            else lblListTitle.Text = "TÌM CÁC QUÁN GẦN ĐÂY:";
            
            if (frmDanhSach.IsVisible) lblStatus.Text = lblListTitle.Text;
            return;
        }

        // Logic cũ cho danh sách bình thường
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
        
        if (frmDanhSach.IsVisible) lblStatus.Text = lblListTitle.Text;
    }

    private DateTime _lastSearchTypeTime = DateTime.MinValue;

    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        _lastSearchTypeTime = DateTime.Now; // KHOÁ BẢN ĐỒ LẠI ĐẾN KHI GÕ XONG!

        string keyword = string.IsNullOrWhiteSpace(e.NewTextValue) ? "" : e.NewTextValue.ToLower().Trim();

        // Chỉ xử lý phản hồi Tự động khi người dùng Bấm Xóa trắng ô Tìm kiếm (Click dấu X)
        // Còn khi họ đang gõ chữ Tiếng Việt có dấu, tuyệt đối không đụng vào thao tác lọc List để tránh văng dấu phím.
        if (string.IsNullOrEmpty(keyword))
        {
            // Restore previous tab view
            if (_currentTab == "Favorites")
            {
                lstQuánFull.ItemsSource = _vinhKhanhPois.Where(p => p.IsFavorite).ToList();
            }
            else if (_currentTab == "Nearby")
            {
                lstQuánFull.ItemsSource = _vinhKhanhPois.Where(p => p.DistanceInMeters <= p.Radius)
                                                        .OrderBy(p => p.DistanceInMeters)
                                                        .ToList();
            }
            else
            {
                lstQuánFull.ItemsSource = _vinhKhanhPois;
            }
            
            UpdateListTitle();
        }
    }

    private void OnSearchBarFocused(object sender, FocusEventArgs e)
    {
        _isSearchBarFocused = true;
    }

    private void OnSearchBarUnfocused(object sender, FocusEventArgs e)
    {
        _isSearchBarFocused = false;
    }

    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        string keyword = searchBar.Text?.ToLower().Trim() ?? "";

        if (!string.IsNullOrEmpty(keyword))
        {
            // Perform Search across all POIs (regardless of Tab)
            var filtered = _vinhKhanhPois.Where(p => 
                (p.Name != null && p.Name.ToLower().Contains(keyword)) || 
                (p.CategoryVi != null && p.CategoryVi.ToLower().Contains(keyword)) ||
                (p.Description != null && p.Description.ToLower().Contains(keyword))
            ).ToList();
            
            lstQuánFull.ItemsSource = filtered;

            // Bắt buộc hiện danh sách để xem kết quả
            frmDanhSach.IsVisible = true;
            
            UpdateListTitle();
            ScrollListToTop();
            
            // Tắt bàn phím sau khi Enter
            searchBar.Unfocus();
        }
    }

    // --- HÀM ẨN THẺ MENU / RESET DỮ LIỆU ---
    private void OnMapClicked(object sender, Microsoft.Maui.Controls.Maps.MapClickedEventArgs e)
    {
        // Khi người dùng bấm ra bãi đất trống trên Bản đồ -> Cất danh sách đi
        frmDanhSach.IsVisible = false;
        if (searchBar != null) searchBar.Text = string.Empty;
        
        // Hủy luôn TTS đang đọc nếu có
        _ttsCts?.Cancel();
    }

    private void OnCloseListOrClearFilterTapped(object sender, EventArgs e)
    {
        // Kiểm tra xem danh sách có đang bị lọc (bởi Search hoặc Pin hoặc Nearby) không?
        if (lstQuánFull.ItemsSource != _vinhKhanhPois && _currentTab != "Favorites")
        {
            // Trả lại toàn bộ quán (như nút Back)
            lstQuánFull.ItemsSource = _vinhKhanhPois;
            if (searchBar != null) searchBar.Text = string.Empty;
            _currentTab = "All";
            UpdateListTitle();
            ScrollListToTop();
        }
        else
        {
            // Nếu vốn đang là All hoặc Yêu thích đầy đủ, bấm X thì cụp thẻ xuống
            frmDanhSach.IsVisible = false;
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

    private async void OnMyLocationTapped(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location == null)
            {
                location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(5)
                });
            }

            if (location != null)
            {
                // Zoom bản đồ về vị trí người dùng
                mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Vị trí lỗi: {ex.Message}");
        }
    }

    private void OnPitchValueChanged(object sender, ValueChangedEventArgs e)
    {
        _currentPitch = (float)e.NewValue;
        
        string baseText = (_currentLang == "vi") ? "Độ vang giọng" : 
                          (_currentLang == "en") ? "Voice Pitch" : 
                          (_currentLang == "ja") ? "声域 (Pitch)" : "音高 (Pitch)";

        lblSettingsPitch.Text = $"{baseText}: {_currentPitch:F1}";
    }

    private void OnVolumeValueChanged(object sender, ValueChangedEventArgs e)
    {
        _currentVolume = (float)e.NewValue;
        
        string baseText = (_currentLang == "vi") ? "Âm lượng" : 
                          (_currentLang == "en") ? "Volume" : 
                          (_currentLang == "ja") ? "音量" : "音量";

        lblSettingsVolume.Text = $"{baseText}: {_currentVolume:P0}";
    }

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value) // Bật Dark Mode
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
            lblListTitle.TextColor = Color.FromArgb("#2ECC71"); // Giữ nguyên màu xanh của Tiêu đề
        }
        else // Tắt Dark Mode
        {
            Application.Current.UserAppTheme = AppTheme.Light;
            lblListTitle.TextColor = Color.FromArgb("#2ECC71");
        }
    }

    // --- HÀM NÚT BẤM TRÊN THẺ QUÁN ĂN ---
    private async void OnCardPlayClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PoiModel poi)
        {
            await PhatThuyetMinh(poi);
        }
    }

    private async void OnCardCallClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PoiModel poi)
        {
            if (string.IsNullOrWhiteSpace(poi.PhoneNumber) || poi.PhoneNumber == "Cập nhật sau")
            {
                await DisplayAlert("Thông báo", "Quán này chưa cập nhật số điện thoại.", "OK");
                return;
            }

            // Hiện hờn hoi Số điện thoại trên màn hình theo yêu cầu
            bool answer = await DisplayAlert("Gọi điện thoại", $"Bạn có muốn gọi cho quán {poi.Name} qua số:\n{poi.PhoneNumber}?", "Gọi ngay", "Hủy");
            
            if (answer)
            {
                try 
                {
                    if (Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.IsSupported)
                        Microsoft.Maui.ApplicationModel.Communication.PhoneDialer.Default.Open(poi.PhoneNumber);
                } 
                catch 
                {
                    await DisplayAlert("Cảnh báo", "Máy ảo không hỗ trợ gắn Sim để gọi điện thật!", "Đã hiểu");
                }
            }
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

            // MỚI: Thả Tym phát là lưu vĩnh viễn vào Database SQLite
            _ = _dbService.UpdatePoiAsync(poi);
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
                // BUG FIX: .NET MAUI Android soft-keyboard composition drops characters if Map updates layout or moves region
                // KHOÁ BẢN ĐỒ: Nếu người dùng vừa gõ bàn phím trong 3 giây qua, BỎ QUA NGAY việc Update bản đồ!
                if ((DateTime.Now - _lastSearchTypeTime).TotalSeconds < 3 || _isSearchBarFocused)
                {
                    return; // Đóng băng bản đồ, ưu tiên bàn phím Tiếng Việt!
                }

                mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(200)));
                
                // Vẽ vòng tròn quét (bán kính quét mặc định 30m)
                if (_userScanCircle == null)
                {
                    _userScanCircle = new Microsoft.Maui.Controls.Maps.Circle
                    {
                        Center = location,
                        Radius = Distance.FromMeters(30),
                        StrokeColor = Color.FromArgb("#882ECC71"), // Viền xanh lá mờ
                        StrokeWidth = 8,
                        FillColor = Color.FromArgb("#332ECC71") // Lõi xanh lá rất mờ
                    };
                    mapVinhKhanh.MapElements.Add(_userScanCircle);
                }
                else
                {
                    // Di chuyển vòng tròn đi theo người dùng
                    _userScanCircle.Center = location;
                }

                // 1. CẬP NHẬT KHOẢNG CÁCH CHUẨN XÁC CHO MỌI QUÁN TRƯỚC
                foreach (var p in _vinhKhanhPois)
                {
                    p.DistanceInMeters = location.CalculateDistance(new Location(p.Latitude, p.Longitude), DistanceUnits.Kilometers) * 1000;
                }

                // 2. SAU ĐÓ MỚI XÉT TRIGGER (Để tránh lỗi filter mảng lấy khoảng cách cũ)
                bool foundAny = false;
                foreach (var poi in _vinhKhanhPois)
                {
                    if (poi.DistanceInMeters <= poi.Radius)
                    {
                        foundAny = true;
                        // Chỉ đọc 1 lần duy nhất khi vừa bước vào vùng an toàn của quán
                        if (!poi.HasAutoPlayed)
                        {
                            poi.HasAutoPlayed = true;
                            
                            // Tự động đẩy danh sách Gần Đây lên giao diện thay vì chỉ hiện 1 Quán
                            _currentTab = "Nearby";
                            // Chỉnh bán kính quét Menu = Đúng bằng bán kính kích hoạt âm thanh (poi.Radius)
                            var nearbyPois = _vinhKhanhPois.Where(p => p.DistanceInMeters <= poi.Radius)
                                                           .OrderBy(p => p.DistanceInMeters)
                                                           .ToList();
                            lstQuánFull.ItemsSource = nearbyPois;
                            if (searchBar != null) searchBar.Text = string.Empty;
                            frmDanhSach.IsVisible = true;
                            UpdateListTitle();
                            ScrollListToTop();

                            await PhatThuyetMinh(poi);
                        }
                    }
                    else if (poi.DistanceInMeters > poi.Radius + 30) // Offset chống nhảy sóng GPS, đi đủ xa mới Reset
                    {
                        // Reset cờ nếu người dùng đã rời khỏi quán đủ xa
                        poi.HasAutoPlayed = false;
                    }
                }
                statusDot.Fill = foundAny ? Colors.Green : Colors.Red;
            });
        });
    }

#if ANDROID
    private class CustomMapCallback : Java.Lang.Object, Android.Gms.Maps.IOnMapReadyCallback
    {
        public void OnMapReady(Android.Gms.Maps.GoogleMap googleMap)
        {
            // Tắt nút vuông định vị mặc định của Android nhưng giữ lại hình dấu chấm xanh dương
            googleMap.UiSettings.MyLocationButtonEnabled = false;
        }
    }
#endif
}