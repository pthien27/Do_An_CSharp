using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages
{
    public partial class UserRegisterPage : ContentPage
    {
        private readonly UserAuthService _authService;
        private string _currentLang;

        public UserRegisterPage()
        {
            InitializeComponent();
            _authService = new UserAuthService();
            _currentLang = Preferences.Default.Get("AppLanguage", "vi");
            UpdateStaticUI();
        }

        private void OnLanguageTapped(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string lang)
            {
                _currentLang = lang;
                Preferences.Default.Set("AppLanguage", _currentLang);
                UpdateStaticUI();
            }
        }

        private void UpdateStaticUI()
        {
            // Cập nhật màu nút chọn ngôn ngữ
            btnLangVi.BackgroundColor = _currentLang == "vi" ? Color.FromArgb("#10b981") : Color.FromArgb("#334155");
            btnLangVi.TextColor = _currentLang == "vi" ? Colors.White : Color.FromArgb("#94a3b8");
            
            btnLangEn.BackgroundColor = _currentLang == "en" ? Color.FromArgb("#10b981") : Color.FromArgb("#334155");
            btnLangEn.TextColor = _currentLang == "en" ? Colors.White : Color.FromArgb("#94a3b8");

            btnLangJa.BackgroundColor = _currentLang == "ja" ? Color.FromArgb("#10b981") : Color.FromArgb("#334155");
            btnLangJa.TextColor = _currentLang == "ja" ? Colors.White : Color.FromArgb("#94a3b8");

            btnLangZh.BackgroundColor = _currentLang.StartsWith("zh") ? Color.FromArgb("#10b981") : Color.FromArgb("#334155");
            btnLangZh.TextColor = _currentLang.StartsWith("zh") ? Colors.White : Color.FromArgb("#94a3b8");

            if (_currentLang == "en")
            {
                lblTitle.Text = "Create Account";
                lblSubtitle.Text = "Join Vinh Khanh Street community";
                lblUsername.Text = "Username";
                RegUsernameEntry.Placeholder = "e.g. jame123";
                lblPassword.Text = "Password";
                RegPasswordEntry.Placeholder = "********";
                lblConfirmPassword.Text = "Confirm password";
                RegConfirmPasswordEntry.Placeholder = "********";
                btnRegister.Text = "REGISTER NOW";
                lblHasAccount.Text = "Already have an account?";
                lblBackToLogin.Text = "Back to Login";
            }
            else if (_currentLang == "ja")
            {
                lblTitle.Text = "アカウント作成";
                lblSubtitle.Text = "ヴィンカン通りコミュニティに参加する";
                lblUsername.Text = "ユーザー名";
                RegUsernameEntry.Placeholder = "例: yamada123";
                lblPassword.Text = "パスワード";
                RegPasswordEntry.Placeholder = "********";
                lblConfirmPassword.Text = "パスワードを確認";
                RegConfirmPasswordEntry.Placeholder = "********";
                btnRegister.Text = "今すぐ登録";
                lblHasAccount.Text = "すでにアカウントをお持ちですか？";
                lblBackToLogin.Text = "ログインに戻る";
            }
            else if (_currentLang.StartsWith("zh"))
            {
                lblTitle.Text = "创建账号";
                lblSubtitle.Text = "加入永庆街社区";
                lblUsername.Text = "用户名";
                RegUsernameEntry.Placeholder = "例如: lisi123";
                lblPassword.Text = "密码";
                RegPasswordEntry.Placeholder = "********";
                lblConfirmPassword.Text = "确认密码";
                RegConfirmPasswordEntry.Placeholder = "********";
                btnRegister.Text = "立即注册";
                lblHasAccount.Text = "已有账号？";
                lblBackToLogin.Text = "返回登录";
            }
            else // vi
            {
                lblTitle.Text = "Tạo Tài Khoản";
                lblSubtitle.Text = "Tham gia cộng đồng Vĩnh Khánh Street";
                lblUsername.Text = "Tên đăng nhập";
                RegUsernameEntry.Placeholder = "Ví dụ: vanthien27";
                lblPassword.Text = "Mật khẩu";
                RegPasswordEntry.Placeholder = "********";
                lblConfirmPassword.Text = "Nhập lại mật khẩu";
                RegConfirmPasswordEntry.Placeholder = "********";
                btnRegister.Text = "ĐĂNG KÝ NGAY";
                lblHasAccount.Text = "Đã có tài khoản?";
                lblBackToLogin.Text = "Quay lại Đăng nhập";
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string username = RegUsernameEntry.Text?.Trim();
            string password = RegPasswordEntry.Text;
            string confirm = RegConfirmPasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                string errorMsg = _currentLang == "en" ? "Please fill in all fields." :
                                  _currentLang == "ja" ? "すべてのフィールドに入力してください。" :
                                  _currentLang.StartsWith("zh") ? "请填写所有信息。" :
                                  "Vui lòng điền đầy đủ thông tin.";
                await DisplayAlert(_currentLang == "vi" ? "Lỗi" : "Error", errorMsg, "OK");
                return;
            }

            if (password != confirm)
            {
                string errorMsg = _currentLang == "en" ? "Passwords do not match." :
                                  _currentLang == "ja" ? "パスワードが一致しません。" :
                                  _currentLang.StartsWith("zh") ? "密码不匹配。" :
                                  "Mật khẩu nhập lại không khớp.";
                await DisplayAlert(_currentLang == "vi" ? "Lỗi" : "Error", errorMsg, "OK");
                return;
            }

            if (password.Length < 6)
            {
                string errorMsg = _currentLang == "en" ? "Password must be at least 6 characters." :
                                  _currentLang == "ja" ? "パスワードは6文字以上である必要があります。" :
                                  _currentLang.StartsWith("zh") ? "密码必须至少有6个字符。" :
                                  "Mật khẩu phải từ 6 ký tự trở lên.";
                await DisplayAlert(_currentLang == "vi" ? "Lỗi" : "Error", errorMsg, "OK");
                return;
            }

            bool success = await _authService.RegisterAsync(username, password);

            if (success)
            {
                string okMsg = _currentLang == "en" ? "Your account has been successfully created!" :
                               _currentLang == "ja" ? "アカウントが正常に作成されました！" :
                               _currentLang.StartsWith("zh") ? "您的帐户已成功创建！" :
                               "Tài khoản của bạn đã được tạo thành công!";
                await DisplayAlert(_currentLang == "vi" ? "Thành công" : "Success", okMsg, "OK");
                await Navigation.PopAsync();
            }
            else
            {
                string failMsg = _currentLang == "en" ? "Username already exists or an error occurred." :
                                 _currentLang == "ja" ? "ユーザー名が既に存在するか、エラーが発生しました。" :
                                 _currentLang.StartsWith("zh") ? "用户名已存在或发生错误。" :
                                 "Tài khoản đã tồn tại hoặc có lỗi xảy ra.";
                await DisplayAlert(_currentLang == "vi" ? "Thất bại" : "Failed", failMsg, "OK");
            }
        }

        private async void OnBackToLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
