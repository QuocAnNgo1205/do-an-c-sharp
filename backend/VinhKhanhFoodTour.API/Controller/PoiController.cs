using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data; // Chú ý: Namespace này phải khớp với chỗ ông để AppDbContext
using VinhKhanhFoodTour.Models; // Chú ý: Namespace chứa class Poi, PoiTranslation

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Owner")] // Chỉ Admin và Chủ quán mới được vào đây
    public class PoiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PoiController(AppDbContext context)
        {
            _context = context;
        }

        // API: Thêm một quán ăn mới
        [HttpPost]
        public async Task<IActionResult> CreateNewPoi([FromBody] CreatePoiRequest request)
        {
            try
            {
                // 1. Tạo thực thể POI (Quán ăn)
                var newPoi = new Poi
                {
                    Name = request.Name,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Status = PoiStatus.Pending, // Mặc định chờ duyệt
                    TriggerRadius = 50,  // Bán kính kích hoạt 50m
                    Translations = new List<PoiTranslation>
                    {
                        new PoiTranslation
                        {
                            LanguageCode = "vi",
                            Title = request.Title,
                            Description = request.Description
                        }
                    }
                };

                // 2. Lưu vào Database
                _context.Pois.Add(newPoi);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Đã thêm quán thành công! Chờ Admin duyệt nhé ông chủ.",
                    Id = newPoi.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Lỗi rồi đại vương ơi: " + ex.Message });
            }
        }
    }

    // Class hứng dữ liệu từ phía Client gửi lên (DTO)
    public class CreatePoiRequest
    {
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}