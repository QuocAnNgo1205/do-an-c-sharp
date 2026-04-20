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

        [JsonIgnore]
        public string FullImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(ImageUrl)) return "placeholder_restaurant.png";
                
                // Nếu URL đã là http nhưng trỏ vào localhost/127.0.0.1 (do cache cũ), ta cần đổi lại IP mới
                var finalImageUrl = ImageUrl;
                if (finalImageUrl.Contains("localhost") || finalImageUrl.Contains("127.0.0.1"))
                {
                    // Lấy phần path sau port (ví dụ: /uploads/images/...)
                    var uri = new Uri(finalImageUrl);
                    finalImageUrl = uri.PathAndQuery;
                }

                if (finalImageUrl.StartsWith("http")) return finalImageUrl;

                // Xây dựng URL tuyệt đối dựa trên IP LAN hiện tại từ Constants
                var rootUrl = Data.Constants.API_BASE_URL.Replace("/api/v1", "");
                return $"{rootUrl.TrimEnd('/')}/{finalImageUrl.TrimStart('/')}";
            }
        }

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