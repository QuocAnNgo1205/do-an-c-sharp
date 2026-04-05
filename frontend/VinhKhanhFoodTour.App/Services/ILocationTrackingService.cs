namespace VinhKhanhFoodTour.App.Services;

public class LocationEventArgs : EventArgs
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public interface ILocationTrackingService
{
    event EventHandler<LocationEventArgs>? LocationUpdated;
    void StartTracking();
    void StopTracking();
    bool IsTracking { get; }
}
