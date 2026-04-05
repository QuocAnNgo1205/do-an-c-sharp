namespace VinhKhanhFoodTour.API.Services
{
    public class MediaService : IMediaService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MediaService>? _logger;

        public MediaService(IWebHostEnvironment env, ILogger<MediaService>? logger = null)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<(string? imageUrl, string? audioFilePath)> SaveMediaAsync(IFormFile? imageFile, IFormFile? audioFile)
        {
            string? imageUrl = null;
            string? audioFilePath = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var imageExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    throw new InvalidOperationException("Định dạng ảnh không hợp lệ. Chỉ cho phép: .jpg, .jpeg, .png");
                }

                var uniqueImageFileName = $"{Guid.NewGuid()}{imageExtension}";
                var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var imageDirectory = Path.Combine(webRootPath, "uploads", "images");
                
                // 🔴 MỚI: Đảm bảo thư mục tồn tại
                Directory.CreateDirectory(imageDirectory);
                
                var imagePhysicalPath = Path.Combine(imageDirectory, uniqueImageFileName);

                try
                {
                    using (var fileStream = new FileStream(imagePhysicalPath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    imageUrl = $"/uploads/images/{uniqueImageFileName}";
                    _logger?.LogInformation($"✓ Ảnh lưu thành công: {imageUrl}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"❌ Lỗi lưu ảnh: {ex.Message}");
                    throw new InvalidOperationException($"Lỗi lưu file ảnh: {ex.Message}", ex);
                }
            }

            if (audioFile != null && audioFile.Length > 0)
            {
                var allowedAudioExtensions = new[] { ".mp3", ".wav" };
                var audioExtension = Path.GetExtension(audioFile.FileName).ToLowerInvariant();
                if (!allowedAudioExtensions.Contains(audioExtension))
                {
                    throw new InvalidOperationException("Định dạng âm thanh không hợp lệ. Chỉ cho phép: .mp3, .wav");
                }

                var uniqueAudioFileName = $"{Guid.NewGuid()}{audioExtension}";
                var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var audioDirectory = Path.Combine(webRootPath, "uploads", "audio");
                
                // 🔴 MỚI: Đảm bảo thư mục tồn tại
                Directory.CreateDirectory(audioDirectory);
                
                var audioPhysicalPath = Path.Combine(audioDirectory, uniqueAudioFileName);

                try
                {
                    using (var fileStream = new FileStream(audioPhysicalPath, FileMode.Create))
                    {
                        await audioFile.CopyToAsync(fileStream);
                    }
                    audioFilePath = $"/uploads/audio/{uniqueAudioFileName}";
                    _logger?.LogInformation($"✓ Âm thanh lưu thành công: {audioFilePath}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"❌ Lỗi lưu âm thanh: {ex.Message}");
                    throw new InvalidOperationException($"Lỗi lưu file âm thanh: {ex.Message}", ex);
                }
            }

            return (imageUrl, audioFilePath);
        }
    }
}
