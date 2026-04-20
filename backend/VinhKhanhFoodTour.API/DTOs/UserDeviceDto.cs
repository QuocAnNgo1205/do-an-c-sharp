namespace VinhKhanhFoodTour.API.DTOs
{
    public class UserDeviceDto
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? Os { get; set; }
        public DateTime LastActiveAt { get; set; }
        public bool IsRevoked { get; set; }
    }
}
