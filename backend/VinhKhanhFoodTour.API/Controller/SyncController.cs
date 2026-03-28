using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.Models;
using System.IO.Compression;
using System.Security.Cryptography;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SyncController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/v1/sync/pois
        [HttpGet("pois")]
        public async Task<IActionResult> GetPoisForSync()
        {
            // Chỉ lấy các quán đã được Admin duyệt (Approved)
            var pois = await _context.Pois
                .Where(p => p.Status == PoiStatus.Approved)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Latitude,
                    p.Longitude,
                    p.TriggerRadius,
                    // Lấy luôn danh sách đa ngôn ngữ đi kèm
                    Translations = p.Translations.Select(t => new
                    {
                        t.LanguageCode,
                        t.Title,
                        t.Description,
                        t.AudioFilePath,
                        t.ImageUrl
                    }).ToList()
                })
                .ToListAsync();

            // Trả về một object bọc ngoài cho chuẩn format API
            return Ok(new
            {
                Version = DateTime.UtcNow.ToString("yyyyMMddHHmmss"), // Tự động tạo version bằng timestamp
                TotalCount = pois.Count,
                Data = pois
            });
        }
        // GET: api/v1/sync/version
        [HttpGet("version")]
        public async Task<IActionResult> CheckVersion()
        {
            // Lấy thời gian tạo/cập nhật mới nhất của quán ăn trong hệ thống
            var latestPoi = await _context.Pois
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestPoi == null) return Ok(new { Version = "1.0.0" }); // DB rỗng

            // Ép ra chuỗi định dạng yyyyMMddHHmmss để làm mã Version
            string currentVersion = latestPoi.CreatedAt.ToString("yyyyMMddHHmmss");

            return Ok(new { Version = currentVersion });
        }
        // GET: api/v1/sync/download-zip
        [HttpGet("download-zip")]
        public IActionResult DownloadOfflinePack()
        {
            using var memoryStream = new MemoryStream();

            // 1. Tạo file ZIP trên RAM
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Giả lập nhét 1 file mp3 tiếng Việt vào thư mục Audio/vi
                var viAudio = archive.CreateEntry("Audio/vi/oc_oanh_vi.mp3");
                using (var entryStream = viAudio.Open())
                using (var streamWriter = new StreamWriter(entryStream))
                {
                    streamWriter.Write("Đây là dữ liệu nhị phân giả lập của file âm thanh tiếng Việt...");
                }

                // Giả lập nhét 1 file mp3 tiếng Anh vào thư mục Audio/en
                var enAudio = archive.CreateEntry("Audio/en/oc_oanh_en.mp3");
                using (var entryStream = enAudio.Open())
                using (var streamWriter = new StreamWriter(entryStream))
                {
                    streamWriter.Write("This is simulated binary data for English audio...");
                }
            }

            memoryStream.Position = 0;

            // 2. Thuật toán băm SHA-256 (Điểm cộng cực mạnh cho đồ án)
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(memoryStream);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            // Reset lại vị trí stream để chuẩn bị tải xuống
            memoryStream.Position = 0;

            // 3. Đính kèm mã SHA-256 vào Header của Response để Mobile App đọc được
            Response.Headers.Append("X-SHA256-Hash", hashString);

            // Trả về file ZIP
            return File(memoryStream.ToArray(), "application/zip", "VinhKhanh_OfflinePack.zip");
        }
    }
}