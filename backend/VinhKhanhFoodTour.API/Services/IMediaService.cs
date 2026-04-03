namespace VinhKhanhFoodTour.API.Services
{
    public interface IMediaService
    {
        Task<(string? imageUrl, string? audioFilePath)> SaveMediaAsync(IFormFile? imageFile, IFormFile? audioFile);
    }
}
