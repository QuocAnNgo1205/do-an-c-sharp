namespace VinhKhanhFoodTour.App.Data
{
    public static class Constants
    {
        // ============================================================
        // 1. CẤU HÌNH API (BASE_URL) - TÍCH HỢP BACKEND
        // ============================================================
        
        // Môi trường chạy (Development, Staging, Production)
        public const string ENVIRONMENT = "Development";

        // Cổng Backend - Thay đổi ở đây nếu Backend chạy trên cổng khác
        private const int BACKEND_PORT = 5007;

        // Base URL được tự động cấu hình dựa trên Platform
        public static string API_BASE_URL => 
            DeviceInfo.Platform == DevicePlatform.Android 
                ? $"http://192.168.2.213:{BACKEND_PORT}/api/v1"  // Đã sửa thành IP LAN thật phục vụ cả máy ảo và điện thoại thật
                : $"http://127.0.0.1:{BACKEND_PORT}/api/v1"; // iOS Simulator / Desktop

        // Timeout mặc định cho HTTP requests (giây)
        public const int HTTP_TIMEOUT_SECONDS = 15;

        // ============================================================
        // 2. CẤU HÌNH CƠSỞ DỮ LIỆU LOCAL (SQLite)
        // ============================================================
        public const string DatabaseFilename = "AppSQLite.db3";

        public static string DatabasePath =>
            $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename)}";

        // ============================================================
        // 3. CẤU HÌNH NGÔN NGỮ (sử dụng cho Audio Guide)
        // ============================================================
        public const string DEFAULT_LANGUAGE_CODE = "vi"; // Tiếng Việt
        public static readonly string[] SUPPORTED_LANGUAGES = { "vi", "en", "ko" };

        // ============================================================
        // 4. CẤU HÌNH AUDIO GUIDE (Smart Fallback)
        // ============================================================
        public const string AUDIO_FALLBACK_MODE = "Smart"; // Smart | AudioOnly | TTSOnly
    }
}