using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Check if data already exists
            if (context.Roles.Any())
            {
                return;
            }

            // Seed Roles
            var roles = new Role[]
            {
                new Role { RoleName = "Admin" },
                new Role { RoleName = "Owner" },
                new Role { RoleName = "Tourist" }
            };
            foreach (var role in roles)
            {
                context.Roles.Add(role);
            }
            context.SaveChanges();

            // Seed Users
            var adminRole = context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
            var ownerRole = context.Roles.FirstOrDefault(r => r.RoleName == "Owner");

            var users = new User[]
            {
                new User { Username = "admin_user", PasswordHash = "dummy_hash_for_demo", RoleId = adminRole.Id },
                new User { Username = "owner_oanh", PasswordHash = "dummy_hash_for_demo", RoleId = ownerRole.Id },
                new User { Username = "owner_vu", PasswordHash = "dummy_hash_for_demo", RoleId = ownerRole.Id }
            };
            foreach (var user in users)
            {
                context.Users.Add(user);
            }
            context.SaveChanges();

            // Seed POIs
            var ownerOanh = context.Users.FirstOrDefault(u => u.Username == "owner_oanh");
            var ownerVu = context.Users.FirstOrDefault(u => u.Username == "owner_vu");

            var pois = new Poi[]
            {
                new Poi
                {
                    Name = "Ốc Oanh",
                    Status = PoiStatus.Approved,
                    Location = new NetTopologySuite.Geometries.Point(106.123456, 10.123456) { SRID = 4326 },
                    TriggerRadius = 20.0,
                    OwnerId = ownerOanh.Id
                },
                new Poi
                {
                    Name = "Ốc Vũ",
                    Status = PoiStatus.Approved,
                    Location = new NetTopologySuite.Geometries.Point(106.125678, 10.125789) { SRID = 4326 },
                    TriggerRadius = 20.0,
                    OwnerId = ownerVu.Id
                }
            };
            foreach (var poi in pois)
            {
                context.Pois.Add(poi);
            }
            context.SaveChanges();

            // Seed PoiTranslations
            var poiOanh = context.Pois.FirstOrDefault(p => p.Name == "Ốc Oanh");
            var poiVu = context.Pois.FirstOrDefault(p => p.Name == "Ốc Vũ");

            var translations = new PoiTranslation[]
            {
                // Ốc Oanh - Vietnamese
                new PoiTranslation
                {
                    PoiId = poiOanh.Id,
                    LanguageCode = "vi",
                    Title = "Ốc Oanh",
                    Description = "Nhà hàng ốc nổi tiếng tại quận 4, thành phố Hồ Chí Minh. Phục vụ các món ốc tươi sống với hương vị đặc sắc của vùng sông nước.",
                    ImageUrl = "https://example.com/oc-oanh-vi.jpg",
                    AudioFilePath = "/audio/oc-oanh-vi.mp3"
                },
                // Ốc Oanh - English
                new PoiTranslation
                {
                    PoiId = poiOanh.Id,
                    LanguageCode = "en",
                    Title = "Ốc Oanh Restaurant",
                    Description = "A renowned snail restaurant in District 4, Ho Chi Minh City. Serving fresh live snails with distinctive flavors of the Mekong Delta region.",
                    ImageUrl = "https://example.com/oc-oanh-en.jpg",
                    AudioFilePath = "/audio/oc-oanh-en.mp3"
                },
                // Ốc Vũ - Vietnamese
                new PoiTranslation
                {
                    PoiId = poiVu.Id,
                    LanguageCode = "vi",
                    Title = "Ốc Vũ",
                    Description = "Quán ốc uy tín tại đường Vinh Khánh, quận 4. Chuyên cung cấp các loại ốc nước ngọt tươi sống với nhiều cách chế biến hấp dẫn.",
                    ImageUrl = "https://example.com/oc-vu-vi.jpg",
                    AudioFilePath = "/audio/oc-vu-vi.mp3"
                },
                // Ốc Vũ - English
                new PoiTranslation
                {
                    PoiId = poiVu.Id,
                    LanguageCode = "en",
                    Title = "Ốc Vũ Restaurant",
                    Description = "A trusted snail restaurant on Vinh Khanh Street, District 4. Specializing in fresh freshwater snails prepared in various delicious ways.",
                    ImageUrl = "https://example.com/oc-vu-en.jpg",
                    AudioFilePath = "/audio/oc-vu-en.mp3"
                }
            };
            foreach (var translation in translations)
            {
                context.PoiTranslations.Add(translation);
            }
            context.SaveChanges();
        }
    }
}
