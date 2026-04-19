using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data;
using System;
using System.Linq;

namespace CheckSeeds
{
    class Program
    {
        static void Main(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            // Update connection string if needed
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=VinhKhanhFoodTour;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            using (var context = new AppDbContext(optionsBuilder.Options))
            {
                var poiCount = context.Pois.Count();
                var logCount = context.UserLocationLogs.Count();
                Console.WriteLine($"POI Count: {poiCount}");
                Console.WriteLine($"UserLocationLog Count: {logCount}");
                
                if (logCount > 0) {
                    var latest = context.UserLocationLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault();
                    Console.WriteLine($"Latest Log Timestamp: {latest?.Timestamp}");
                }
            }
        }
    }
}
