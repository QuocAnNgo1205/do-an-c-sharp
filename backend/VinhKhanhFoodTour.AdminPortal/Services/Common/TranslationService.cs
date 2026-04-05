namespace VinhKhanhFoodTour.AdminPortal.Services.Common;

public class TranslationService : ITranslationService
{
    public async Task<string> TranslateAsync(string text, string sourceLangCode, string targetLangCode)
    {
        // Giả lập độ trễ của API dịch thuật (ví dụ Google Translate API)
        await Task.Delay(1500);

        // Giả lập kết quả dịch thuật dựa trên mã ngôn ngữ
        return targetLangCode switch
        {
            "en" => $"[Translated to English] {text}",
            "ja" => $"[Translated to Japanese] {text}",
            "ko" => $"[Translated to Korean] {text}",
            "zh" => $"[Translated to Chinese] {text}",
            "fr" => $"[Translated to French] {text}",
            _ => text
        };
    }
}
