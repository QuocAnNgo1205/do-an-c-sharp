namespace VinhKhanhFoodTour.AdminPortal.Models.Statistics;

public class ListenStats
{
    public int TotalListens { get; set; }
    public Dictionary<string, int> ListensByLanguage { get; set; } = [];
    public Dictionary<long, int> ListensByPoi { get; set; } = [];
    public DateTime Period { get; set; }
}

public class SyncStatus
{
    public DateTime? LastSuccess { get; set; }
    public bool IsRunning { get; set; }
    public TimeSpan Duration { get; set; }
}
