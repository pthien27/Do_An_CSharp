
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
    private bool _isSequentialReading = false; // Biến khóa cờ thao tác đọc tuần tự
    private List<PoiModel> _lastNearbyResults; // Ghi nhớ danh sách quán gần đây mới nhất
    private PoiModel _currentlyNarratingPoi;   // Quán đang được thuyết minh tự động
    private Microsoft.Maui.Controls.Maps.Circle _spotlightCircle; // Vòng tròn Spotlight cho quán đang thuyết minh
    
    // Biến lưu trạng thái tạm thời (Snapshot) để khôi phục nếu không nhấn "Lưu"
    private string _snapLang;
    private float _snapPitch;
    private float _snapVolume;
    private AppTheme _snapTheme;
    private string _snapTab;
    private bool _isConfirmedSave = false;
    private float _tempPitch;
    private float _tempVolume;
    private string _nextLang = "vi"; // Ngôn ngữ đang chọn thử trong menu

    // [UC1 - Xem Bản Đồ & Danh Sách Quán: Khởi tạo dữ liệu SQLite lên bản đồ]
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
        
        // MỚI: Đưa ống kính Camera của Bản đồ về thẳng tọa độ MỚI của Ốc Oanh để khách thấy ngay
        var vinhKhanhCenter = new Location(10.761204237032537, 106.703307923906);
        mapVinhKhanh.MoveToRegion(MapSpan.FromCenterAndRadius(vinhKhanhCenter, Distance.FromKilometers(0.5)));

        lstAll.ItemsSource = _vinhKhanhPois;

        // Tiến hành nạp dữ liệu từ SQLite lên và gắn pin vào Map
        _ = LoadDatabaseAndStartAsync();
    }

    private void SwitchToList(CollectionView listToShow)
    {
        lstAll.IsVisible = (listToShow == lstAll);
        lstFav.IsVisible = (listToShow == lstFav);
        lstFocus.IsVisible = (listToShow == lstFocus);
    }

    private CollectionView GetActiveList()
    {
        if (lstFocus.IsVisible) return lstFocus;
        if (lstFav.IsVisible) return lstFav;
        return lstAll;
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

    // [UC5 - Đổi Ngôn Ngữ Giao Diện/Script: Thay đổi State biến Cờ _currentLang]
    // --- HÀM XỬ LÝ CHỌN NGÔN NGỮ ---
    private void OnLanguageViTapped(object sender, EventArgs e)
    {
        _nextLang = "vi";
        UpdateSettingsMenuVisuals();
    }

    private void OnLanguageEnTapped(object sender, EventArgs e)
    {
        _nextLang = "en";
        UpdateSettingsMenuVisuals();
    }

    private void OnLanguageJaTapped(object sender, EventArgs e)
    {
        _nextLang = "ja";
        UpdateSettingsMenuVisuals();
    }

    private void OnLanguageZhTapped(object sender, EventArgs e)
    {
        _nextLang = "zh-CN";
        UpdateSettingsMenuVisuals();
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
        UpdateSettingsMenuVisuals();
        
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
            if (btnSaveSettings != null) btnSaveSettings.Text = "SAVE SETTINGS";
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
            if (btnSaveSettings != null) btnSaveSettings.Text = "設定を保存";
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
            if (btnSaveSettings != null) btnSaveSettings.Text = "保存设置";
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
            if (btnSaveSettings != null) btnSaveSettings.Text = "LƯU CÀI ĐẶT";
        }

        // Dịch thuật nút Tìm quán gần đây
        if (btnGanDayInsideList != null)
        {
            if (_currentLang == "en") btnGanDayInsideList.Text = "📍 FIND NEARBY RESTAURANTS";
            else if (_currentLang == "ja") btnGanDayInsideList.Text = "📍 近くの店を探す";
            else if (_currentLang.StartsWith("zh")) btnGanDayInsideList.Text = "📍 查找附近餐厅";
            else btnGanDayInsideList.Text = "📍 TÌM QUÁN GẦN ĐÂY";
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
                translated = !string.IsNullOrWhiteSpace(poi.DescriptionEn) ? poi.DescriptionEn : poi.Description;
                open = "Open"; close = "Closes at"; play = "Play"; call = "Call"; save = poi.IsFavorite ? "Saved" : "Save";
                category = (poi.CategoryVi == "Quán ốc") ? "Seafood Restaurant" : "System";
            }
            else if (_currentLang == "ja")
            {
                translated = !string.IsNullOrWhiteSpace(poi.DescriptionJa) ? poi.DescriptionJa : poi.Description;
                open = "営業中"; close = "営業時間終了"; play = "再生"; call = "電話"; save = poi.IsFavorite ? "保存済み" : "保存";
                category = (poi.CategoryVi == "Quán ốc") ? "シーフードレストラン" : "システム";
            }
            else if (_currentLang.StartsWith("zh"))
            {
                translated = !string.IsNullOrWhiteSpace(poi.DescriptionZh) ? poi.DescriptionZh : poi.Description;
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

    private void UpdateSettingsMenuVisuals()
    {
        // Cập nhật màu nút bấm trong Side Menu theo ngôn ngữ ĐANG CHỌN (_nextLang)
        btnMenuVi.BackgroundColor = _nextLang == "vi" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuVi.TextColor = _nextLang == "vi" ? Colors.White : Colors.Gray;

        btnMenuEn.BackgroundColor = _nextLang == "en" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuEn.TextColor = _nextLang == "en" ? Colors.White : Colors.Gray;

        btnMenuJa.BackgroundColor = _nextLang == "ja" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuJa.TextColor = _nextLang == "ja" ? Colors.White : Colors.Gray;

        btnMenuZh.BackgroundColor = _nextLang == "zh-CN" ? Color.FromArgb("#2ECC71") : Color.FromArgb("#F0F0F0");
        btnMenuZh.TextColor = _nextLang == "zh-CN" ? Colors.White : Colors.Gray;
    }

    // [UC4 - Nghe Thuyết Minh Âm Thanh (TTS): Khởi tạo giọng đọc Text To Speech]
    // --- HÀM THUYẾT MINH ĐA NGÔN NGỮ ---
    private async Task PhatThuyetMinh(PoiModel poi, bool isManual = true)
    {
        // Chỉ khi phát THỦ CÔNG (bấm nút), ta mới ngắt tiến trình đọc tuần tự
        if (isManual) _isSequentialReading = false;

        // 1. Hủy tiến trình đọc cũ nếu nó đang đọc
        _ttsCts?.Cancel();
        // 2. Tạo một tiến trình hủy mới
        _ttsCts = new CancellationTokenSource();

        string noidung = poi.Description;
        string ttsLang = _currentLang; // Mã ngôn ngữ dùng cho bộ loa (mặc định là ngôn ngữ đang chọn)

        // Đọc dựa trên Dữ liệu Database offline
        if (_currentLang == "en")
        {
            noidung = !string.IsNullOrWhiteSpace(poi.DescriptionEn) ? poi.DescriptionEn : poi.Description;
            if (string.IsNullOrWhiteSpace(noidung) || noidung == poi.Description) ttsLang = "vi"; // Rớt về mốc Tiếng Việt
        }
        else if (_currentLang == "ja")
        {
            noidung = !string.IsNullOrWhiteSpace(poi.DescriptionJa) ? poi.DescriptionJa : poi.Description;
            if (string.IsNullOrWhiteSpace(noidung) || noidung == poi.Description) ttsLang = "vi";
        }
        else if (_currentLang.StartsWith("zh"))
        {
            noidung = !string.IsNullOrWhiteSpace(poi.DescriptionZh) ? poi.DescriptionZh : poi.Description;
            if (string.IsNullOrWhiteSpace(noidung) || noidung == poi.Description) ttsLang = "vi";
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
                e.HideInfoWindow = true; // Ẩn bong bóng trắng hiện tên quán mặc định
                statusDot.Fill = Colors.Green;
                
                if (_currentLang == "vi") lblStatus.Text = $"Đang thuyết minh: {poi.Name}";
                else if (_currentLang == "en") lblStatus.Text = $"Narrating: {poi.Name}";
                else if (_currentLang == "ja") lblStatus.Text = $"説明中: {poi.Name}";
                else lblStatus.Text = $"解说中: {poi.Name}";
                
                // MỚI: Chỉ hiển thị thẻ thông tin của quán này trên danh sách
                _currentTab = "AutoVoice"; // Chuyển Tab trạng thái để UI đồng bộ
                UpdateTabAesthetics();    // Làm sáng menu Quán ăn
                
                SwitchToList(lstFocus);
                lstFocus.ItemsSource = new List<PoiModel> { poi };
                if (searchBar != null) searchBar.Text = string.Empty;
                frmDanhSach.IsVisible = true;
                UpdateListTitle();
                
                // Khi xem chi tiết 1 quán mới thì vẫn nên Scroll lên đầu của list Focus
                lstFocus.ScrollTo(0, position: ScrollToPosition.Start, animate: false);

                // MỚI: Chỉ hiển thị Spotlight khi nhấn ghim (Bỏ Zoom theo yêu cầu)
                var location = new Location(poi.Latitude, poi.Longitude);
                if (_spotlightCircle != null) mapVinhKhanh.MapElements.Remove(_spotlightCircle);
                _spotlightCircle = new Microsoft.Maui.Controls.Maps.Circle
                {
                    Center = location,
                    Radius = Distance.FromMeters(8),
                    StrokeColor = Color.FromArgb("#FF2ECC71"),
                    StrokeWidth = 12,
                    FillColor = Color.FromArgb("#552ECC71")
                };
                mapVinhKhanh.MapElements.Add(_spotlightCircle);

                await PhatThuyetMinh(poi);
            };
            mapVinhKhanh.Pins.Add(pin);
        }
    }

    // MỚI: Phím bấm độc lập cho Danh sách Tất Cả Quán Ăn
    private void OnShowRestaurantListTapped(object sender, EventArgs e)
    {
        // Nếu ĐANG từ Tab khác (Yêu thích/QR/...) quay lại Tab Quán ăn
        if (_currentTab != "All" && _currentTab != "Nearby" && _currentTab != "AutoVoice")
        {
            // CHỈ QUAY LẠI FOCUS NẾU ĐANG PHÁT THUYẾT MINH
            if (_currentlyNarratingPoi != null || _isSequentialReading)
            {
                if (_lastNearbyResults != null && _lastNearbyResults.Any())
                {
                    _currentTab = "Nearby";
                    SwitchToList(lstFocus);
                    lstFocus.ItemsSource = _lastNearbyResults;
                }
                else if (_currentlyNarratingPoi != null)
                {
                    _currentTab = "AutoVoice";
                    SwitchToList(lstFocus);
                    lstFocus.ItemsSource = new List<PoiModel> { _currentlyNarratingPoi };
                    
                    if (_currentLang == "vi") lblStatus.Text = $"Đang thuyết minh: {_currentlyNarratingPoi.Name}";
                    else if (_currentLang == "en") lblStatus.Text = $"Narrating: {_currentlyNarratingPoi.Name}";
                    else if (_currentLang == "ja") lblStatus.Text = $"説明中: {_currentlyNarratingPoi.Name}";
                    else lblStatus.Text = $"解说中: {_currentlyNarratingPoi.Name}";
                }
            }
            else
            {
                // MẶC ĐỊNH: Nếu không có gì đang phát, quay về danh sách All
                _currentTab = "All";
                SwitchToList(lstAll);
            }
            
            frmDanhSach.IsVisible = true;
            if (searchBar != null) searchBar.Text = string.Empty;
            UpdateListTitle();
        }
        else
        {
            // Nếu vốn đang ở chính nó thì Toggle ẩn/hiện
            frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
            UpdateListTitle();
        }
        UpdateTabAesthetics(); // Cập nhật màu sắc Tab
    }

    // MỚI: Phím bấm độc lập cho Tính năng Quán Gần Đây
    private void OnShowNearbyTapped(object sender, EventArgs e)
    {
        bool isNearAtLeastOne = _vinhKhanhPois.Any(p => p.DistanceInMeters <= 50); // Cố định mốc tìm gần 50 mét
        
        if (isNearAtLeastOne)
        {
            if (_currentTab != "Nearby")
            {
                _currentTab = "Nearby";
                var nearbyPois = _vinhKhanhPois.Where(p => p.DistanceInMeters <= 50)
                                               .OrderBy(p => p.DistanceInMeters)
                                               .ToList();
                _lastNearbyResults = nearbyPois; // Ghi nhớ để X có thể quay lại
                
                SwitchToList(lstFocus);
                lstFocus.ItemsSource = nearbyPois;

                frmDanhSach.IsVisible = true;
                if (searchBar != null) searchBar.Text = string.Empty;
                UpdateListTitle();
                lstFocus.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
            }
            else
            {
                frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
            }
        }
        else
        {
            // Nếu không có quán nào gần, cảnh báo bằng Alert
            if (_currentLang == "vi") DisplayAlert("Thông báo", "Bạn chưa đến gần khu vực quán ẩm thực nào!", "ĐÓNG");
            else if (_currentLang == "en") DisplayAlert("Notice", "You are not near any culinary places!", "CLOSE");
            else DisplayAlert("Chú ý", "Không có quán lân cận.", "ĐÓNG");
        }

        UpdateListTitle();
        UpdateTabAesthetics();
    }

    private void OnShowFavoritesTapped(object sender, EventArgs e)
    {
        // SỬA: Kiểm tra nếu Tab hiện tại KHÔNG PHẢI là Favorites thì mới thực hiện chuyển Tab
        if (_currentTab != "Favorites")
        {
            _currentTab = "Favorites";
            SwitchToList(lstFav);

            // TỐI ƯU: Chỉ nạp dữ liệu nếu danh sách đang trống (lần đầu tiên mở app)
            // Việc cập nhật dữ liệu khi bấm 'Lưu' đã được xử lý trong hàm OnCardSaveClicked
            if (lstFav.ItemsSource == null)
            {
                var favorites = _vinhKhanhPois.Where(p => p.IsFavorite).ToList();
                lstFav.ItemsSource = favorites;
            }

            // KHÔI PHỤC LOGIC: Nếu bạn đang nghe thuyết minh một quán yêu thích THỦ CÔNG (không phải tự động quét gần đây)
            // khi quay lại menu Yêu thích nó phải hiện đúng cái thẻ tập trung (lstFocus) của quán đó.
            if (_currentlyNarratingPoi != null && _currentlyNarratingPoi.IsFavorite && !_isSequentialReading)
            {
                SwitchToList(lstFocus);
                lstFocus.ItemsSource = new List<PoiModel> { _currentlyNarratingPoi };
            }

            frmDanhSach.IsVisible = true;
            if (searchBar != null) searchBar.Text = string.Empty; // Xóa tìm kiếm khi chuyển Tab
        }
        else
        {
            // Nếu vốn đã ở Tab Favorites rồi thì mới thực hiện ẩn/hiện (Toggle)
            frmDanhSach.IsVisible = !frmDanhSach.IsVisible;
        }

        UpdateListTitle();
        UpdateTabAesthetics(); // Cập nhật màu sắc Tab
    }

    private void ScrollListToTop()
    {
        MainThread.BeginInvokeOnMainThread(async () => {
            try {
                await Task.Delay(50); // Đợi Frame render xong
                var activeList = GetActiveList();
                var list = activeList.ItemsSource as System.Collections.IList;
                if (list != null && list.Count > 0)
                {
                    activeList.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
                }
            } catch { } // Bỏ qua lỗi an toàn
        });
    }

    private void UpdateListTitle()
    {
        var activeList = GetActiveList();
        var currentSource = activeList.ItemsSource as IList<PoiModel>;

        // 1. Ưu tiên số 1: Nếu danh sách chỉ có DUY NHẤT 1 quán (Chế độ xem chi tiết)
        // Hiển thị tiêu đề "THÔNG TIN: ..." ở bất kỳ Tab nào (Quán ăn, Yêu thích, Gần đây)
        if (currentSource != null && currentSource.Count == 1 && activeList == lstFocus && string.IsNullOrWhiteSpace(searchBar?.Text))
        {
            var poi = currentSource[0];
            lblListTitle.Text = _currentLang == "vi" ? $"THÔNG TIN: {poi.Name.ToUpper()}" :
                                 _currentLang == "en" ? $"DETAILS: {poi.Name.ToUpper()}" :
                                 _currentLang == "ja" ? $"詳細: {poi.Name.ToUpper()}" : 
                                 $"详情: {poi.Name.ToUpper()}";
        }
        // 2. Ưu tiên số 2: Tiêu đề Tìm Kiếm nếu có chữ trong ô Search
        else if (searchBar != null && !string.IsNullOrWhiteSpace(searchBar.Text))
        {
            var resultsCount = (activeList.ItemsSource as System.Collections.IList)?.Count ?? 0;
            if (_currentLang == "en") lblListTitle.Text = $"SEARCH RESULTS ({resultsCount})";
            else if (_currentLang == "ja") lblListTitle.Text = $"検索結果 ({resultsCount})";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = $"搜索结果 ({resultsCount})";
            else lblListTitle.Text = $"KẾT QUẢ TÌM KIẾM ({resultsCount})";
        }
        // 3. Tab Nearby
        else if (_currentTab == "Nearby")
        {
            if (_currentLang == "en") lblListTitle.Text = "NEARBY RESTAURANTS:";
            else if (_currentLang == "ja") lblListTitle.Text = "近くのレストラン:";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = "附近餐厅:";
            else lblListTitle.Text = "TÌM CÁC QUÁN GẦN ĐÂY:";
        }
        // 4. Tab Favorites
        else if (_currentTab == "Favorites")
        {
            if (_currentLang == "en") lblListTitle.Text = "FAVORITE RESTAURANTS";
            else if (_currentLang == "ja") lblListTitle.Text = "お気に入りのレストラン";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = "收藏的餐厅";
            else lblListTitle.Text = "DANH SÁCH YÊU THÍCH";
        }
        // 5. Mặc định (All)
        else
        {
            if (_currentLang == "en") lblListTitle.Text = "RESTAURANTS ON VINH KHANH STREET";
            else if (_currentLang == "ja") lblListTitle.Text = "ヴィンカン通りのレストラン";
            else if (_currentLang.StartsWith("zh")) lblListTitle.Text = "永庆街上的餐厅";
            else lblListTitle.Text = "QUÁN ĂN TRÊN PHỐ VĨNH KHÁNH";
        }
        
        // --- LOGIC ẨN/HIỆN NÚT [X] THEO YÊU CẦU ---
        bool showX = true;
        // Nếu không tìm kiếm (SearchBar trống)
        if (string.IsNullOrWhiteSpace(searchBar?.Text))
        {
            // ẨN nút X nếu đang ở danh sách tổng (ALL) hoặc danh sách Yêu thích đầy đủ
            if ((_currentTab == "All" || _currentTab == "Favorites") && currentSource != null && currentSource.Count > 1)
            {
                showX = false;
            }
        }
        lblCloseList.IsVisible = showX;

        if (frmDanhSach.IsVisible && !_isSequentialReading) 
        {
            lblStatus.Text = lblListTitle.Text;
        }
        
        // MỚI: Chỉ hiển thị Nút "Vị trí gần đây" nếu đang ở màn hình Danh sách Mặc Định (All) và KHÔNG tìm kiếm, KHÔNG xem chi tiết 1 quán
        btnGanDayInsideList.IsVisible = (_currentTab == "All" && string.IsNullOrWhiteSpace(searchBar?.Text) && (currentSource == null || currentSource.Count > 1 || currentSource == _vinhKhanhPois));
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
                SwitchToList(lstFav);
            }
            else if (_currentTab == "Nearby")
            {
                SwitchToList(lstFocus);
            }
            else
            {
                SwitchToList(lstAll);
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

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    // [UC2 - Tìm Kiếm Quán Ăn / Món Ăn: Chống văng dấu và bỏ dấu Tiếng Việt]
    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        string keyword = searchBar.Text?.Trim() ?? "";

        if (!string.IsNullOrEmpty(keyword))
        {
            string keywordNoDiacritics = RemoveDiacritics(keyword).ToLower();

            // Perform Search across all POIs (regardless of Tab)
            var filtered = _vinhKhanhPois.Where(p => 
                (p.Name != null && RemoveDiacritics(p.Name).ToLower().Contains(keywordNoDiacritics)) || 
                (p.CategoryVi != null && RemoveDiacritics(p.CategoryVi).ToLower().Contains(keywordNoDiacritics)) ||
                (p.Description != null && RemoveDiacritics(p.Description).ToLower().Contains(keywordNoDiacritics))
            ).ToList();

            if (filtered.Any())
            {
                var target = filtered.First();
                
                // MỚI: Chỉ hiện Spotlight quán được tìm thấy (Bỏ Zoom theo yêu cầu)
                var location = new Location(target.Latitude, target.Longitude);

                if (_spotlightCircle != null) mapVinhKhanh.MapElements.Remove(_spotlightCircle);
                _spotlightCircle = new Microsoft.Maui.Controls.Maps.Circle
                {
                    Center = location,
                    Radius = Distance.FromMeters(8),
                    StrokeColor = Color.FromArgb("#FF2ECC71"),
                    StrokeWidth = 12,
                    FillColor = Color.FromArgb("#552ECC71")
                };
                mapVinhKhanh.MapElements.Add(_spotlightCircle);

                // Tắt bàn phím sau khi Enter
                searchBar?.Unfocus();

                // GIỐNG HỆT NHƯ CLICK GHIM: Bật trạng thái Thuyết minh
                statusDot.Fill = Colors.Green;
                if (_currentLang == "vi") lblStatus.Text = $"Đang thuyết minh: {target.Name}";
                else if (_currentLang == "en") lblStatus.Text = $"Narrating: {target.Name}";
                else if (_currentLang == "ja") lblStatus.Text = $"説明中: {target.Name}";
                else lblStatus.Text = $"解说中: {target.Name}";

                // Ép chỉ nạp đúng 1 quán để UI hiển thị chuẩn thẻ THÔNG TIN: TÊN QUÁN
                _currentTab = "AutoVoice";
                UpdateTabAesthetics();

                _currentlyNarratingPoi = target; // Ghi nhớ quán đang chiếu
                SwitchToList(lstFocus);
                lstFocus.ItemsSource = new List<PoiModel> { target };
                
                frmDanhSach.IsVisible = true;
                
                UpdateListTitle();
                lstFocus.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
                
                // Chơi luôn Audio thuyết minh (Đây là phát thủ công do tìm kiếm)
                await PhatThuyetMinh(target, true);
            }
            else
            {
                if (_currentLang == "vi") lblStatus.Text = "❌ Không tìm thấy quán!";
                else if (_currentLang == "en") lblStatus.Text = "❌ No results found!";
                else if (_currentLang == "ja") lblStatus.Text = "❌ 結果が見つかりません";
                else lblStatus.Text = "❌ 未找到结果";
                
                searchBar?.Unfocus();
            }
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
        _currentlyNarratingPoi = null; // Xóa trạng thái focus

        // Xóa Spotlight nếu đang hiện
        if (_spotlightCircle != null)
        {
            mapVinhKhanh.MapElements.Remove(_spotlightCircle);
            _spotlightCircle = null;
        }
    }

    private void OnCloseListOrClearFilterTapped(object sender, EventArgs e)
    {
        var activeList = GetActiveList();

        // 1. Kiểm tra nếu đang ở Tab Favorites
        if (_currentTab == "Favorites")
        {
            // Nếu đang xem CHI TIẾT 1 quán (đang ở list Focus)
            if (activeList == lstFocus)
            {
                // Quay trở lại danh sách yêu thích chính
                SwitchToList(lstFav);
                UpdateListTitle();
            }
            else
            {
                // Nếu đã là danh sách đầy đủ, bấm X sẽ đóng menu
                frmDanhSach.IsVisible = false;
            }
            return;
        }

        // 2. Nếu đang ở màn hình Focus (Tìm kiếm hoặc Thuyết minh)
        if (activeList == lstFocus)
        {
            // Kiểm tra: Nếu trước đó có lịch sử Quét quán gần đây thì ưu tiên quay về danh sách đó
            if (_lastNearbyResults != null && _lastNearbyResults.Any() && _currentTab != "Nearby")
            {
                _currentTab = "Nearby";
                lstFocus.ItemsSource = _lastNearbyResults;
                if (searchBar != null) searchBar.Text = string.Empty;
                UpdateListTitle();
                lstFocus.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
            }
            else
            {
                // Nếu không có lịch sử gần đây, hoặc đang ở chính tab Nearby rồi bấm X lần nữa thì reset hoàn toàn về All
                _currentTab = "All";
                SwitchToList(lstAll);
                if (searchBar != null) searchBar.Text = string.Empty;
                _lastNearbyResults = null; // Xóa lịch sử khi thực sự muốn thoát hẳn về All
                UpdateListTitle();
            }

            UpdateTabAesthetics();

            // Dọn dẹp các hiệu ứng đang chạy
            _isSequentialReading = false;
            _currentlyNarratingPoi = null;
            _ttsCts?.Cancel();
            if (_spotlightCircle != null)
            {
                mapVinhKhanh.MapElements.Remove(_spotlightCircle);
                _spotlightCircle = null;
            }
        }
        else
        {
            // Nếu vốn đang là All đầy đủ, bấm X thì cụp thẻ xuống
            frmDanhSach.IsVisible = false;
        }
    }

    private async void OnXemThemTapped(object sender, EventArgs e)
    {
        _snapTab = _currentTab; // Lưu lại tab trước khi vào Settings
        _currentTab = "More";
        UpdateTabAesthetics();

        // Chụp ảnh trạng thái hiện tại trước khi thay đổi (Nguyên bản của bạn)
        _snapLang = _currentLang;
        _snapPitch = _currentPitch;
        _snapVolume = _currentVolume;
        _snapTheme = Application.Current.UserAppTheme;
        _isConfirmedSave = false;
        
        // Khởi tạo biến tạm cho thanh trượt
        _tempPitch = _currentPitch;
        _tempVolume = _currentVolume;
        _nextLang = _currentLang; // Khởi tạo ngôn ngữ nháp bằng ngôn ngữ hiện tại
        UpdateSettingsMenuVisuals(); // Cập nhật màu xanh cho nút ngay lập tức

        bgOverlay.IsVisible = true;
        _ = bgOverlay.FadeTo(0.4, 250);
        await frmSettings.TranslateTo(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseSettingsTapped(object sender, EventArgs e)
    {
        // Nếu không phải là nhấn nút Lưu (ví dụ bấm dấu X hoặc bấm ra ngoài nền đen)
        // thì khôi phục lại trạng thái cũ
        if (!_isConfirmedSave)
        {
            _currentLang = _snapLang;
            _currentPitch = _snapPitch;
            _currentVolume = _snapVolume;
            _currentTab = _snapTab; // Khôi phục tab cũ
            UpdateTabAesthetics();  // Cập nhật lại màu sắc tab cũ
            
            // Chỉ đồng bộ lại giao diện menu cho lần mở sau
            sliderPitch.Value = _currentPitch;
            sliderVolume.Value = _currentVolume;
            swDarkMode.IsToggled = (_snapTheme == AppTheme.Dark);
        }

        _ = bgOverlay.FadeTo(0, 250);
        await frmSettings.TranslateTo(280, 0, 300, Easing.CubicIn);
        bgOverlay.IsVisible = false;
    }

    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        // Loại bỏ việc gán trực tiếp Application.Current.UserAppTheme tại đây
        // Việc này sẽ được xử lý khi nhấn nút LƯU
    }

    private async void OnSaveSettingsClicked(object sender, EventArgs e)
    {
        // 1. Xác nhận lưu
        _isConfirmedSave = true;

        // 2. Chốt ngôn ngữ mới từ lựa chọn nháp
        _currentLang = _nextLang;

        // 3. Áp dụng Giao diện tối/sáng theo trạng thái Toggle hiện tại
        Application.Current.UserAppTheme = swDarkMode.IsToggled ? AppTheme.Dark : AppTheme.Light;

        // 3. Cập nhật Âm lượng và Độ vang thật từ biến tạm
        _currentPitch = _tempPitch;
        _currentVolume = _tempVolume;

        // 4. Cập nhật toàn bộ giao diện app theo các thiết lập mới
        await UpdateStaticUI();

        // 5. Dừng mọi thuyết minh cũ và đưa App về trạng thái ban đầu
        ResetToMainView();

        // 6. Đóng bảng cài đặt
        OnCloseSettingsTapped(sender, e);
    }

    private void ResetToMainView()
    {
        // Hủy TTS nếu đang đọc
        _ttsCts?.Cancel();
        _isSequentialReading = false;

        // Xóa Spotlight
        if (_spotlightCircle != null)
        {
            mapVinhKhanh.MapElements.Remove(_spotlightCircle);
            _spotlightCircle = null;
        }

        // Khôi phục lại Tab mà người dùng đã đứng trước khi vào Settings (thay vì ép về All)
        _currentTab = _snapTab;
        
        if (_currentTab == "Favorites")
        {
            SwitchToList(lstFav);
        }
        else if (_currentTab == "Nearby" || _currentTab == "AutoVoice")
        {
            if (_lastNearbyResults != null && _lastNearbyResults.Any())
            {
                _currentTab = "Nearby";
                SwitchToList(lstFocus);
                lstFocus.ItemsSource = _lastNearbyResults;
            }
            else
            {
                _currentTab = "All";
                SwitchToList(lstAll);
            }
        }
        else
        {
            _currentTab = "All";
            SwitchToList(lstAll);
        }

        if (searchBar != null) searchBar.Text = string.Empty;
        
        UpdateListTitle();
        UpdateTabAesthetics(); // Sáng lại icon Tab chuẩn xác
        ScrollListToTop();

        // Hiện danh sách menu lại
        frmDanhSach.IsVisible = true; 
        
        if (_currentLang == "vi") lblStatus.Text = "Đã lưu thiết lập thành công";
        else if (_currentLang == "en") lblStatus.Text = "Settings saved successfully";
        else if (_currentLang == "ja") lblStatus.Text = "設定が正常に保存されました";
        else lblStatus.Text = "设置已成功保存";

        // MỚI: Tự động quay lại tiêu đề chính sau 3 giây
        _ = Task.Run(async () => {
            await Task.Delay(3000);
            MainThread.BeginInvokeOnMainThread(() => {
                // Chỉ cập nhật lại nếu người dùng chưa chuyển sang thao tác khác (vẫn đang ở tab All)
                if (_currentTab == "All" || _currentTab == "Nearby") {
                    UpdateListTitle();
                }
            });
        });
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
        _tempPitch = (float)e.NewValue;
        
        string baseText = (_currentLang == "vi") ? "Độ vang giọng" : 
                          (_currentLang == "en") ? "Voice Pitch" : 
                          (_currentLang == "ja") ? "声域 (Pitch)" : "音高 (Pitch)";

        lblSettingsPitch.Text = $"{baseText}: {_tempPitch:F1}";
    }

    private void OnVolumeValueChanged(object sender, ValueChangedEventArgs e)
    {
        _tempVolume = (float)e.NewValue;
        
        string baseText = (_currentLang == "vi") ? "Âm lượng" : 
                          (_currentLang == "en") ? "Volume" : 
                          (_currentLang == "ja") ? "音量" : "音量";

        lblSettingsVolume.Text = $"{baseText}: {_tempVolume:P0}";
    }


    // --- HÀM NÚT BẤM TRÊN THẺ QUÁN ĂN ---
    private async void OnCardPlayClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is PoiModel poi)
        {
            _currentlyNarratingPoi = poi; // Ghi nhớ quán đang đọc thủ công

            // MỚI: Luôn hiện giao diện chi tiết (1 quán) bất kể đang ở Tab nào
            SwitchToList(lstFocus);
            lstFocus.ItemsSource = new List<PoiModel> { poi };
            UpdateListTitle();

            // HIỆN SPOTLIGHT TRÊN BẢN ĐỒ
            var location = new Location(poi.Latitude, poi.Longitude);
            if (_spotlightCircle != null) mapVinhKhanh.MapElements.Remove(_spotlightCircle);
            _spotlightCircle = new Microsoft.Maui.Controls.Maps.Circle
            {
                Center = location,
                Radius = Distance.FromMeters(8),
                StrokeColor = Color.FromArgb("#FF2ECC71"),
                StrokeWidth = 12,
                FillColor = Color.FromArgb("#552ECC71")
            };
            mapVinhKhanh.MapElements.Add(_spotlightCircle); // Thêm vòng spotlight vào bản đồ

            // PHÁT THUYẾT MINH
            await PhatThuyetMinh(poi, true);
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

            // Cập nhật lại nội dung lstFav nếu nó đang hiện hoặc nếu cần đồng bộ
            var favorites = _vinhKhanhPois.Where(p => p.IsFavorite).ToList();
            lstFav.ItemsSource = favorites;
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

    // [UC6 - Đo Lường Vị Trí GPS (Geofencing): Vòng lặp tracking thiết bị mỗi 1s tính toán Haversine]
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

                bool foundAny = _vinhKhanhPois.Any(p => p.DistanceInMeters <= p.Radius);
                statusDot.Fill = foundAny ? Colors.Green : Colors.Red;

                // 2. Tắt chế độ Tự Động Thuyết Minh. Nhường quyền quyết định lại cho người dùng khi bấm Nút "Tìm Quán Gần Đây"
            });
        });
    }

    // MỚI: Tính năng TÌM VÀ THUYẾT MINH THỦ CÔNG khi bấm nút trên giao diện
    private async void OnAutoNarrateNearbyClicked(object sender, EventArgs e)
    {
        if (_isSequentialReading) 
        {
            // Tránh bấm nhiều lần chồng âm thanh
            await DisplayAlert("Thông báo", "Hệ thống đang đọc tiến trình trước, vui lòng đợi!", "Đóng");
            return; 
        }

        var poisToRead = _vinhKhanhPois
            .Where(p => p.DistanceInMeters <= 50) // Quét trong 50 mét
            .OrderBy(p => p.DistanceInMeters)
            .ToList();

        if (poisToRead.Any())
        {
            _isSequentialReading = true; 
            _lastNearbyResults = poisToRead; // Ghi nhớ kết quả quét
            _currentTab = "AutoVoice"; // Ép chuyển sang chế độ thuyết minh để hiện UI ngay
            
            foreach (var poi in poisToRead)
            {
                // KIỂM TRA: Nếu cờ dừng đã được bật (do phát thủ công hoặc hành động khác ở Tab khác)
                if (!_isSequentialReading) break;

                _currentlyNarratingPoi = poi; // Ghi nhớ quán đang đọc

                // 1. CẬP NHẬT TOÀN CỤC (Luôn chạy dù ở Tab nào)
                if (_currentLang == "vi") lblStatus.Text = $"Đang thuyết minh: {poi.Name}";
                else if (_currentLang == "en") lblStatus.Text = $"Narrating: {poi.Name}";
                else if (_currentLang == "ja") lblStatus.Text = $"説明中: {poi.Name}";
                else lblStatus.Text = $"解说中: {poi.Name}";

                // Spotlight toàn cục
                var location = new Location(poi.Latitude, poi.Longitude);
                if (_spotlightCircle != null) mapVinhKhanh.MapElements.Remove(_spotlightCircle);
                _spotlightCircle = new Microsoft.Maui.Controls.Maps.Circle
                {
                    Center = location,
                    Radius = Distance.FromMeters(8),
                    StrokeColor = Color.FromArgb("#FF2ECC71"),
                    StrokeWidth = 12,
                    FillColor = Color.FromArgb("#552ECC71")
                };
                mapVinhKhanh.MapElements.Add(_spotlightCircle);

                // 2. CẬP NHẬT RIÊNG CHO TAB (Chỉ chiếm UI nếu người dùng đang ở tab 'nhà' của nó)
                bool isUserInNarrationTab = (_currentTab == "Nearby" || _currentTab == "AutoVoice");
                if (isUserInNarrationTab)
                {
                    _currentTab = "AutoVoice";
                    frmDanhSach.IsVisible = true; 
                    SwitchToList(lstFocus);
                    lstFocus.ItemsSource = new List<PoiModel> { poi };
                    
                    UpdateListTitle();
                    lstFocus.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
                    await Task.Delay(600); 
                }

                // Thuyết minh tuần tự (Âm thanh)
                await PhatThuyetMinh(poi, false); 
                
                if (!_isSequentialReading) break;

                // Xóa Spotlight sau khi đọc xong
                if (_spotlightCircle != null)
                {
                    mapVinhKhanh.MapElements.Remove(_spotlightCircle);
                    _spotlightCircle = null;
                }

                await Task.Delay(400); 
            }
            
            // XỬ LÝ KẾT THÚC:
            // Chỉ xóa _currentlyNarratingPoi nếu nó vẫn là kết quả của việc đọc tuần tự (không bị ghi đè bởi manual play)
            if (_isSequentialReading)
            {
                _currentlyNarratingPoi = null;
            }
            
            // Chỉ thực hiện nạp lại UI danh sách kết quả nếu người dùng vẫn 'đang chờ' ở các Tab liên quan
            if (_isSequentialReading && (_currentTab == "Nearby" || _currentTab == "AutoVoice"))
            {
                _isSequentialReading = false; 
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _currentTab = "Nearby";
                    SwitchToList(lstFocus);
                    lstFocus.ItemsSource = poisToRead; 
                    UpdateListTitle();
                    lstFocus.ScrollTo(0, position: ScrollToPosition.Start, animate: false);
                    
                    if (_currentLang == "vi") lblStatus.Text = "Đã xong thuyết minh lân cận";
                    else if (_currentLang == "en") lblStatus.Text = "Nearby narration finished";
                    else if (_currentLang == "ja") lblStatus.Text = "近くの案内が終了しました";
                    else lblStatus.Text = "附近解说已结束";

                    _ = Task.Run(async () => {
                        await Task.Delay(3000);
                        MainThread.BeginInvokeOnMainThread(() => {
                            if (_currentTab == "Nearby") 
                            {
                                UpdateListTitle();
                                UpdateTabAesthetics(); // Cập nhật lại màu Tab
                            }
                        });
                    });
                });
            }
            else
            {
                _isSequentialReading = false;
            }
        }
        else
        {
            if (_currentLang == "vi") await DisplayAlert("Thông báo", "Bạn chưa đến gần khu vực quán ẩm thực nào!", "ĐÓNG");
            else if (_currentLang == "en") await DisplayAlert("Notice", "You are not near any culinary places!", "CLOSE");
            else await DisplayAlert("Chú ý", "Không có quán lân cận.", "ĐÓNG");
        }
    }

    private void UpdateTabAesthetics()
    {
        // Khôi phục trạng thái mặc định cho tất cả (Xám, không bóng, scale 1.0)
        Color inactiveColor = Color.FromArgb("#808080"); // Gray
        var inactiveShadow = new Shadow { Radius = 0, Opacity = 0 };

        lblIconQuanAn.TextColor = inactiveColor;
        lblTabQuanAn.TextColor = inactiveColor;
        lblTabQuanAn.FontAttributes = FontAttributes.None;
        lblIconQuanAn.Scale = 1.0;
        lblIconQuanAn.Shadow = inactiveShadow;

        lblIconYeuThich.TextColor = inactiveColor;
        lblTabYeuThich.TextColor = inactiveColor;
        lblTabYeuThich.FontAttributes = FontAttributes.None;
        lblIconYeuThich.Scale = 1.0;
        lblIconYeuThich.Shadow = inactiveShadow;

        lblIconYeuThich.Shadow = inactiveShadow;

        lblIconXemThem.TextColor = inactiveColor;
        lblTabXemThem.TextColor = inactiveColor;
        lblTabXemThem.FontAttributes = FontAttributes.None;
        lblIconXemThem.Scale = 1.0;
        lblIconXemThem.Shadow = inactiveShadow;

        // Xử lý Active Tab
        Color activeColor = Color.FromArgb("#2ECC71"); // Emerald Green
        var activeShadow = new Shadow { Radius = 10, Opacity = 0.5f, Brush = new SolidColorBrush(activeColor), Offset = new Point(0,0) };

        switch (_currentTab)
        {
            case "All":
            case "Nearby":
            case "AutoVoice":
                lblIconQuanAn.TextColor = activeColor;
                lblTabQuanAn.TextColor = activeColor;
                lblTabQuanAn.FontAttributes = FontAttributes.Bold;
                lblIconQuanAn.Scale = 1.2;
                lblIconQuanAn.Shadow = activeShadow;
                break;
            case "Favorites":
                lblIconYeuThich.TextColor = activeColor;
                lblTabYeuThich.TextColor = activeColor;
                lblTabYeuThich.FontAttributes = FontAttributes.Bold;
                lblIconYeuThich.Scale = 1.2;
                lblIconYeuThich.Shadow = activeShadow;
                break;
            case "More":
                lblIconXemThem.TextColor = activeColor;
                lblTabXemThem.TextColor = activeColor;
                lblTabXemThem.FontAttributes = FontAttributes.Bold;
                lblIconXemThem.Scale = 1.2;
                lblIconXemThem.Shadow = activeShadow;
                break;
        }
    }

    private async void OnOwnerLoginClicked(object sender, EventArgs e)
    {
        // Ẩn menu cài đặt trước khi chuyển trang
        frmSettings.TranslationX = 280;
        bgOverlay.IsVisible = false;

        await Navigation.PushAsync(new LoginPage());
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