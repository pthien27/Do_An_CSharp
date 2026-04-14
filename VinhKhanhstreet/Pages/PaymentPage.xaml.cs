using VinhKhanhstreet.Models;
using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages;

public partial class PaymentPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private UserModel _user;
    private PoiModel _poi;

    public PaymentPage(UserModel user, PoiModel poi)
    {
        InitializeComponent();
        _user = user;
        _poi = poi;

        lblAccount.Text = _user.Username;
        lblResName.Text = _poi.Name;
    }

    private void OnMethodClicked(object sender, EventArgs e)
    {
        // Phản hồi UI đơn giản khi chọn phương thức
        if (sender is Button btn)
        {
            btnBank.Opacity = (btn == btnBank) ? 1.0 : 0.5;
            btnMoMo.Opacity = (btn == btnMoMo) ? 1.0 : 0.5;
        }
    }

    private async void OnFinalPaymentClicked(object sender, EventArgs e)
    {
        try 
        {
            // Hiệu ứng chờ đợi
            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Text = "ĐANG XỬ LÝ...";

            await Task.Delay(2000); // Giả lập chờ cổng thanh toán

            // Ghi dữ liệu thật sự vào Cloud Firestore
            await _dbService.FinalizeOwnerRegistrationAsync(_user, _poi);

            await DisplayAlert("Thành công", "Thanh toán thành công! Tài khoản chủ quán và Quán ăn của bạn đã được kích hoạt.", "VỀ TRANG ĐĂNG NHẬP");
            
            // Tìm vị trí trang LoginPage trong Stack
            var stack = Navigation.NavigationStack.ToList();
            var loginPage = stack.FirstOrDefault(p => p is LoginPage);
            
            if (loginPage != null)
            {
                // 1. Xóa "âm thầm" các trang nằm giữa Login and Payment (trang Register)
                for (int i = stack.Count - 2; i > stack.IndexOf(loginPage); i--)
                {
                    Navigation.RemovePage(stack[i]);
                }
                
                // 2. Quay về thẳng LoginPage
                await Navigation.PopAsync();
            }
            else 
            {
                // Nếu không tìm thấy, quay về gốc rồi mở mới
                await Navigation.PopToRootAsync();
                await Navigation.PushAsync(new LoginPage());
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Có lỗi xảy ra trong quá trình thanh toán: " + ex.Message, "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
