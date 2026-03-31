using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.Models;
using VinhKhanhFoodTour.DTOs;
namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Owner")] // Chỉ Admin và Chủ quán mới được vào đây
    public class PoiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PoiController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // API: Thêm một quán ăn mới
        [HttpPost]
        public async Task<IActionResult> CreateNewPoi([FromBody] CreatePoiRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var ownerId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });
                }

                // 1. Tạo thực thể POI (Quán ăn)
                var newPoi = new Poi
                {
                    OwnerId = ownerId,
                    Name = request.Name,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Status = PoiStatus.Pending, // Mặc định chờ duyệt
                    TriggerRadius = 50,  // Bán kính kích hoạt 50m
                    LastUpdated = DateTime.UtcNow,
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

        // API: Lấy danh sách các quán đang chờ duyệt (Admin only)
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPois()
        {
            try
            {
                var pendingPois = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Pending)
                    .Select(p => new PoiDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        Translations = p.Translations.Select(t => new PoiTranslationDto
                        {
                            Id = t.Id,
                            LanguageCode = t.LanguageCode, // Sửa lại tên biến cho khớp với Entity của bạn
                            Title = t.Title,
                            Description = t.Description
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(pendingPois);
            }
            catch (Exception ex)
            {
                // Trong thực tế nên dùng ILogger để log lỗi ex.Message lại
                return StatusCode(500, "Đã xảy ra lỗi khi lấy danh sách quán ăn chờ duyệt.");
            }
        }

        // API: Duyệt một quán ăn (Admin only)
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePoi(int id)
        {
            try
            {
                var poi = await _context.Pois.FindAsync(id);

                if (poi == null)
                {
                    return NotFound(new { Message = $"Không tìm thấy quán với ID: {id}" });
                }

                poi.Status = PoiStatus.Approved;
                poi.LastUpdated = DateTime.UtcNow;
                _context.Pois.Update(poi);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Đã duyệt quán ăn thành công!", Id = poi.Id });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Lỗi cơ sở dữ liệu: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi duyệt quán: " + ex.Message });
            }
        }

        // API: Từ chối một quán ăn (Admin only)
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectPoi(int id, [FromBody] RejectPoiDto request)
        {
            try
            {
                var poi = await _context.Pois.FindAsync(id);

                if (poi == null)
                {
                    return NotFound(new { Message = $"Không tìm thấy quán với ID: {id}" });
                }

                poi.Status = PoiStatus.Rejected;
                poi.RejectionReason = request.Reason;
                poi.LastUpdated = DateTime.UtcNow;
                _context.Pois.Update(poi);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Đã từ chối quán ăn thành công!", Id = poi.Id });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Lỗi cơ sở dữ liệu: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi từ chối quán: " + ex.Message });
            }
        }

        // API: Chủ quán tạo một quán ăn mới (Owner/Admin)
        [HttpPost("owner/create")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> CreatePoi([FromBody] CreatePoiDto request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var ownerId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var newPoi = new Poi
                {
                    OwnerId = ownerId,
                    Name = request.Name,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Status = PoiStatus.Pending,
                    TriggerRadius = 50,
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

                _context.Pois.Add(newPoi);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(CreatePoi), new { id = newPoi.Id }, new { Message = "Đã tạo quán ăn thành công! Chờ Admin duyệt.", Id = newPoi.Id });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Lỗi cơ sở dữ liệu: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tạo quán: " + ex.Message });
            }
        }

        // API: Chủ quán cập nhật quán ăn của mình (Owner only)
        [HttpPut("owner/{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdatePoi(int id, [FromBody] UpdatePoiDto request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var currentUserId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var poi = await _context.Pois.FindAsync(id);
                if (poi == null)
                {
                    return NotFound(new { Message = $"Không tìm thấy quán với ID: {id}" });
                }

                if (poi.OwnerId != currentUserId)
                {
                    return Forbid();
                }

                poi.Name = request.Name;
                poi.Latitude = request.Latitude;
                poi.Longitude = request.Longitude;
                poi.Status = PoiStatus.Pending;
                poi.RejectionReason = null;
                poi.LastUpdated = DateTime.UtcNow;

                _context.Pois.Update(poi);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Đã cập nhật quán ăn thành công! Chờ Admin duyệt lại.", Id = poi.Id });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Lỗi cơ sở dữ liệu: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi cập nhật quán: " + ex.Message });
            }
        }

        // API: Lấy danh sách quán ăn của chủ quán hiện tại (Owner only)
        [HttpGet("owner/my-pois")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetMyPois()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var ownerIdForList))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var myPois = await _context.Pois
                    .Where(p => p.OwnerId == ownerIdForList)
                    .Select(p => new PoiDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        Translations = p.Translations.Select(t => new PoiTranslationDto
                        {
                            Id = t.Id,
                            LanguageCode = t.LanguageCode,
                            Title = t.Title,
                            Description = t.Description
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(myPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán của bạn: " + ex.Message });
            }
        }

        // API: Lấy danh sách các quán ăn đã được duyệt (Public)
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicPois()
        {
            try
            {
                var approvedPois = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .Select(p => new PoiDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        Translations = p.Translations.Select(t => new PoiTranslationDto
                        {
                            Id = t.Id,
                            LanguageCode = t.LanguageCode,
                            Title = t.Title,
                            Description = t.Description
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(approvedPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán công khai: " + ex.Message });
            }
        }

        // API: Lấy danh sách các chân dung quán trên bản đồ (Public - tối ưu cho bản đồ)
        [HttpGet("public/map-pins")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMapPins()
        {
            try
            {
                var mapPins = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .Select(p => new MapPinDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude
                    })
                    .ToListAsync();

                return Ok(mapPins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách chân dung quán: " + ex.Message });
            }
        }

        // API: Lấy danh sách các quán ăn gần người dùng (Public - dựa trên vị trí)
        [HttpGet("public/nearby")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearbyPois([FromQuery] double userLat, [FromQuery] double userLng, [FromQuery] double radiusInMeters = 50)
        {
            try
            {
                // Lấy tất cả các quán đã được duyệt
                var approvedPois = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .Select(p => new PoiDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Status = p.Status,
                        RejectionReason = p.RejectionReason,
                        Translations = p.Translations.Select(t => new PoiTranslationDto
                        {
                            Id = t.Id,
                            LanguageCode = t.LanguageCode,
                            Title = t.Title,
                            Description = t.Description
                        }).ToList()
                    })
                    .ToListAsync();

                // Lọc các quán nằm trong bán kính
                var nearbyPois = approvedPois
                    .Where(p => CalculateDistance(userLat, userLng, p.Latitude, p.Longitude) <= radiusInMeters)
                    .ToList();

                return Ok(nearbyPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán gần đây: " + ex.Message });
            }
        }

        // API: Chủ quán tải lên tệp media (ảnh/âm thanh) cho bản dịch của quán (Owner only)
        [HttpPost("owner/{id}/translations/{languageCode}/media")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UploadMediaForTranslation(int id, string languageCode, IFormFile? imageFile, IFormFile? audioFile)
        {
            try
            {
                // 1. Extract current user's ID from JWT claims
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });
                }

                // 2. Find the POI by id asynchronously
                var poi = await _context.Pois.FindAsync(id);
                if (poi == null)
                {
                    return NotFound(new { Message = $"Không tìm thấy quán với ID: {id}" });
                }

                // 3. Verify ownership
                if (poi.OwnerId != currentUserId)
                {
                    return Forbid();
                }

                // 4. Find the PoiTranslation associated with this POI and the given languageCode
                var translation = await _context.PoiTranslations
                    .FirstOrDefaultAsync(t => t.PoiId == id && t.LanguageCode == languageCode);

                if (translation == null)
                {
                    return NotFound(new { Message = $"Không tìm thấy bản dịch cho ngôn ngữ: {languageCode}" });
                }

                // 5. Handle image file upload if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Validate extension
                    var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var imageExtension = Path.GetExtension(imageFile.FileName).ToLower();

                    if (!allowedImageExtensions.Contains(imageExtension))
                    {
                        return BadRequest(new { Message = "Định dạng ảnh không hợp lệ. Chỉ cho phép: .jpg, .jpeg, .png" });
                    }

                    // Generate unique filename using GUID
                    var uniqueImageFileName = $"{Guid.NewGuid()}{imageExtension}";
                    var imageDirectory = Path.Combine(_env.WebRootPath, "uploads", "images");

                    // Ensure the directory exists
                    if (!Directory.Exists(imageDirectory))
                    {
                        Directory.CreateDirectory(imageDirectory);
                    }

                    var imageFilePath = Path.Combine(imageDirectory, uniqueImageFileName);

                    // Save file asynchronously with using statement
                    using (var fileStream = new FileStream(imageFilePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    // Update the translation's ImageUrl
                    translation.ImageUrl = $"/uploads/images/{uniqueImageFileName}";
                }

                // 6. Handle audio file upload if provided
                if (audioFile != null && audioFile.Length > 0)
                {
                    // Validate extension
                    var allowedAudioExtensions = new[] { ".mp3", ".wav" };
                    var audioExtension = Path.GetExtension(audioFile.FileName).ToLower();

                    if (!allowedAudioExtensions.Contains(audioExtension))
                    {
                        return BadRequest(new { Message = "Định dạng âm thanh không hợp lệ. Chỉ cho phép: .mp3, .wav" });
                    }

                    // Generate unique filename using GUID
                    var uniqueAudioFileName = $"{Guid.NewGuid()}{audioExtension}";
                    var audioDirectory = Path.Combine(_env.WebRootPath, "uploads", "audio");

                    // Ensure the directory exists
                    if (!Directory.Exists(audioDirectory))
                    {
                        Directory.CreateDirectory(audioDirectory);
                    }

                    var audioFilePath = Path.Combine(audioDirectory, uniqueAudioFileName);

                    // Save file asynchronously with using statement
                    using (var fileStream = new FileStream(audioFilePath, FileMode.Create))
                    {
                        await audioFile.CopyToAsync(fileStream);
                    }

                    // Update the translation's AudioFilePath
                    translation.AudioFilePath = $"/uploads/audio/{uniqueAudioFileName}";
                }

                // Check if at least one file was uploaded
                if (imageFile == null && audioFile == null)
                {
                    return BadRequest(new { Message = "Vui lòng cung cấp ít nhất một tệp ảnh hoặc âm thanh." });
                }

                // 7. Save changes to database
                _context.PoiTranslations.Update(translation);
                await _context.SaveChangesAsync();

                // Return Ok with updated ImageUrl and AudioFilePath
                return Ok(new
                {
                    Message = "Đã tải lên tệp media thành công!",
                    ImageUrl = translation.ImageUrl,
                    AudioFilePath = translation.AudioFilePath
                });
            }
            catch (IOException ioEx)
            {
                return StatusCode(500, new { Message = "Lỗi tệp hệ thống: " + ioEx.Message });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { Message = "Lỗi cơ sở dữ liệu: " + dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tải lên tệp media: " + ex.Message });
            }
        }

        // Helper: Tính toán khoảng cách giữa hai tọa độ bằng công thức Haversine (đơn vị: mét)
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusMeters = 6371000; // Bán kính Trái Đất tính bằng mét

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = EarthRadiusMeters * c;

            return distance;
        }

        // Helper: Chuyển đổi độ sang radian
        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
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

    // DTO để nhận lý do từ chối quán ăn
    public class RejectPoiDto
    {
        public string Reason { get; set; } = null!;
    }

    // DTO để chủ quán tạo quán ăn mới
    public class CreatePoiDto
    {
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

    // DTO để chủ quán cập nhật quán ăn của mình
    public class UpdatePoiDto
    {
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
    // DTO nhẹ cho bản đồ (chỉ chứa Id, Name, Latitude, Longitude)
    public class MapPinDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}