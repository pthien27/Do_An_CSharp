using VinhKhanhstreet.Models;
using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private PoiModel _tempPoi = new PoiModel();
    private readonly List<string> _selectedPhotos = new();
    private bool _isTranslated = false;

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnTranslateClicked(object sender, EventArgs e)
    {
        string vi = edDescVi.Text?.Trim();
        if (string.IsNullOrWhiteSpace(vi))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập mô tả Tiếng Việt trước khi dịch", "OK");
            return;
        }

        lblStatusTranslate.Text = "Đang kết nối máy chủ dịch thuật...";
        btnGoToPayment.IsEnabled = false;

        try 
        {
            // Gọi Service dịch thật từ dự án
            _tempPoi.Description = vi;
            
            var taskEn = GoogleTranslateService.TranslateAsync(vi, "en");
            var taskJa = GoogleTranslateService.TranslateAsync(vi, "ja");
            var taskZh = GoogleTranslateService.TranslateAsync(vi, "zh-CN");

            await Task.WhenAll(taskEn, taskJa, taskZh);

            _tempPoi.DescriptionEn = await taskEn ?? "[Lỗi dịch Tiếng Anh]";
            _tempPoi.DescriptionJa = await taskJa ?? "[Lỗi dịch Tiếng Nhật]";
            _tempPoi.DescriptionZh = await taskZh ?? "[Lỗi dịch Tiếng Trung]";

            // Cập nhật lên giao diện
            edDescEn.Text = _tempPoi.DescriptionEn;
            edDescJa.Text = _tempPoi.DescriptionJa;
            edDescZh.Text = _tempPoi.DescriptionZh;
            stkTranslations.IsVisible = true;

            _isTranslated = true;
            lblStatusTranslate.Text = "✅ Đã dịch thuật thành công!";
            lblStatusTranslate.TextColor = Colors.Green;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể dịch thuật lúc này. Vui lòng kiểm tra kết nối mạng!", "OK");
            lblStatusTranslate.Text = "❌ Lỗi dịch thuật";
        }
        finally 
        {
            btnGoToPayment.IsEnabled = true;
        }
    }

    private async void OnPickImagesClicked(object sender, EventArgs e)
    {
        try
        {
            if (_selectedPhotos.Count >= 5)
            {
                await DisplayAlert("Thông báo", "Bạn đã chọn đủ 5 tấm ảnh!", "OK");
                return;
            }

            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = $"Chọn thêm {5 - _selectedPhotos.Count} ảnh",
                FileTypes = FilePickerFileType.Images
            });

            if (results == null || !results.Any()) return;

            // Cộng dồn vào danh sách hiện tại (chống trùng bằng Tên File)
            int skipCount = 0;
            foreach (var result in results)
            {
                // Kiểm tra xem tên file này đã tồn tại trong danh sách chọn chưa
                bool isDuplicate = _selectedPhotos.Any(p => System.IO.Path.GetFileName(p) == result.FileName);

                if (isDuplicate)
                {
                    skipCount++;
                    continue;
                }

                if (_selectedPhotos.Count < 5)
                {
                    _selectedPhotos.Add(result.FullPath);
                }
            }

            if (skipCount > 0)
            {
                await DisplayAlert("Thông báo", $"Đã bỏ qua {skipCount} ảnh bị trùng.", "OK");
            }

            // Cập nhật giao diện preview (fill lần lượt từ 1 đến 5)
            img1.Source = _selectedPhotos.Count > 0 ? _selectedPhotos[0] : "dotnet_bot.png";
            img2.Source = _selectedPhotos.Count > 1 ? _selectedPhotos[1] : null;
            img3.Source = _selectedPhotos.Count > 2 ? _selectedPhotos[2] : null;
            img4.Source = _selectedPhotos.Count > 3 ? _selectedPhotos[3] : null;
            img5.Source = _selectedPhotos.Count > 4 ? _selectedPhotos[4] : null;

            // Cập nhật Model phục vụ lưu database
            if (_selectedPhotos.Count > 0) _tempPoi.ImageUrl1 = _selectedPhotos[0];
            if (_selectedPhotos.Count > 1) _tempPoi.ImageUrl2 = _selectedPhotos[1];
            if (_selectedPhotos.Count > 2) _tempPoi.ImageUrl3 = _selectedPhotos[2];
            if (_selectedPhotos.Count > 3) _tempPoi.ImageUrl4 = _selectedPhotos[3];
            if (_selectedPhotos.Count > 4) _tempPoi.ImageUrl5 = _selectedPhotos[4];

            lblPhotoCount.Text = $"Đã chọn: {_selectedPhotos.Count}/5";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể chọn ảnh: " + ex.Message, "OK");
        }
    }

    private async void OnGoToPaymentClicked(object sender, EventArgs e)
    {
        // 1. Kiểm tra trống
        if (string.IsNullOrWhiteSpace(entNewUser.Text) || 
            string.IsNullOrWhiteSpace(entNewPass.Text) ||
            string.IsNullOrWhiteSpace(entResName.Text) ||
            string.IsNullOrWhiteSpace(entLat.Text) ||
            string.IsNullOrWhiteSpace(entLon.Text) ||
            pckCategory.SelectedIndex == -1)
        {
            await DisplayAlert("Lỗi", "Vui lòng điền đầy đủ tất cả các trường thông tin!", "OK");
            return;
        }

        // 2. Kiểm tra mật khẩu khớp
        if (entNewPass.Text != entConfirmPass.Text)
        {
            await DisplayAlert("Lỗi", "Mật khẩu nhập lại không khớp!", "OK");
            return;
        }

        // 3. Kiểm tra đã dịch chưa
        if (!_isTranslated)
        {
            await DisplayAlert("Lỗi", "Bạn phải nhấn nút Dịch thuật để tạo nội dung đa ngôn ngữ trước!", "OK");
            return;
        }

        // 4. Kiểm tra Trạng thái và Phân loại
        if (pckStatus.SelectedIndex == -1 || pckCategory.SelectedIndex == -1)
        {
            await DisplayAlert("Lỗi", "Vui lòng chọn đầy đủ Trạng thái và Phân loại quán!", "OK");
            return;
        }

        // 5. Kiểm tra đủ 5 tấm ảnh
        if (_selectedPhotos.Count < 5)
        {
            await DisplayAlert("Lỗi", "Bạn bắt buộc phải chọn đủ 5 tấm ảnh của quán!", "OK");
            return;
        }

        // 5. Kiểm tra trùng (Logic Database)
        try 
        {
            bool isUserDup = await _dbService.IsUsernameDuplicateAsync(entNewUser.Text.Trim());
            if (isUserDup) {
                await DisplayAlert("Lỗi", "Tên tài khoản này đã tồn tại!", "OK");
                return;
            }

            double lat = double.Parse(entLat.Text);
            double lon = double.Parse(entLon.Text);
            bool isResDup = await _dbService.IsRestaurantDuplicateAsync(entResName.Text.Trim(), lat, lon);
            if (isResDup) {
                await DisplayAlert("Lỗi", "Tên quán hoặc vị trí này đã được đăng ký!", "OK");
                return;
            }

            // 6. Nếu mọi thứ OK, chuẩn bị dữ liệu và chuyển sang Payment
            var user = new UserModel { Username = entNewUser.Text.Trim(), Password = entNewPass.Text.Trim() };
            
            _tempPoi.Name = entResName.Text.Trim();
            _tempPoi.Latitude = lat;
            _tempPoi.Longitude = lon;
            _tempPoi.CategoryVi = pckCategory.SelectedItem.ToString();
            _tempPoi.StrStatusOpen = pckStatus.SelectedItem.ToString();
            _tempPoi.ClosingTime = entClosingTime.Text ?? "22:00";
            _tempPoi.Radius = 30; // Default

            // Gán 5 ảnh vào Model để sang trang Payment hiển thị
            _tempPoi.ImageUrl1 = _selectedPhotos[0];
            _tempPoi.ImageUrl2 = _selectedPhotos[1];
            _tempPoi.ImageUrl3 = _selectedPhotos[2];
            _tempPoi.ImageUrl4 = _selectedPhotos[3];
            _tempPoi.ImageUrl5 = _selectedPhotos[4];

            await Navigation.PushAsync(new PaymentPage(user, _tempPoi));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại tọa độ!", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
