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
        private static readonly HttpClient _httpClient;

        static GoogleTranslateService()
        {
            _httpClient = new HttpClient();
            // Thêm User-Agent để Google nhận diện là trình duyệt, tránh bị block 403
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

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
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Translation Error] Google returned {response.StatusCode}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                // Cấu trúc Data Google trả về khá phức tạp: [[["Bản dịch", "Bản Gốc", null, null, 1]]]
                // Nên chúng ta sẽ lấy mảng gốc (RootElement) -> phần tử 0 -> phần tử 0 -> phần tử 0 chứa text dịch.
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                
                string translatedText = "";
                // Duyệt qua tất cả các câu trong mảng kết quả
                var parts = doc.RootElement[0].EnumerateArray();
                foreach (JsonElement sentence in parts)
                {
                    // Phần tử đầu tiên của mỗi câu là text đã dịch
                    if (sentence.ValueKind == JsonValueKind.Array)
                    {
                        translatedText += sentence[0].GetString();
                    }
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
