using System.Collections.Generic;
using System.Threading.Tasks;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services
{
    public interface IPoiService
    {
        // Giữ lại hàm cũ nếu bạn đang dùng ở chỗ khác
        Task<List<Poi>> GetPoisAsync();

        // 👉 THÊM DÒNG NÀY VÀO ĐỂ HẾT LỖI GẠCH ĐỎ Ở MAPPAGE
        Task<List<Poi>> GetPublicPoisAsync();
    }
}