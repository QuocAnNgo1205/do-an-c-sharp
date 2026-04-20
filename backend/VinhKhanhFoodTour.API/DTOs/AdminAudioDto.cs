using System;

namespace VinhKhanhFoodTour.API.DTOs
{
    public class AdminAudioDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; } // in bytes
        public DateTime? LastModified { get; set; }
        
        // Usage info (optional, will be null if not used)
        public int? PoiId { get; set; }
        public string? PoiName { get; set; }
        public string? LanguageCode { get; set; }
        public bool IsUsed => PoiId.HasValue;
    }
}
