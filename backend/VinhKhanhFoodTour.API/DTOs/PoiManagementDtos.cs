namespace VinhKhanhFoodTour.DTOs
{
    public class RejectPoiDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    // 🔴 MỚI: Form request để nhận ảnh + dữ liệu POI
    public class CreatePoiFormRequest
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
    }

    public class CreatePoiDto
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // 🔴 MỚI: Cho phép upload ảnh cùng lúc tạo quán
        public string? ImageUrl { get; set; }
    }

    public class UpdatePoiDto
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // 🔴 MỚI: Form request để sửa quán (Update)
    public class UpdatePoiFormRequest
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
    }

    public class MapPinDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class OverviewMapPinDto : MapPinDto
    {
        public int Status { get; set; }
        public int OwnerId { get; set; }
        public int ListenCount { get; set; }
    }
}
