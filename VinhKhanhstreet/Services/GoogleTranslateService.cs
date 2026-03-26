using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace VinhKhanhstreet.Services
{
    public static class GoogleTranslateService
    {
        // Sử dụng HttpClient dạng static để tái sử dụng connection, tránh cạn kiệt socket.
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Dịch văn bản thông qua Google Translate API (Free endpoint không cần API Key)
        /// </summary>
        /// <param name="text">Nội dung văn bản cần dịch</param>
        /// <param name="toLanguage">Mã ngôn ngữ đích (Ví dụ: "en", "ja", "ko", "zh-CN")</param>
        /// <param name="fromLanguage">Mã ngôn ngữ nguồn (Mặc định: "vi", ghi "auto" để Google tự nhận diện)</param>
        /// <returns>Văn bản sau khi đã dịch xong</returns>
        public static async Task<string> TranslateAsync(string text, string toLanguage = "en", string fromLanguage = "vi")
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                // Đóng gói đoạn text vào định dạng an toàn cho url
                string encodedText = WebUtility.UrlEncode(text);
                
                // Endpoint GTX nội bộ của Google Translate (dùng được free mà không bị block nếu k call quá nhiều)
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={encodedText}";

                // Gọi request lấy chuỗi JSON
                var response = await _httpClient.GetStringAsync(url);
                
                // Cấu trúc Data Google trả về khá phức tạp: [[["Bản dịch", "Bản Gốc", null, null, 1]]]
                // Nên chúng ta sẽ lấy mảng gốc (RootElement) -> phần tử 0 -> phần tử 0 -> phần tử 0 chứa text dịch.
                using JsonDocument doc = JsonDocument.Parse(response);
                
                string translatedText = "";
                // Duyệt qua tất cả các câu (vì Google có thể tách text dài thành nhiều câu trong mảng)
                foreach (JsonElement sentence in doc.RootElement[0].EnumerateArray())
                {
                    translatedText += sentence[0].GetString();
                }
                
                return translatedText;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ví dụ không có mạng hoặc bị block IP)
                // Trả về null để bên ngoài biết là lỗi và không lưu cache sai lệch
                Console.WriteLine($"[Translation Error] {ex.Message}");
                return null;
            }
        }
    }
}
