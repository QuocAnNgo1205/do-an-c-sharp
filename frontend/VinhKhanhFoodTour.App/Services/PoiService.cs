using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services
{
    public class PoiService : IPoiService
    {
        private readonly HttpClient _httpClient;

        // BaseUrl cho các API chung
        private const string BaseUrl = "http://10.0.2.2:5007/api/v1/poi";

        public PoiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // 1. Hàm lấy tất cả (Hàm cũ của bạn)
        public async Task<List<Poi>> GetPoisAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Poi>>(BaseUrl);
                return response ?? new List<Poi>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching POIs: {ex.Message}");
                return new List<Poi>();
            }
        }

        // 2. 👉 THÊM HÀM NÀY ĐỂ HẾT LỖI VÀ HIỆN CHẤM ĐỎ TRÊN BẢN ĐỒ
        public async Task<List<Poi>> GetPublicPoisAsync()
        {
            try
            {
                // Gọi đến endpoint "public" của Backend (api/v1/poi/public)
                var response = await _httpClient.GetFromJsonAsync<List<Poi>>($"{BaseUrl}/public");
                return response ?? new List<Poi>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching public POIs: {ex.Message}");
                return new List<Poi>();
            }
        }
    }
}