namespace VinhKhanhFoodTour.AdminPortal.Services.Common;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string sourceLangCode, string targetLangCode);
}
