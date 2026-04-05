using VinhKhanhFoodTour.Models;
using VinhKhanhFoodTour.DTOs;

namespace VinhKhanhFoodTour.DTOs
{
    public class PoiDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // --- THÊM DÒNG NÀY VÀO ĐỂ APP NHẬN ĐƯỢC ẢNH ---
        public string ImageUrl { get; set; } = string.Empty;

        public PoiStatus Status { get; set; }
        public string? RejectionReason { get; set; }

        public List<PoiTranslationDto> Translations { get; set; } = new();
    }
}