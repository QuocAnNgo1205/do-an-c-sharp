namespace VinhKhanhFoodTour.AdminPortal.Services.Admin;

/// <summary>
/// DTO for Sync Status from Backend
/// </summary>
public class SyncStatusDto
{
    public bool HasSuccess { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public long? LastDurationMs { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public bool IsRunning { get; set; }
}

/// <summary>
/// Service interface for communicating with the Backend Offline Pack System
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Get the current status of the offline pack synchronization
    /// </summary>
    Task<SyncStatusDto> GetSyncStatusAsync();
}
