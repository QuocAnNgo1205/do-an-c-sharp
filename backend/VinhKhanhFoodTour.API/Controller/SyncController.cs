using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SyncController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // API: Check Latest Version of Approved POIs
        [HttpGet("public/sync/version")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLatestVersion()
        {
            try
            {
                // Query the Pois table for all Approved POIs
                var approvedPois = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .ToListAsync();

                string versionString;

                if (approvedPois.Count == 0)
                {
                    // If there are no approved POIs, return default version
                    versionString = "1.0.0";
                }
                else
                {
                    // Find the maximum LastUpdated DateTime
                    var maxLastUpdated = approvedPois.Max(p => p.LastUpdated);
                    // Format as "yyyyMMddHHmmss"
                    versionString = maxLastUpdated.ToString("yyyyMMddHHmmss");
                }

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
                // Asynchronously fetch all Approved POIs including their Translations
                var approvedPois = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .Include(p => p.Translations)
                    .ToListAsync();

                // Map to anonymous objects to avoid object cycles
                var poisData = approvedPois.Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    TriggerRadius = p.TriggerRadius,
                    Translations = p.Translations.Select(t => new
                    {
                        Id = t.Id,
                        LanguageCode = t.LanguageCode,
                        Title = t.Title,
                        Description = t.Description,
                        ImageUrl = t.ImageUrl,
                        AudioFilePath = t.AudioFilePath
                    }).ToList()
                }).ToList();

                // Serialize to JSON with formatting
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var poisJson = JsonSerializer.Serialize(poisData, jsonOptions);

                // Create in-memory ZIP archive
                using (var memoryStream = new MemoryStream())
                {
                    using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        // Add pois.json to the ZIP
                        var poisJsonEntry = zipArchive.CreateEntry("pois.json");
                        using (var writer = new StreamWriter(poisJsonEntry.Open()))
                        {
                            await writer.WriteAsync(poisJson);
                        }

                        // Iterate through translations and add media files
                        foreach (var poi in approvedPois)
                        {
                            foreach (var translation in poi.Translations)
                            {
                                // Handle ImageUrl
                                if (!string.IsNullOrEmpty(translation.ImageUrl))
                                {
                                    await AddMediaFileToZip(zipArchive, translation.ImageUrl);
                                }

                                // Handle AudioFilePath
                                if (!string.IsNullOrEmpty(translation.AudioFilePath))
                                {
                                    await AddMediaFileToZip(zipArchive, translation.AudioFilePath);
                                }
                            }
                        }
                    }

                    // Convert stream to byte array
                    var zipBytes = memoryStream.ToArray();

                    // Calculate SHA-256 hash
                    using (var sha256 = SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(zipBytes);
                        var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                        // Add hash to response headers
                        Response.Headers.Append("X-SHA256-Hash", hashHex);
                    }

                    // Return ZIP file
                    return File(zipBytes, "application/zip", "VinhKhanh_OfflinePack.zip");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tạo gói offline: " + ex.Message });
            }
        }

        // Helper: Add media file to ZIP archive
        private async Task AddMediaFileToZip(ZipArchive zipArchive, string relativePath)
        {
            try
            {
                // Construct physical file path from relative path
                if (string.IsNullOrEmpty(relativePath))
                    return;

                // Remove leading slash if present
                var cleanPath = relativePath.TrimStart('/');
                var physicalPath = Path.Combine(_env.WebRootPath, cleanPath);

                // Verify file exists
                if (!System.IO.File.Exists(physicalPath))
                {
                    // Log and ignore if file is missing
                    return;
                }

                // Add file to ZIP using relative path as entry name
                var zipEntryName = cleanPath.Replace("\\", "/");
                var zipEntry = zipArchive.CreateEntry(zipEntryName);

                using (var fileStream = System.IO.File.OpenRead(physicalPath))
                {
                    using (var zipStream = zipEntry.Open())
                    {
                        await fileStream.CopyToAsync(zipStream);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire ZIP creation
                // In production, use ILogger for proper logging
                System.Diagnostics.Debug.WriteLine($"Lỗi khi thêm file media vào ZIP: {relativePath} - {ex.Message}");
            }
        }
    }
}