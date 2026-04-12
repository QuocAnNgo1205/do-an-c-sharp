namespace VinhKhanhFoodTour.AdminPortal.Services.Common;

/// <summary>
/// Dịch văn bản dùng MyMemory API (miễn phí, không cần API key).
/// Giới hạn: ~5000 ký tự/request, ~500 request/ngày/IP.
/// </summary>
public class TranslationService : ITranslationService
{
    private static readonly HttpClient _http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<string> TranslateAsync(string text, string sourceLangCode, string targetLangCode)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        if (sourceLangCode == targetLangCode) return text;

        try
        {
            // Cắt ngắn tránh vượt giới hạn (MyMemory giới hạn 500 ký tự free tier)
            var safeText = text.Length > 500 ? text[..500] + "..." : text;
            var encodedText = Uri.EscapeDataString(safeText);
            var langPair = $"{sourceLangCode}|{targetLangCode}";
            var url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={langPair}";

            var response = await _http.GetStringAsync(url);

            // Parse thủ công để không phụ thuộc Newtonsoft
            // Response: {"responseData":{"translatedText":"..."},"responseStatus":200}
            const string key = "\"translatedText\":\"";
            var startIdx = response.IndexOf(key);
            if (startIdx < 0) return text; // Fallback về bản gốc nếu parse thất bại

            startIdx += key.Length;
            var endIdx = response.IndexOf('"', startIdx);
            if (endIdx < 0) return text;

            var translated = response[startIdx..endIdx];
            // Unescape unicode sequences (\uXXXX)
            translated = System.Text.RegularExpressions.Regex.Unescape(translated);

            return string.IsNullOrWhiteSpace(translated) ? text : translated;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TranslationService] Error: {ex.Message}. Returning original text.");
            return text; // Trả về text gốc (tiếng Việt) để TTS vẫn đọc được
        }
    }
}
