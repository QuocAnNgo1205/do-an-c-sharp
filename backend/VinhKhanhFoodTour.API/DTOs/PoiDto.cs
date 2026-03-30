using VinhKhanhFoodTour.Models; // Đổi namespace cho khớp với enum PoiStatus của bạn
using VinhKhanhFoodTour.DTOs; // Đảm bảo namespace này khớp với nơi bạn đặt PoiTranslationDto   

namespace VinhKhanhFoodTour.DTOs
{
    public class PoiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public PoiStatus Status { get; set; }
        public string? RejectionReason { get; set; }

        // Chứa danh sách bản dịch, nhưng không bị vòng lặp
        public List<PoiTranslationDto> Translations { get; set; } = new();
    }
}