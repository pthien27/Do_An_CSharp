using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();

	public LoginPage()
	{
		InitializeComponent();
	}

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string user = entUsername.Text?.Trim();
        string pass = entPassword.Text?.Trim();

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập đầy đủ tài khoản và mật khẩu", "OK");
            return;
        }

        // Mock login check for now
        if (user == "admin" && pass == "123")
        {
            await DisplayAlert("Thành công", "Đăng nhập quyền chủ quán thành công!", "OK");
            // Navigation to Admin Dashboard later
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Thất bại", "Tài khoản hoặc mật khẩu không đúng", "OK");
        }
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
