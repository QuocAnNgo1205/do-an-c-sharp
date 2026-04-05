using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace VinhKhanhFoodTour.App.Models
{
    public partial class Poi : ObservableObject
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
        [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; } = string.Empty;
        [JsonPropertyName("status")] public int Status { get; set; }

        // --- THÊM PHẦN NÀY ĐỂ HIỂN THỊ KHOẢNG CÁCH ---
        [ObservableProperty]
        [JsonIgnore]
        private string _distanceDisplay = "Đang tính...";
    }
}