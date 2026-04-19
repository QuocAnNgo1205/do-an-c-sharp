using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.API.Services;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    // TÔI ĐÃ BỎ [Authorize] Ở ĐÂY để App mobile có thể xem danh sách quán mà không cần đăng nhập
    public class PoiController : ControllerBase
    {
        private readonly IPoiService _poiService;
        private readonly IMediaService _mediaService;
        private readonly ISyncOrchestrator _syncOrchestrator;
        private readonly AppDbContext _db;
        private readonly ILogger<PoiController> _logger;

        public PoiController(IPoiService poiService, IMediaService mediaService, ISyncOrchestrator syncOrchestrator, AppDbContext db, ILogger<PoiController> logger)
        {
            _poiService = poiService;
            _mediaService = mediaService;
            _syncOrchestrator = syncOrchestrator;
            _db = db;
            _logger = logger;
        }


        // ======================================================================
        // CÁC HÀM TÔI THÊM VÀO ĐỂ SỬA LỖI CHO BẠN
        // ======================================================================

        // API MỚI 1: Để Mobile App gọi được api/v1/Poi (Sửa lỗi 405 Method Not Allowed)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllPoisForMobile()
        {
            try
            {
                var approvedPois = await _poiService.GetPublicPoisAsync();
                return Ok(approvedPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách: " + ex.Message });
            }
        }

        // Đã xóa bỏ hàm DeletePoi và dấu gạch chéo '/' bị lỗi ở đây

        // API: Thêm một quán ăn mới
        [HttpPost]
        [Authorize(Roles = "Admin,Owner")]
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

                var newPoi = await _poiService.CreatePoiAsync(ownerId, request);

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
                var pendingPois = await _poiService.GetPendingPoisAsync();
                return Ok(pendingPois);
            }
            catch (Exception)
            {
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
                var poi = await _poiService.ApprovePoiAsync(id);
                return Ok(new { Message = "Đã duyệt quán ăn thành công!", Id = poi.Id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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
                var poi = await _poiService.RejectPoiAsync(id, request.Reason);
                return Ok(new { Message = "Đã từ chối quán ăn thành công!", Id = poi.Id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi từ chối quán: " + ex.Message });
            }
        }

        // API: Chủ quán tạo một quán ăn mới (Owner/Admin) - Hỗ trợ upload ảnh
        [HttpPost("owner/create")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> CreatePoi(
            [FromForm] string name, 
            [FromForm] double latitude, 
            [FromForm] double longitude, 
            [FromForm] string title, 
            [FromForm] string description, 
            IFormFile? imageFile)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var ownerId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                // Ghi log nhận dữ liệu
                _logger.LogDebug("[CreatePoi] Received: Name={Name}, ImageFile={ImageFile}, ImageFile.Length={ImageLength}",
                    name, imageFile?.FileName, imageFile?.Length);

                // Xử lý upload ảnh nếu có
                string? imageUrl = null;
                if (imageFile != null && imageFile.Length > 0)
                {
                    _logger.LogDebug("[CreatePoi] Processing image: {FileName} ({Length} bytes)", imageFile.FileName, imageFile.Length);
                    var (savedImageUrl, _) = await _mediaService.SaveMediaAsync(imageFile, null);
                    imageUrl = savedImageUrl;
                    _logger.LogDebug("[CreatePoi] Image saved as: {ImageUrl}", imageUrl);
                }
                else
                {
                    _logger.LogDebug("[CreatePoi] No image file provided");
                }

                // Tạo CreatePoiDto với ImageUrl
                var poiDto = new CreatePoiDto
                {
                    Name = name,
                    Latitude = latitude,
                    Longitude = longitude,
                    Title = title,
                    Description = description,
                    ImageUrl = imageUrl
                };

                _logger.LogDebug("[CreatePoi] CreatePoiDto prepared with ImageUrl={ImageUrl}", poiDto.ImageUrl);

                var newPoi = await _poiService.CreateOwnerPoiAsync(ownerId, poiDto);

                _logger.LogDebug("[CreatePoi] POI created: Id={Id}, ImageUrl={ImageUrl}", newPoi.Id, newPoi.ImageUrl);

                return CreatedAtAction(nameof(CreatePoi), new { id = newPoi.Id }, new { Message = "Đã tạo quán ăn thành công! Chờ Admin duyệt.", Id = newPoi.Id, ImageUrl = imageUrl });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tạo quán: " + ex.Message });
            }
        }

        // API: Chủ quán cập nhật quán ăn của mình (Owner only) - Hỗ trợ [FromForm] để upload ảnh
        [HttpPut("owner/{id}")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> UpdatePoi(int id, [FromForm] UpdatePoiFormRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var currentUserId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                string? imageUrl = null;
                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    var (savedImageUrl, _) = await _mediaService.SaveMediaAsync(request.ImageFile, null);
                    imageUrl = savedImageUrl;
                }

                var poi = await _poiService.UpdatePoiAsync(id, currentUserId, request, imageUrl);

                return Ok(new { Message = "Đã cập nhật quán ăn thành công! Chờ Admin duyệt lại.", Id = poi.Id });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi cập nhật quán: " + ex.Message });
            }
        }

        // API: Xóa một quán ăn (Owner/Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> DeletePoi(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var isAdmin = User.IsInRole("Admin");
                await _poiService.DeletePoiAsync(id, userId, isAdmin);

                return Ok(new { Message = "Đã xóa quán ăn và các dữ liệu liên quan thành công!" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi xóa quán: " + ex.Message });
            }
        }

        // API: Lấy thông tin POI dành cho Owner editing (không filter Approved)
        [HttpGet("owner/{id}")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> GetOwnerPoiById(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var isAdmin = User.IsInRole("Admin");
                var poi = await _poiService.GetPoiByIdAsync(id, userId, isAdmin);
                
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var dto = new PoiDto
                {
                    Id = poi.Id,
                    Name = poi.Name,
                    Latitude = poi.Latitude,
                    Longitude = poi.Longitude,
                    ImageUrl = string.IsNullOrEmpty(poi.ImageUrl) ? "" : (poi.ImageUrl.StartsWith("http") ? poi.ImageUrl : baseUrl.TrimEnd('/') + "/" + poi.ImageUrl.TrimStart('/')),
                    Status = poi.Status,
                    RejectionReason = poi.RejectionReason,
                    Translations = poi.Translations.Select(t => new PoiTranslationDto
                    {
                        Id = t.Id,
                        LanguageCode = t.LanguageCode,
                        Title = t.Title,
                        Description = t.Description ?? string.Empty
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy thông tin quán: " + ex.Message });
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

                var myPois = await _poiService.GetOwnerPoisAsync(ownerIdForList);

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
                var approvedPois = await _poiService.GetPublicPoisAsync();
                return Ok(approvedPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán công khai: " + ex.Message });
            }
        }

        // API: Lấy thông tin chi tiết một quán ăn công khai (Public)
        [HttpGet("public/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicPoiById(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                int? currentUserId = null;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var uId))
                {
                    currentUserId = uId;
                }
                var isAdmin = User.IsInRole("Admin");

                var poi = await _poiService.GetPublicPoiByIdAsync(id, currentUserId, isAdmin);
                return Ok(poi);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy thông tin chi tiết quán: " + ex.Message });
            }
        }

        // API: Lấy danh sách các chân dung quán trên bản đồ (Public - tối ưu cho bản đồ)
        [HttpGet("public/map-pins")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMapPins()
        {
            try
            {
                var mapPins = await _poiService.GetMapPinsAsync();
                return Ok(mapPins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách chân dung quán: " + ex.Message });
            }
        }

        // API: Lấy danh sách các quán ăn gần người dùng (Public - dựa trên vị trí)
        [HttpGet("overview-pins")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetOverviewMapPins()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized(new { Message = "Không thể xác định người dùng." });
            }

            var isAdmin = User.IsInRole("Admin");
            var pins = await _poiService.GetOverviewMapPinsAsync(currentUserId, isAdmin);
            return Ok(pins);
        }

        [HttpGet("public/nearby")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearbyPois([FromQuery] double userLat, [FromQuery] double userLng, [FromQuery] double radiusInMeters = 50)
        {
            try
            {
                var nearbyPois = await _poiService.GetNearbyPoisAsync(userLat, userLng, radiusInMeters);
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
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });
                }

                if (imageFile == null && audioFile == null)
                {
                    return BadRequest(new { Message = "Vui lòng cung cấp ít nhất một tệp ảnh hoặc âm thanh." });
                }

                var (poi, translation) = await _poiService.GetOwnerTranslationAsync(id, languageCode, currentUserId);
                var saved = await _mediaService.SaveMediaAsync(imageFile, audioFile);
                if (saved.imageUrl != null)
                {
                    translation.ImageUrl = saved.imageUrl;
                }
                if (saved.audioFilePath != null)
                {
                    translation.AudioFilePath = saved.audioFilePath;
                }

                await _poiService.SaveTranslationAsync(translation);
                if (poi.Status == PoiStatus.Approved)
                {
                    await _syncOrchestrator.TryRefreshOfflinePackAsync();
                }

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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tải lên tệp media: " + ex.Message });
            }
        }
    }

    // ── Unused placeholder kept for line-count compatibility ──

    // Extension: analytics endpoint added below the class is intentional;
    // the real implementation is appended correctly via the partial below.
}

// ── Analytics DTO (file-scoped for simplicity) ──────────────────────────────
namespace VinhKhanhFoodTour.API.Controllers
{
    public static class PoiAnalyticsExtensions { } // intentionally empty

    [Route("api/v1/Poi")]
    [ApiController]
    public class PoiAnalyticsController : ControllerBase
    {
        private readonly VinhKhanhFoodTour.Data.AppDbContext _db;
        public PoiAnalyticsController(VinhKhanhFoodTour.Data.AppDbContext db) => _db = db;

        // GET /api/v1/Poi/analytics
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetPoiAnalytics()
        {
            try
            {
                // Robust admin check
                var isAdmin = User.IsInRole("Admin") || 
                              User.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                int? ownerId = null;
                if (!isAdmin && int.TryParse(userIdStr, out var uId))
                {
                    ownerId = uId;
                }

                // Filter Narration Logs
                var narrationQuery = _db.NarrationLogs.AsQueryable();
                if (ownerId.HasValue) narrationQuery = narrationQuery.Where(n => n.Poi!.OwnerId == ownerId.Value);

                var narrationCounts = await narrationQuery
                    .GroupBy(n => new { n.PoiId, n.Poi!.Name })
                    .Select(g => new 
                    { 
                        g.Key.PoiId, 
                        g.Key.Name, 
                        Count = g.Count(),
                        AvgDuration = g.Average(x => x.ListenDurationSeconds)
                    })
                    .ToListAsync();

                // Filter QR Scan Logs
                var qrQuery = _db.QrScanLogs.AsQueryable();
                if (ownerId.HasValue) qrQuery = qrQuery.Where(q => q.Poi!.OwnerId == ownerId.Value);

                var qrCounts = await qrQuery
                    .GroupBy(q => q.PoiId)
                    .Select(g => new { PoiId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Filter POIs
                var poiQuery = _db.Pois.AsQueryable();
                if (ownerId.HasValue) poiQuery = poiQuery.Where(p => p.OwnerId == ownerId.Value);

                var allPois = await poiQuery
                    .Select(p => new { p.Id, p.Name })
                    .ToListAsync();

                var poiStats = allPois.Select(p => 
                {
                    var nStat = narrationCounts.FirstOrDefault(n => n.PoiId == p.Id);
                    return new
                    {
                        poiId = p.Id,
                        poiName = p.Name,
                        listenCount = nStat?.Count ?? 0,
                        qrScanCount = qrCounts.FirstOrDefault(q => q.PoiId == p.Id)?.Count ?? 0,
                        avgListenTime = nStat?.AvgDuration ?? 0
                    };
                })
                .OrderByDescending(x => x.listenCount + x.qrScanCount)
                .ToList();

                var cutoff   = DateTime.UtcNow.AddMonths(-11);
                
                // Filter Monthly Trend
                var trendQuery = _db.NarrationLogs.Where(n => n.Timestamp >= new DateTime(cutoff.Year, cutoff.Month, 1));
                if (ownerId.HasValue) trendQuery = trendQuery.Where(n => n.Poi!.OwnerId == ownerId.Value);

                var monthlyRaw = await trendQuery
                    .GroupBy(n => new { n.Timestamp.Year, n.Timestamp.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                    .ToListAsync();

                var monthlyTrend = Enumerable.Range(0, 12)
                    .Select(i =>
                    {
                        var d   = DateTime.UtcNow.AddMonths(-(11 - i));
                        var cnt = monthlyRaw.FirstOrDefault(m => m.Year == d.Year && m.Month == d.Month)?.Count ?? 0;
                        return new { label = d.ToString("MM/yyyy"), count = cnt };
                    })
                    .ToList();

                return Ok(new { poiStats, monthlyTrend });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy thống kê: " + ex.Message });
            }
        }

        // GET /api/v1/Poi/owner/qr-manage
        [HttpGet("owner/qr-manage")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetOwnerQrManagement()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var ownerId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                // Get owner's POIs with unique QR scan logic
                var pois = await _db.Pois
                    .Where(p => p.OwnerId == ownerId)
                    .Select(p => new
                    {
                        poiId = p.Id,
                        poiName = p.Name,
                        // Count DISTINCT DeviceId from QrScanLogs for this POI
                        uniqueScanCount = _db.QrScanLogs
                            .Where(q => q.PoiId == p.Id && q.DeviceId != null)
                            .Select(q => q.DeviceId)
                            .Distinct()
                            .Count(),
                        // Total overall scans just for reference (optional, keeping it simple if needed)
                        totalScanCount = _db.QrScanLogs.Count(q => q.PoiId == p.Id)
                    })
                    .OrderByDescending(x => x.uniqueScanCount)
                    .ToListAsync();

                return Ok(pois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy dữ liệu QR: " + ex.Message });
            }
        }

        // GET /api/v1/Poi/admin/qr-manage
        [HttpGet("admin/qr-manage")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminQrManagement()
        {
            try
            {
                // Get all POIs with unique QR scan logic
                var pois = await _db.Pois
                    .Select(p => new
                    {
                        poiId = p.Id,
                        poiName = p.Name,
                        uniqueScanCount = _db.QrScanLogs
                            .Where(q => q.PoiId == p.Id && q.DeviceId != null)
                            .Select(q => q.DeviceId)
                            .Distinct()
                            .Count(),
                        totalScanCount = _db.QrScanLogs.Count(q => q.PoiId == p.Id)
                    })
                    .OrderByDescending(x => x.uniqueScanCount)
                    .ToListAsync();

                return Ok(pois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy dữ liệu QR hệ thống: " + ex.Message });
            }
        }
        // POST /api/v1/Poi/user-location
        [HttpPost("user-location")]
        [AllowAnonymous]
        public async Task<IActionResult> PostUserLocation([FromBody] UserLocationRequest request)
        {
            try
            {
                var log = new UserLocationLog
                {
                    DeviceId = request.DeviceId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    CurrentTourId = request.CurrentTourId,
                    Timestamp = DateTime.UtcNow
                };

                _db.UserLocationLogs.Add(log);
                await _db.SaveChangesAsync();
                return Ok(new { Message = "Location recorded." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error recording location: " + ex.Message });
            }
        }

        // GET /api/v1/Poi/user-heatmap
        [HttpGet("user-heatmap")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserHeatmap()
        {
            try
            {
                // Get last 24h location data for heatmap
                var cutoff = DateTime.UtcNow.AddHours(-24);
                var points = await _db.UserLocationLogs
                    .Where(l => l.Timestamp >= cutoff)
                    .Select(l => new HeatmapPointDto
                    {
                        Lat = l.Latitude,
                        Lng = l.Longitude,
                        Intensity = 1.0
                    })
                    .ToListAsync();

                return Ok(points);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error fetching heatmap data: " + ex.Message });
            }
        }

        // POST /api/v1/Poi/seed-user-locations
        [HttpPost("seed-user-locations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SeedUserLocations()
        {
            try
            {
                // Clear existing logs
                _db.UserLocationLogs.RemoveRange(_db.UserLocationLogs);
                await _db.SaveChangesAsync();

                var random = new Random();
                var logs = new List<UserLocationLog>();
                var pois = await _db.Pois.ToListAsync();

                if (pois.Any())
                {
                    // Seeding logic (concentrated around POIs as requested)
                    foreach (var poi in pois)
                    {
                        int crowdSize = random.Next(20, 40);
                        for (int i = 0; i < crowdSize; i++)
                        {
                            logs.Add(new UserLocationLog
                            {
                                DeviceId = $"Tourist-{poi.Id}-{i}",
                                Latitude = poi.Latitude + (random.NextDouble() - 0.5) * 0.001,
                                Longitude = poi.Longitude + (random.NextDouble() - 0.5) * 0.001,
                                Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(5, 120))
                            });
                        }
                    }

                    // Vinh Khanh street walkers
                    for (int j = 0; j < 50; j++)
                    {
                        double ratio = random.NextDouble();
                        double midLat = pois[0].Latitude + (pois[1].Latitude - pois[0].Latitude) * ratio;
                        double midLng = pois[0].Longitude + (pois[1].Longitude - pois[0].Longitude) * ratio;

                        logs.Add(new UserLocationLog
                        {
                            DeviceId = $"Walker-{j}",
                            Latitude = midLat + (random.NextDouble() - 0.5) * 0.0006,
                            Longitude = midLng + (random.NextDouble() - 0.5) * 0.0006,
                            Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(0, 60))
                        });
                    }
                }

                _db.UserLocationLogs.AddRange(logs);
                await _db.SaveChangesAsync();

                return Ok(new { Message = "Heatmap data seeded sample successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error seeding heatmap: " + ex.Message });
            }
        }
    }
}