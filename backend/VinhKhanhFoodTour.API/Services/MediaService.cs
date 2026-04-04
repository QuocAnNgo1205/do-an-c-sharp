namespace VinhKhanhFoodTour.API.Services
{
    public class MediaService : IMediaService
    {
        private readonly IWebHostEnvironment _env;

        public MediaService(IWebHostEnvironment env)
        {
            _env = env;
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
                var imageDirectory = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "images");
                Directory.CreateDirectory(imageDirectory);
                var imagePhysicalPath = Path.Combine(imageDirectory, uniqueImageFileName);

                using (var fileStream = new FileStream(imagePhysicalPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                imageUrl = $"/uploads/images/{uniqueImageFileName}";
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
                var audioDirectory = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "audio");
                Directory.CreateDirectory(audioDirectory);
                var audioPhysicalPath = Path.Combine(audioDirectory, uniqueAudioFileName);

                using (var fileStream = new FileStream(audioPhysicalPath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(fileStream);
                }

                audioFilePath = $"/uploads/audio/{uniqueAudioFileName}";
            }

            return (imageUrl, audioFilePath);
        }
    }
}
