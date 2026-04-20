using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.API.DTOs;
using VinhKhanhFoodTour.Data;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class MediaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MediaController> _logger;

        public MediaController(AppDbContext context, IWebHostEnvironment env, ILogger<MediaController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [HttpGet("audio")]
        public async Task<ActionResult<IEnumerable<AdminAudioDto>>> GetAudioFiles()
        {
            try
            {
                var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var audioDirectory = Path.Combine(webRootPath, "uploads", "audio");

                if (!Directory.Exists(audioDirectory))
                {
                    return Ok(new List<AdminAudioDto>());
                }

                // 1. Get all files in directory
                var directoryInfo = new DirectoryInfo(audioDirectory);
                var files = directoryInfo.GetFiles();

                // 2. Get all usages in database
                var usages = await _context.PoiTranslations
                    .Include(t => t.Poi)
                    .Where(t => !string.IsNullOrEmpty(t.AudioFilePath))
                    .ToListAsync();

                // 3. Match them up
                var result = files.Select(file =>
                {
                    // AudioFilePath in DB is like "/uploads/audio/uuid.mp3"
                    var relativePath = $"/uploads/audio/{file.Name}";
                    var usage = usages.FirstOrDefault(u => u.AudioFilePath == relativePath);

                    return new AdminAudioDto
                    {
                        FileName = file.Name,
                        FilePath = relativePath,
                        FileSize = file.Length,
                        LastModified = file.LastWriteTime,
                        PoiId = usage?.PoiId,
                        PoiName = usage?.Poi?.Name,
                        LanguageCode = usage?.LanguageCode
                    };
                }).OrderByDescending(x => x.LastModified).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error listing audio files: {ex.Message}");
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách âm thanh." });
            }
        }

        [HttpDelete("audio/{fileName}")]
        public async Task<IActionResult> DeleteAudioFile(string fileName)
        {
            try
            {
                var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(webRootPath, "uploads", "audio", fileName);
                var relativePath = $"/uploads/audio/{fileName}";

                // 1. Delete from physical storage
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // 2. Clear references in database
                var usages = await _context.PoiTranslations
                    .Where(t => t.AudioFilePath == relativePath)
                    .ToListAsync();

                foreach (var usage in usages)
                {
                    usage.AudioFilePath = null;
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Đã xóa file âm thanh thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting audio file: {ex.Message}");
                return StatusCode(500, new { Message = "Lỗi khi xóa file âm thanh." });
            }
        }
    }
}
