using VinhKhanhFoodTour.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddControllers();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    // Ensure database is created
    context.Database.EnsureCreated();

    // Call the initializer
    DbInitializer.Initialize(context);
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
// Bỏ dòng HttpsRedirection đi
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Thêm cái API test nhanh này vào
app.MapGet("/test", () => "Tuyet voi! Server Vinh Khanh Food Tour dang chay ngon lanh!");

app.Run();