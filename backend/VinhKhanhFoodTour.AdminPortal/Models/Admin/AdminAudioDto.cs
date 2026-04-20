using System;

namespace VinhKhanhFoodTour.AdminPortal.Models.Admin
{
    public class AdminAudioDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime? LastModified { get; set; }
        
        public int? PoiId { get; set; }
        public string? PoiName { get; set; }
        public string? LanguageCode { get; set; }
        
        public bool IsUsed => PoiId.HasValue;
    }
}
