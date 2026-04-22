using VinhKhanhstreet.Services;

namespace VinhKhanhstreet.Pages
{
    public partial class UserLoginPage : ContentPage
    {
        private readonly UserAuthService _authService;
        private string _currentLang;

        public UserLoginPage()
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
            // Cập nhật màu các nút chọn ngôn ngữ
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
                lblTitle.Text = "Vinh Khanh Street";
                lblSubtitle.Text = "For Users";
                lblUsername.Text = "Username";
                UsernameEntry.Placeholder = "Enter username...";
                lblPassword.Text = "Password";
                PasswordEntry.Placeholder = "********";
                btnLogin.Text = "LOGIN";
                lblNoAccount.Text = "Don't have an account?";
                lblRegister.Text = "Register now";
                btnSkipLogin.Text = "Continue as Guest";
            }
            else if (_currentLang == "ja")
            {
                lblTitle.Text = "ヴィンカン通り";
                lblSubtitle.Text = "ユーザー向け";
                lblUsername.Text = "ユーザー名";
                UsernameEntry.Placeholder = "ユーザー名を入力...";
                lblPassword.Text = "パスワード";
                PasswordEntry.Placeholder = "********";
                btnLogin.Text = "ログイン";
                lblNoAccount.Text = "アカウントをお持ちでないですか？";
                lblRegister.Text = "今すぐ登録";
                btnSkipLogin.Text = "ゲストとして続行";
            }
            else if (_currentLang.StartsWith("zh"))
            {
                lblTitle.Text = "永庆街";
                lblSubtitle.Text = "用户专区";
                lblUsername.Text = "用户名";
                UsernameEntry.Placeholder = "输入用户名...";
                lblPassword.Text = "密码";
                PasswordEntry.Placeholder = "********";
                btnLogin.Text = "登录";
                lblNoAccount.Text = "还没有账号？";
                lblRegister.Text = "立即注册";
                btnSkipLogin.Text = "以游客身份继续";
            }
            else // vi
            {
                lblTitle.Text = "Vĩnh Khánh Street";
                lblSubtitle.Text = "Dành cho người dùng";
                lblUsername.Text = "Tên đăng nhập";
                UsernameEntry.Placeholder = "Nhập tài khoản...";
                lblPassword.Text = "Mật khẩu";
                PasswordEntry.Placeholder = "********";
                btnLogin.Text = "ĐĂNG NHẬP";
                lblNoAccount.Text = "Chưa có tài khoản?";
                lblRegister.Text = "Đăng ký ngay";
                btnSkipLogin.Text = "Vào xem với tư cách Khách";
            }
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                string errorMsg = _currentLang == "en" ? "Please fill in all fields." :
                                  _currentLang == "ja" ? "すべてのフィールドに入力してください。" :
                                  _currentLang.StartsWith("zh") ? "请填写所有信息。" :
                                  "Vui lòng nhập đầy đủ thông tin.";
                await DisplayAlert(_currentLang == "vi" ? "Lỗi" : "Error", errorMsg, "OK");
                return;
            }

            var result = await _authService.LoginAsync(username, password);

            if (result == LoginResult.Success)
            {
                await SecureStorage.Default.SetAsync("IsUserLoggedIn", "true");
                await SecureStorage.Default.SetAsync("CurrentUser", username);
                Application.Current.MainPage = new AppShell();
            }
            else if (result == LoginResult.Locked)
            {
                string lockedMsg = _currentLang == "en" ? "Your account has been locked. Please contact the administrator." :
                                   _currentLang == "ja" ? "アカウントがロックされています。管理者にお問い合わせください。" :
                                   _currentLang.StartsWith("zh") ? "您的账号已被锁定，请联系管理员。" :
                                   "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                await DisplayAlert("⛔ Tài khoản bị khóa", lockedMsg, "OK");
            }
            else
            {
                string failMsg = _currentLang == "en" ? "Incorrect username or password." :
                                 _currentLang == "ja" ? "ユーザー名またはパスワードが正しくありません。" :
                                 _currentLang.StartsWith("zh") ? "用户名或密码不正确。" :
                                 result == LoginResult.NotFound
                                     ? "Tài khoản không tồn tại."
                                     : "Tài khoản hoặc mật khẩu không chính xác.";
                await DisplayAlert(_currentLang == "vi" ? "Thất bại" : "Failed", failMsg, "OK");
            }
        }

        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UserRegisterPage());
        }

        private void OnSkipLoginClicked(object sender, EventArgs e)
        {
            // MỚI: Xóa sạch dấu vết đăng nhập cũ (nếu có) để đảm bảo vào với tư cách Khách thực thụ
            SecureStorage.Default.Remove("IsUserLoggedIn");
            SecureStorage.Default.Remove("CurrentUser");

            // Vào thẳng AppShell (MapPage) với tư cách Khách
            Application.Current.MainPage = new AppShell();
        }
    }
}
