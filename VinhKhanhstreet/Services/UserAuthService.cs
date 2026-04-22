using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Plugin.CloudFirestore;
using VinhKhanhstreet.Models;

namespace VinhKhanhstreet.Services
{
    public enum LoginResult
    {
        Success,
        WrongPassword,
        NotFound,
        Locked
    }

    public class UserAuthService
    {
        private ICollectionReference _usersCollection => CrossCloudFirestore.Current.Instance.GetCollection("users");

        public UserAuthService()
        {
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            try
            {
                // Kiểm tra user tồn tại
                var doc = await _usersCollection.GetDocument(username).GetDocumentAsync();
                if (doc.Exists) return false;

                // Hash mật khẩu
                string pwdHash = HashPassword(password);

                var user = new UserModel(username, pwdHash);
                await _usersCollection.GetDocument(username).SetDataAsync(user);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            try
            {
                var doc = await _usersCollection.GetDocument(username).GetDocumentAsync();
                if (!doc.Exists) return LoginResult.NotFound;

                var user = doc.ToObject<UserModel>();

                // Kiểm tra khóa tài khoản
                if (user.IsLocked) return LoginResult.Locked;

                string pwdHash = HashPassword(password);
                if (user.PasswordHash != pwdHash) return LoginResult.WrongPassword;

                // Đăng nhập thành công → cập nhật thống kê và trạng thái Online
                await _usersCollection.GetDocument(username).UpdateDataAsync(new
                {
                    LoginCount = (user.LoginCount + 1),
                    LastActiveAt = DateTime.UtcNow,
                    IsOnline = true
                });

                return LoginResult.Success;
            }
            catch (Exception)
            {
                return LoginResult.WrongPassword;
            }
        }

        public async Task SetOnlineStatusAsync(string username, bool isOnline)
        {
            try
            {
                if (string.IsNullOrEmpty(username)) return;
                await _usersCollection.GetDocument(username).UpdateDataAsync(new { IsOnline = isOnline });
            }
            catch { }
        }

        /// <summary>Kiểm tra tài khoản có bị khóa hay bị xóa không (khi app đang chạy).</summary>
        public async Task<bool> IsAccountLockedOrDeletedAsync(string username)
        {
            try
            {
                var doc = await _usersCollection.GetDocument(username).GetDocumentAsync();
                if (!doc.Exists) return true; // Bị xóa → coi như bị khóa
                var user = doc.ToObject<UserModel>();
                return user.IsLocked;
            }
            catch
            {
                return false; // Lỗi mạng → không kick
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
