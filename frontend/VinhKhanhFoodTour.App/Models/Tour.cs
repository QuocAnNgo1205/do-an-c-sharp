using System.Collections.Generic;

namespace VinhKhanhFoodTour.App.Models
{
    public class Tour
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int NumberOfStops { get; set; }
        public string Duration { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<int> PoiIds { get; set; } = new(); // Danh sách ID các điểm thuộc tour này
    }
}
