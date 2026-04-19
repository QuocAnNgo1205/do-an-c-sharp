using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace VinhKhanhFoodTour.App.Models
{
    public class Tour
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("thumbnailUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("usageCount")]
        public int UsageCount { get; set; }

        [JsonPropertyName("pois")]
        public List<TourPoiItem> Pois { get; set; } = new();

        [JsonIgnore]
        public int NumberOfStops => Pois?.Count ?? 0;

        [JsonIgnore]
        public string Duration => "Tùy người tham gia";

        [JsonIgnore]
        public List<int> PoiIds => Pois?.OrderBy(p => p.OrderIndex).Select(p => p.PoiId).ToList() ?? new List<int>();
    }

    public class TourPoiItem
    {
        [JsonPropertyName("poiId")]
        public int PoiId { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("poiName")]
        public string PoiName { get; set; } = string.Empty;

        [JsonPropertyName("poiImageUrl")]
        public string? PoiImageUrl { get; set; }
    }
}
