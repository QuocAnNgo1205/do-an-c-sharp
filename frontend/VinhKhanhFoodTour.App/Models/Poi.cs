using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace VinhKhanhFoodTour.App.Models
{
    /// <summary>
    /// Model đại diện cho một POI (Điểm Du Lịch) - ví dụ: Quán ăn, Di tích, Khu vui chơi...
    /// </summary>
    public partial class Poi : ObservableObject
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = "Đường Vĩnh Khánh, Phường 8, Quận 4, TP.Hồ Chí Minh";

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("translations")]
        public List<PoiTranslation> Translations { get; set; } = new();

        [ObservableProperty]
        [JsonIgnore]
        private string _distanceDisplay = "Đang tính...";

        [ObservableProperty]
        [JsonIgnore]
        private bool _isPlaying;

        [ObservableProperty]
        [JsonIgnore]
        private bool _isLoadingAudio;

        public PoiTranslation? GetTranslation(string languageCode)
        {
            return Translations?.FirstOrDefault(t => t.LanguageCode == languageCode);
        }

        public bool HasAudioForLanguage(string languageCode)
        {
            var translation = GetTranslation(languageCode);
            return !string.IsNullOrEmpty(translation?.AudioFilePath);
        }
    }

    /// <summary>
    /// Model dành cho biểu đồ thống kê
    /// </summary>
    public class PoiCategory
    {
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}