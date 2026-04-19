using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.API.Services;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ISyncService _syncService;
        private readonly string _offlinePacksDirectory;
        private const string PACK_FILENAME = "VinhKhanh_OfflinePack.zip";
        private const string HASH_FILENAME = "VinhKhanh_OfflinePack.sha256.txt";

        public SyncController(AppDbContext context, IWebHostEnvironment env, ISyncService syncService)
        {
            _context = context;
            _env = env;
            _syncService = syncService;
            _offlinePacksDirectory = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "offline-packs");
        }

        // API: Check Latest Version of Approved POIs
        [HttpGet("public/sync/version")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLatestSyncVersion()
        {
            try
            {
                // Tính MAX(LastUpdated) trực tiếp trên DB — không load toàn bộ entity vào RAM
                var maxLastUpdated = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .MaxAsync(p => (DateTime?)p.LastUpdated);

                string versionString = maxLastUpdated.HasValue
                    ? maxLastUpdated.Value.ToString("yyyyMMddHHmmss")
                    : "1.0.0";

                return Ok(new { version = versionString });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi kiểm tra phiên bản: " + ex.Message });
            }
        }

        // API: Generate and Download Offline ZIP Pack
        [HttpGet("public/sync/download-zip")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadOfflineZip()
        {
            try
            {
                var manifest = await _syncService.GetManifestAsync();
                var packPath = _syncService.GetOfflinePackPath();
                if (manifest == null || !System.IO.File.Exists(packPath))
                {
                    return NotFound(new { Message = "Offline pack is not available. Please contact the admin to generate the pack." });
                }

                try
                {
                    // Add hash to response headers
                    Response.Headers.Append("X-SHA256-Hash", manifest.SHA256Hash);
                    return PhysicalFile(packPath, "application/zip", PACK_FILENAME);
                }
                catch (IOException ex)
                {
                    return StatusCode(500, new { Message = $"Error reading offline pack: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Error serving offline pack: {ex.Message}" });
            }
        }

        // API: Admin endpoint to generate and cache the Offline ZIP Pack
        [HttpPost("admin/sync/generate-pack")]
        [Authorize]
        public async Task<IActionResult> GenerateOfflinePack()
        {
            try
            {
                // Verify user has Admin role
                var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(roleName) || roleName != "Admin")
                {
                    return Forbid();
                }

                var packPath = Path.Combine(_offlinePacksDirectory, PACK_FILENAME);
                var hashPath = Path.Combine(_offlinePacksDirectory, HASH_FILENAME);

                try
                {
                    await _syncService.GenerateOfflinePackAsync();
                    var hashHex = (await System.IO.File.ReadAllTextAsync(hashPath)).Trim();

                    // Get file info for response
                    var fileInfo = new FileInfo(packPath);
                    return Ok(new
                    {
                        Message = "Offline pack generated successfully.",
                        FileName = PACK_FILENAME,
                        FileSizeBytes = fileInfo.Length,
                        SHA256Hash = hashHex,
                        GeneratedAt = DateTime.UtcNow
                    });
                }
                catch (IOException ex)
                {
                    return StatusCode(500, new { Message = $"Error creating ZIP file: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = $"Error generating offline pack: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpGet("public/sync/manifest")]
        [AllowAnonymous]
        public async Task<IActionResult> GetManifest()
        {
            var manifest = await _syncService.GetManifestAsync();
            if (manifest == null)
            {
                return NotFound(new { Message = "Offline pack is not available yet." });
            }

            return Ok(manifest);
        }

        [HttpGet("admin/sync/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSyncStatus()
        {
            var status = await _syncService.GetStatusAsync();
            return Ok(status);
        }

        [HttpPost("logs")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateNarrationLog([FromBody] NarrationLogRequestDto request)
        {
            if (request.PoiId <= 0 || string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return BadRequest(new { Message = "PoiId và DeviceId là bắt buộc." });
            }

            var poiExists = await _context.Pois.AnyAsync(p => p.Id == request.PoiId && p.Status == PoiStatus.Approved);
            if (!poiExists)
            {
                return NotFound(new { Message = "POI không tồn tại hoặc chưa được duyệt." });
            }

            var log = new NarrationLog
            {
                PoiId = request.PoiId,
                DeviceId = request.DeviceId,
                Timestamp = request.Timestamp ?? DateTime.UtcNow
            };
            _context.NarrationLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã ghi nhận lượt nghe.", Id = log.Id });
        }

        // Ghi nhận lượt quét QR Code
        [HttpPost("public/logs/scan")]
        [AllowAnonymous]
        public async Task<IActionResult> LogQrScan([FromBody] NarrationLogRequestDto request)
        {
            if (request == null || request.PoiId <= 0 || string.IsNullOrEmpty(request.DeviceId))
            {
                return BadRequest(new { Message = "Dữ liệu không hợp lệ." });
            }

            var log = new QrScanLog
            {
                PoiId = request.PoiId,
                DeviceId = request.DeviceId,
                Timestamp = request.Timestamp ?? DateTime.UtcNow
            };
            _context.QrScanLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã ghi nhận lượt quét QR.", Id = log.Id });
        }

        [HttpGet("owner/stats/listens")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetOwnerListenStats([FromQuery] string mode = "listen")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var ownerId))
            {
                return Unauthorized(new { Message = "Không thể xác định người dùng." });
            }

            var stats = await _context.Pois
                .Where(p => p.OwnerId == ownerId)
                .Select(p => new OwnerListenStatsDto
                {
                    PoiId = p.Id,
                    PoiName = p.Name,
                    ListenCount = mode.ToLower() == "scan" 
                        ? _context.QrScanLogs.Where(n => n.PoiId == p.Id && n.DeviceId != null)
                            .Select(n => n.DeviceId).Distinct().Count()
                        : _context.NarrationLogs.Count(n => n.PoiId == p.Id)
                })
                .ToListAsync();

            return Ok(stats);
        }

        [HttpGet("owner/stats/listens/trend")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetOwnerListenTrend([FromQuery] string type = "day", [FromQuery] string mode = "listen")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var ownerId))
            {
                return Unauthorized(new { Message = "Không thể xác định người dùng." });
            }

            var ownerPoiIds = await _context.Pois
                .Where(p => p.OwnerId == ownerId)
                .Select(p => p.Id)
                .ToListAsync();

            if (!ownerPoiIds.Any())
            {
                return Ok(new List<ListenTrendDto>());
            }

            var trend = new List<ListenTrendDto>();

            // Convert to list of DateTime based on mode
            List<DateTime> logDates;
            if (mode.ToLower() == "scan") 
            {
                 logDates = await _context.QrScanLogs
                    .Where(n => ownerPoiIds.Contains(n.PoiId))
                    .Select(n => n.Timestamp)
                    .ToListAsync();
            }
            else
            {
                 logDates = await _context.NarrationLogs
                    .Where(n => ownerPoiIds.Contains(n.PoiId))
                    .Select(n => n.Timestamp)
                    .ToListAsync();
            }

            if (type.ToLower() == "month")
            {
                // Last 12 months
                var startDate = DateTime.UtcNow.AddMonths(-11);
                var startDateMonth = new DateTime(startDate.Year, startDate.Month, 1);
                
                var logsFiltered = logDates.Where(d => d >= startDateMonth).ToList();
                
                var grouped = logsFiltered
                    .GroupBy(d => new { d.Year, d.Month })
                    .Select(g => new ListenTrendDto
                    {
                        Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                        ListenCount = g.Count()
                    })
                    .ToList();

                // Fill gaps
                for (int i = 0; i < 12; i++)
                {
                    var d = startDateMonth.AddMonths(i);
                    var label = $"{d.Month:D2}/{d.Year}";
                    if (!grouped.Any(x => x.Label == label))
                    {
                        grouped.Add(new ListenTrendDto { Label = label, ListenCount = 0 });
                    }
                }
                
                // Sort chronologically (MM/yyyy string sorting won't work well directly without parsing, so order by parsed date)
                trend = grouped.OrderBy(x => DateTime.ParseExact(x.Label, "MM/yyyy", null)).ToList();
            }
            else
            {
                // Last 30 days
                var startDate = DateTime.UtcNow.Date.AddDays(-29);
                var logsFiltered = logDates.Where(d => d >= startDate).ToList();

                var grouped = logsFiltered
                    .GroupBy(d => d.Date)
                    .Select(g => new ListenTrendDto
                    {
                        Label = g.Key.ToString("dd/MM"),
                        ListenCount = g.Count()
                    })
                    .ToList();

                // Fill gaps for 30 days
                for (int i = 0; i < 30; i++)
                {
                    var d = startDate.AddDays(i);
                    var label = d.ToString("dd/MM");
                    if (!grouped.Any(x => x.Label == label))
                    {
                        grouped.Add(new ListenTrendDto { Label = label, ListenCount = 0 });
                    }
                }

                trend = grouped.OrderBy(x => DateTime.ParseExact(x.Label, "dd/MM", null)).ToList();
            }

            return Ok(trend);
        }

        // API: Kiểm tra phiên bản dữ liệu (Dùng mã SHA-256 làm Version)
        // Mobile App gọi API này mỗi khi khởi động
        [HttpGet("public/version")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLatestVersion()
        {
            try
            {
                // Trỏ tới file text chứa mã SHA-256 mà hàm Generate Pack đã tạo ra
                var hashFilePath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "offline-packs", "VinhKhanh_OfflinePack.sha256.txt");

                // Trường hợp Admin chưa từng bấm Generate lần nào
                if (!System.IO.File.Exists(hashFilePath))
                {
                    return Ok(new 
                    { 
                        version = "none", 
                        message = "Chưa có gói dữ liệu offline nào được tạo." 
                    });
                }

                // Đọc mã hash từ đĩa (Tốn 0.001s, không dùng đến Database)
                var currentHash = await System.IO.File.ReadAllTextAsync(hashFilePath);

                // Lấy thêm ngày giờ file được tạo để App có thể hiển thị: "Bản cập nhật ngày..."
                var lastUpdated = System.IO.File.GetLastWriteTimeUtc(hashFilePath);

                return Ok(new 
                { 
                    version = currentHash.Trim(), 
                    lastUpdated = lastUpdated
                });
            }
            catch (Exception ex)
            {
                // In production, use proper logging
                return StatusCode(500, new { message = "Lỗi khi lấy phiên bản: " + ex.Message });
            }
        }
    }
}