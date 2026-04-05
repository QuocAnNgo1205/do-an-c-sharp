using System.Collections.Generic;
using System.Threading.Tasks;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services
{
    /// <summary>
    /// Interface cho tất cả các API liên quan đến POI
    /// 
    /// Endpoint được hỗ trợ:
    /// - GET /api/v1/Poi - Lấy danh sách POI công khai
    /// - GET /api/v1/Poi/{id} - Lấy chi tiết POI kèm Translations
    /// </summary>
    public interface IPoiService
    {
        // ====================================================================
        // PHƯƠNG THỨC CŨ - Giữ để tương thích với code đang dùng
        // ====================================================================
        /// <summary>
        /// Lấy danh sách tất cả POI
        /// </summary>
        Task<List<Poi>> GetPoisAsync();

        /// <summary>
        /// Lấy danh sách POI công khai
        /// </summary>
        Task<List<Poi>> GetPublicPoisAsync();

        // ====================================================================
        // 🔴 PHƯƠNG THỨC MỚI - Cho tính năng Thuyết minh thông minh
        // ====================================================================
        /// <summary>
        /// Lấy chi tiết đầy đủ của một POI, bao gồm danh sách Translations
        /// 
        /// Response bao gồm:
        /// - Id, Name, Latitude, Longitude, ImageUrl, Status
        /// - Danh sách Translations với audioFilePath và description
        /// 
        /// Ghi chú: Hãy dùng phương thức này khi bạn cần phát audio
        /// hoặc hiển thị thuyết minh chi tiết của quán ăn.
        /// </summary>
        Task<Poi?> GetPoiDetailAsync(int poiId);

        /// <summary>
        /// Lấy bản dịch của POI theo mã ngôn ngữ
        /// </summary>
        PoiTranslation? GetTranslation(Poi poi, string languageCode);

        /// <summary>
        /// Kiểm tra xem POI có audio cho ngôn ngữ yêu cầu không
        /// </summary>
        bool HasAudio(Poi poi, string languageCode);
    }
}