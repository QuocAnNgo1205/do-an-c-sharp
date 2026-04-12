using VinhKhanhFoodTour.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries; // Thư viện để dùng kiểu Point

namespace VinhKhanhFoodTour.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // 1. Kiểm tra nếu đã có dữ liệu POI thì không chạy lại nữa
            if (context.Pois.Any()) return;

            // 2. Tạo Role
            var adminRole = new Role { RoleName = "Admin" };
            var ownerRole = new Role { RoleName = "Owner" };
            var touristRole = new Role { RoleName = "Tourist" };
            context.Roles.AddRange(adminRole, ownerRole, touristRole);
            context.SaveChanges();

            // 3. Tạo User (Hash mật khẩu bằng BCrypt để khớp với logic Auth mới)
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
                    // Tọa độ chuẩn cho SQL Server xử lý bản đồ
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

            // 5. Tạo Translations (Thuyết minh đa ngôn ngữ)
            var translations = new PoiTranslation[]
            {
                new PoiTranslation
                {
                    PoiId = pois[0].Id, // Ốc Oanh
                    LanguageCode = "vi",
                    Title = "Ốc Oanh Vinh Khánh",
                    Description = "Nhà hàng ốc nổi tiếng tại quận 4, thành phố Hồ Chí Minh. Phục vụ các món ốc tươi sống với hương vị đặc sắc.",
                    ImageUrl = "https://images.foody.vn/res/g1/476/prof/foody-mobile-oc-oanh-vinh-khanh-avatar-804-63799651234567890.jpg"
                },
                new PoiTranslation
                {
                    PoiId = pois[1].Id, // Ốc Vũ
                    LanguageCode = "vi",
                    Title = "Ốc Vũ Vinh Khánh",
                    Description = "Quán ốc uy tín chuyên các loại ốc tươi sống, không gian thoáng mát tại đường Vinh Khánh.",
                    ImageUrl = "https://vcdn1-dulich.vnecdn.net/2021/11/24/1-1637745123.jpg"
                }
            };

            context.PoiTranslations.AddRange(translations);
            context.SaveChanges();
        }
    }
}