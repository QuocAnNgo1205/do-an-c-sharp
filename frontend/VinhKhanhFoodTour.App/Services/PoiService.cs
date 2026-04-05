using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Data;

namespace VinhKhanhFoodTour.App.Services
{
    /// <summary>
    /// Service chuyên xử lý các API liên quan đến POI
    /// 
    /// Tính năng:
    /// - Lấy danh sách POI công khai
    /// - Lấy chi tiết POI kèm translations
    /// - Xử lý lỗi toàn cầu
    /// </summary>
    public class PoiService : IPoiService
    {
        private readonly HttpClient _httpClient;

        // 🔴 THAY VÀO: Sử dụng Constants thay cho hard-code
        private readonly string _baseUrl = Constants.API_BASE_URL;

        public PoiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ====================================================================
        // ENDPOINT 1: Lấy danh sách POI công khai (Versions cũ - giữ để tương thích)
        // ====================================================================
        /// <summary>
        /// Hàm cũ - giữ để tương thích với code đang dùng
        /// 
        /// Gọi: GET /api/v1/Poi
        /// Trả về: Danh sách Poi cơ bản (không bao gồm Translations)
        /// </summary>
        public async Task<List<Poi>> GetPoisAsync()
        {
            try
            {
                Debug.WriteLine($"[PoiService] GET {_baseUrl}/Poi");
                var response = await _httpClient.GetFromJsonAsync<List<Poi>>($"{_baseUrl}/Poi");
                var result = response ?? new List<Poi>();
                Debug.WriteLine($"[PoiService] ✓ Retrieved {result.Count} POIs");
                return result;
            }
            catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
            {
                Debug.WriteLine($"[PoiService] ⏱️ Timeout: {ex.Message}");
                return new List<Poi>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiService] ❌ Error fetching POIs: {ex.Message}");
                return new List<Poi>();
            }
        }

        // ====================================================================
        // ENDPOINT 2: Lấy danh sách POI công khai
        // ====================================================================
        /// <summary>
        /// Lấy danh sách tất cả POI được duyệt (công khai)
        /// 
        /// Gọi: GET /api/v1/Poi
        /// Trả về: Danh sách Poi với các trường cơ bản
        ///
        /// Lưu ý: Không bao gồm Translations. Dùng GetPoiDetailAsync để lấy chi tiết.
        /// </summary>
        public async Task<List<Poi>> GetPublicPoisAsync()
        {
            try
            {
                Debug.WriteLine($"[PoiService] GET {_baseUrl}/Poi (Public)");
                var response = await _httpClient.GetFromJsonAsync<List<Poi>>($"{_baseUrl}/Poi");
                var result = response ?? new List<Poi>();
                Debug.WriteLine($"[PoiService] ✓ Retrieved {result.Count} public POIs");
                return result;
            }
            catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
            {
                Debug.WriteLine($"[PoiService] ⏱️ Timeout getting public POIs");
                return new List<Poi>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiService] ❌ Error fetching public POIs: {ex.Message}");
                return new List<Poi>();
            }
        }

        // ====================================================================
        // ENDPOINT 3: 🔴 MỚI - Lấy chi tiết POI kèm Translations
        // ====================================================================
        /// <summary>
        /// Lấy chi tiết đầy đủ của một POI, bao gồm danh sách Translations
        /// 
        /// Gọi: GET /api/v1/Poi/{id}
        /// 
        /// Response bao gồm:
        /// - Id, Name, Latitude, Longitude, ImageUrl, Status
        /// - 🔴 QUAN TRỌNG: Danh sách Translations với AudioFilePath
        /// 
        /// Ví dụ Response:
        /// {
        ///   "id": 1,
        ///   "name": "Quán Phở",
        ///   "latitude": 21.028511,
        ///   "longitude": 105.854007,
        ///   "imageUrl": "https://...",
        ///   "status": 1,
        ///   "translations": [
        ///     {
        ///       "id": 10,
        ///       "languageCode": "vi",
        ///       "title": "Phở Gia Truyền",
        ///       "description": "Quán phở nổi tiếng với phở bò...",
        ///       "audioFilePath": "/media/vi/poi_1.mp3",
        ///       "imageUrl": null
        ///     },
        ///     {
        ///       "id": 11,
        ///       "languageCode": "en",
        ///       "title": "Traditional Pho",
        ///       "description": "Famous pho restaurant...",
        ///       "audioFilePath": null,
        ///       "imageUrl": null
        ///     }
        ///   ]
        /// }
        /// 
        /// Cách dùng:
        /// var detail = await poiService.GetPoiDetailAsync(1);
        /// if (detail != null)
        /// {
        ///   var viTranslation = detail.GetTranslation("vi");
        ///   await audioGuideService.PlayAudioAsync(viTranslation);
        /// }
        /// </summary>
        public async Task<Poi?> GetPoiDetailAsync(int poiId)
        {
            try
            {
                Debug.WriteLine($"[PoiService] GET {_baseUrl}/Poi/{poiId}");

                var response = await _httpClient.GetFromJsonAsync<Poi>($"{_baseUrl}/Poi/{poiId}");
                
                if (response != null)
                {
                    Debug.WriteLine($"[PoiService] ✓ Retrieved POI detail with {response.Translations?.Count ?? 0} translations");
                }
                
                return response;
            }
            catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
            {
                Debug.WriteLine($"[PoiService] ⏱️ Timeout getting POI detail {poiId}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PoiService] ❌ Error fetching POI detail {poiId}: {ex.Message}");
                return null;
            }
        }

        // ====================================================================
        // TIỆN ÍCH: Lấy bản dịch từ POI theo ngôn ngữ
        // ====================================================================
        /// <summary>
        /// Lấy bản dịch của POI theo mã ngôn ngữ
        /// </summary>
        public PoiTranslation? GetTranslation(Poi poi, string languageCode)
        {
            return poi?.GetTranslation(languageCode);
        }

        /// <summary>
        /// Kiểm tra xem POI có audio cho ngôn ngữ yêu cầu không
        /// </summary>
        public bool HasAudio(Poi poi, string languageCode)
        {
            return poi?.HasAudioForLanguage(languageCode) ?? false;
        }
    }
}