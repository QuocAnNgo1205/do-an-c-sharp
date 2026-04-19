using VinhKhanhFoodTour.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries; // Thư viện để dùng kiểu Point

namespace VinhKhanhFoodTour.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Seed User Locations for Heatmap
            SeedUserLocations(context);

            // Seed Narration Logs with duration data
            SeedNarrationLogs(context);

            // 1. Kiểm tra nếu đã có dữ liệu POI thì không chạy lại nữa
            if (context.Pois.Any()) return;

            // 2. Tạo Role
            var adminRole = new Role { RoleName = "Admin" };
            var ownerRole = new Role { RoleName = "Owner" };
            var touristRole = new Role { RoleName = "Tourist" };
            context.Roles.AddRange(adminRole, ownerRole, touristRole);
            context.SaveChanges();

            // 3. Tạo User (Hash mật khẩu bằng BCrypt)
            var adminUser = new User 
            { 
                Username = "admin", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), 
                RoleId = adminRole.Id 
            };
            var ownerOanhUser = new User 
            { 
                Username = "owner_oanh", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), 
                RoleId = ownerRole.Id 
            };
            var ownerVuUser = new User 
            { 
                Username = "owner_vu", 
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), 
                RoleId = ownerRole.Id 
            };

            context.Users.AddRange(adminUser, ownerOanhUser, ownerVuUser);
            context.SaveChanges();

            // 4. Tạo POIs (Quán ăn)
            var pois = new Poi[]
            {
                new Poi
                {
                    Name = "Ốc Oanh",
                    Status = PoiStatus.Approved,
                    Location = new Point(106.702081, 10.760193) { SRID = 4326 },
                    TriggerRadius = 20.0,
                    OwnerId = ownerOanhUser.Id,
                    ImageUrl = "https://images.foody.vn/res/g1/476/prof/foody-mobile-oc-oanh-vinh-khanh-avatar-804-63799651234567890.jpg",
                    Latitude = 10.760193,
                    Longitude = 106.702081
                },
                new Poi
                {
                    Name = "Ốc Vũ",
                    Status = PoiStatus.Approved,
                    Location = new Point(106.703123, 10.761456) { SRID = 4326 },
                    TriggerRadius = 20.0,
                    OwnerId = ownerVuUser.Id,
                    ImageUrl = "https://vcdn1-dulich.vnecdn.net/2021/11/24/1-1637745123.jpg",
                    Latitude = 10.761456,
                    Longitude = 106.703123
                }
            };

            context.Pois.AddRange(pois);
            context.SaveChanges();

            // 5. Tạo Translations
            var translations = new PoiTranslation[]
            {
                new PoiTranslation
                {
                    PoiId = pois[0].Id,
                    LanguageCode = "vi",
                    Title = "Ốc Oanh Vinh Khánh",
                    Description = "Nhà hàng ốc nổi tiếng tại quận 4, thành phố Hồ Chí Minh.",
                    ImageUrl = "https://images.foody.vn/res/g1/476/prof/foody-mobile-oc-oanh-vinh-khanh-avatar-804-63799651234567890.jpg"
                },
                new PoiTranslation
                {
                    PoiId = pois[1].Id,
                    LanguageCode = "vi",
                    Title = "Ốc Vũ Vinh Khánh",
                    Description = "Quán ốc uy tín chuyên các loại ốc tươi sống tại đường Vinh Khánh.",
                    ImageUrl = "https://vcdn1-dulich.vnecdn.net/2021/11/24/1-1637745123.jpg"
                }
            };

            context.PoiTranslations.AddRange(translations);
            context.SaveChanges();
        }

        private static void SeedUserLocations(AppDbContext context)
        {
            if (context.UserLocationLogs.Any()) return;

            var random = new Random();
            var logs = new List<UserLocationLog>();
            
            // Lấy danh sách POI để rải người dùng xung quanh quán
            var pois = context.Pois.ToList();
            if (!pois.Any()) return;

            // 1. Rải người dùng CỰC KỲ TẬP TRUNG quanh mỗi quán ăn (Hotspots)
            foreach (var poi in pois)
            {
                int crowdSize = random.Next(15, 30);
                for (int i = 0; i < crowdSize; i++)
                {
                    logs.Add(new UserLocationLog
                    {
                        DeviceId = $"Tourist-{poi.Id}-{i}",
                        // Rải trong bán kính rất hẹp (khoảng 20-30m)
                        Latitude = poi.Latitude + (random.NextDouble() - 0.5) * 0.0005,
                        Longitude = poi.Longitude + (random.NextDouble() - 0.5) * 0.0005,
                        Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(5, 120))
                    });
                }
            }

            // 2. Rải thêm một ít người đi bộ dọc theo trục đường Vĩnh Khánh (giữa các quán)
            for (int j = 0; j < 40; j++)
            {
                // Nội suy ngẫu nhiên giữa Ốc Oanh và Ốc Vũ để tạo cảm giác người đang đi bộ trên phố
                double ratio = random.NextDouble();
                double midLat = pois[0].Latitude + (pois[1].Latitude - pois[0].Latitude) * ratio;
                double midLng = pois[0].Longitude + (pois[1].Longitude - pois[0].Longitude) * ratio;

                logs.Add(new UserLocationLog
                {
                    DeviceId = $"Walker-{j}",
                    Latitude = midLat + (random.NextDouble() - 0.5) * 0.0003,
                    Longitude = midLng + (random.NextDouble() - 0.5) * 0.0003,
                    Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(0, 60))
                });
            }

            context.UserLocationLogs.AddRange(logs);
            context.SaveChanges();
        }
        private static void SeedNarrationLogs(AppDbContext context)
        {
            if (context.NarrationLogs.Any()) return;

            var random = new Random();
            var pois = context.Pois.ToList();
            if (!pois.Any()) return;

            var logs = new List<NarrationLog>();
            foreach (var poi in pois)
            {
                // Create between 10 and 20 logs for each POI
                int count = random.Next(10, 21);
                for (int i = 0; i < count; i++)
                {
                    logs.Add(new NarrationLog
                    {
                        PoiId = poi.Id,
                        DeviceId = $"Device-{random.Next(100, 999)}",
                        ListenDurationSeconds = random.Next(30, 301), // 30s to 5m
                        Timestamp = DateTime.UtcNow.AddDays(-random.Next(0, 30))
                    });
                }
            }

            context.NarrationLogs.AddRange(logs);
            context.SaveChanges();
        }
    }
}