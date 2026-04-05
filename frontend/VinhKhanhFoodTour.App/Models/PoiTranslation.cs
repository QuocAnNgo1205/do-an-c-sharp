using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace VinhKhanhFoodTour.App.Models
{
    /// <summary>
    /// Model đại diện cho một bản dịch của POI (Điểm Du Lịch)
    /// 
    /// Cấu trúc này khớp với PoiDetailTranslationDto từ Backend
    /// </summary>
    public partial class PoiTranslation : ObservableObject
    {
        /// <summary>
        /// ID bản dịch trong hệ thống
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Mã ngôn ngữ (ví dụ: "vi" cho Tiếng Việt, "en" cho tiếng Anh)
        /// </summary>
        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = string.Empty;

        /// <summary>
        /// Tiêu đề bản dịch (ví dụ: "Phở Gia Truyền")
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả chi tiết về các sự kiện, đặc sản (ví dụ: "Quán phở nổi tiếng với phở bò")
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn file âm thanh (thuyết minh). Có thể là:
        /// - Đường dẫn tương đối: "/media/vi/poi_1.mp3"
        /// - Null nếu không có audio (sẽ fallback sang TTS)
        /// </summary>
        [JsonPropertyName("audioFilePath")]
        public string? AudioFilePath { get; set; }

        /// <summary>
        /// Đường dẫn ảnh phụ của bản dịch (nếu có)
        /// </summary>
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }
}
