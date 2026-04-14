namespace VinhKhanhFoodTour.DTOs
{
    public class OfflinePackManifestDto
    {
        public string Version { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string SHA256Hash { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    public class SyncStatusDto
    {
        public bool HasSuccess { get; set; }
        public DateTime? LastSuccessAt { get; set; }
        public long? LastDurationMs { get; set; }
        public string? LastError { get; set; }
        public DateTime? LastErrorAt { get; set; }
        public bool IsRunning { get; set; }
    }

    public class NarrationLogRequestDto
    {
        public int PoiId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }
    }

    public class OwnerListenStatsDto
    {
        public int PoiId { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public int ListenCount { get; set; }
    }

    public class ListenTrendDto
    {
        public string Label { get; set; } = string.Empty;
        public int ListenCount { get; set; }
    }
}
