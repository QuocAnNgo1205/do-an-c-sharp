namespace VinhKhanhFoodTour.DTOs
{
    public class PoiTranslationDto
    {
        public int Id { get; set; }
        // Giả sử bảng của bạn có trường LanguageCode (như "vi", "en")
        public string LanguageCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}