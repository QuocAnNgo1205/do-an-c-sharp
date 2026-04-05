namespace VinhKhanhFoodTour.DTOs
{
    public class PoiDetailTranslationDto
    {
        public int Id { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AudioFilePath { get; set; }
        public string? ImageUrl { get; set; }
    }
}
