using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Services
{
    public class SyncService : ISyncService
    {
        private static readonly SemaphoreSlim _packLock = new(1, 1);
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SyncService> _logger;
        private readonly string _offlinePacksDirectory;
        private const string PACK_FILENAME = "VinhKhanh_OfflinePack.zip";
        private const string HASH_FILENAME = "VinhKhanh_OfflinePack.sha256.txt";
        private DateTime? _lastSuccessAt;
        private long? _lastDurationMs;
        private string? _lastError;
        private DateTime? _lastErrorAt;
        private bool _isRunning;

        public SyncService(AppDbContext context, IWebHostEnvironment env, ILogger<SyncService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _offlinePacksDirectory = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "offline-packs");
        }

        public async Task GenerateOfflinePackAsync()
        {
            await _packLock.WaitAsync();
            var stopwatch = Stopwatch.StartNew();
            _isRunning = true;
            try
            {
                if (!Directory.Exists(_offlinePacksDirectory))
                {
                    Directory.CreateDirectory(_offlinePacksDirectory);
                }

                var approvedPois = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .Include(p => p.Translations)
                    .ToListAsync();

                var poisData = approvedPois.Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Latitude = p.Location.Y,
                    Longitude = p.Location.X,
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

                var poisJson = JsonSerializer.Serialize(poisData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var packPath = Path.Combine(_offlinePacksDirectory, PACK_FILENAME);
                var hashPath = Path.Combine(_offlinePacksDirectory, HASH_FILENAME);
                var tempPackPath = $"{packPath}.{Guid.NewGuid():N}.tmp";
                var tempHashPath = $"{hashPath}.{Guid.NewGuid():N}.tmp";

                using (var fileStream = new FileStream(tempPackPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                {
                    var poisJsonEntry = zipArchive.CreateEntry("pois.json");
                    using (var writer = new StreamWriter(poisJsonEntry.Open()))
                    {
                        await writer.WriteAsync(poisJson);
                    }

                    foreach (var poi in approvedPois)
                    {
                        foreach (var translation in poi.Translations)
                        {
                            if (!string.IsNullOrEmpty(translation.ImageUrl))
                            {
                                await AddMediaFileToZip(zipArchive, translation.ImageUrl);
                            }

                            if (!string.IsNullOrEmpty(translation.AudioFilePath))
                            {
                                await AddMediaFileToZip(zipArchive, translation.AudioFilePath);
                            }
                        }
                    }
                }

                byte[] hashBytes;
                using (var sha256 = SHA256.Create())
                using (var fileStream = File.OpenRead(tempPackPath))
                {
                    hashBytes = sha256.ComputeHash(fileStream);
                }

                var hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                await File.WriteAllTextAsync(tempHashPath, hashHex);

                File.Move(tempPackPath, packPath, true);
                File.Move(tempHashPath, hashPath, true);

                stopwatch.Stop();
                _lastSuccessAt = DateTime.UtcNow;
                _lastDurationMs = stopwatch.ElapsedMilliseconds;
                _lastError = null;
                _lastErrorAt = null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _lastError = ex.Message;
                _lastErrorAt = DateTime.UtcNow;
                _logger.LogError(ex, "Failed to generate offline pack.");
                throw;
            }
            finally
            {
                _isRunning = false;
                _packLock.Release();
            }
        }

        public async Task<OfflinePackManifestDto?> GetManifestAsync()
        {
            await _packLock.WaitAsync();
            try
            {
                var packPath = Path.Combine(_offlinePacksDirectory, PACK_FILENAME);
                var hashPath = Path.Combine(_offlinePacksDirectory, HASH_FILENAME);

                if (!File.Exists(packPath) || !File.Exists(hashPath))
                {
                    return null;
                }

                var hash = (await File.ReadAllTextAsync(hashPath)).Trim();
                var info = new FileInfo(packPath);
                return new OfflinePackManifestDto
                {
                    Version = hash,
                    FileName = PACK_FILENAME,
                    FileSizeBytes = info.Length,
                    SHA256Hash = hash,
                    GeneratedAt = info.LastWriteTimeUtc
                };
            }
            finally
            {
                _packLock.Release();
            }
        }

        public Task<SyncStatusDto> GetStatusAsync()
        {
            return Task.FromResult(new SyncStatusDto
            {
                HasSuccess = _lastSuccessAt.HasValue,
                LastSuccessAt = _lastSuccessAt,
                LastDurationMs = _lastDurationMs,
                LastError = _lastError,
                LastErrorAt = _lastErrorAt,
                IsRunning = _isRunning
            });
        }

        public string GetOfflinePackPath()
        {
            return Path.Combine(_offlinePacksDirectory, PACK_FILENAME);
        }

        private async Task AddMediaFileToZip(ZipArchive zipArchive, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return;
            }

            var cleanPath = relativePath.TrimStart('/');
            var physicalPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), cleanPath);
            if (!File.Exists(physicalPath))
            {
                _logger.LogWarning("Media file missing while generating pack: {Path}", relativePath);
                return;
            }

            var zipEntryName = cleanPath.Replace("\\", "/");
            var zipEntry = zipArchive.CreateEntry(zipEntryName);

            using var fileStream = File.OpenRead(physicalPath);
            using var zipStream = zipEntry.Open();
            await fileStream.CopyToAsync(zipStream);
        }
    }
}
