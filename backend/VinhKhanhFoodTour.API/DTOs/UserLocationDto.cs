namespace VinhKhanhFoodTour.DTOs
{
    public class UserLocationRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? CurrentTourId { get; set; }
    }

    public class HeatmapPointDto
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Intensity { get; set; } = 1.0;
    }
}
