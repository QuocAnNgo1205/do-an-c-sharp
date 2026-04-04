namespace VinhKhanhFoodTour.AdminPortal.Models.Poi;

public class PoiDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TriggerRadius { get; set; }
    public string Status { get; set; } = string.Empty; // Approved, Pending, Rejected
    public DateTime CreatedAt { get; set; }
    public string? ImageUrl { get; set; }
    public List<PoiTranslationDto> Translations { get; set; } = [];
}

public class PoiTranslationDto
{
    public long Id { get; set; }
    public string Language { get; set; } = string.Empty; // "vi", "en"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrationUrl { get; set; }
}
