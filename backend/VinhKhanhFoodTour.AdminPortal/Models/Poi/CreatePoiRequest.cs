namespace VinhKhanhFoodTour.AdminPortal.Models.Poi;

public class CreatePoiRequest
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TriggerRadius { get; set; }
    public IFormFile? Image { get; set; }
    public List<TranslationInput> Translations { get; set; } = [];
}

public class TranslationInput
{
    public string Language { get; set; } = string.Empty; // "vi", "en"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile? NarrationFile { get; set; }
}
