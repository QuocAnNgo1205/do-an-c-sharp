using VinhKhanhFoodTour.AdminPortal.Components;
using VinhKhanhFoodTour.AdminPortal.Services.Auth;
using VinhKhanhFoodTour.AdminPortal.Services.Http;
using VinhKhanhFoodTour.AdminPortal.Services.Owner;
using VinhKhanhFoodTour.AdminPortal.Services.Admin;
using VinhKhanhFoodTour.AdminPortal.Services.Common;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

// 2. Add 3rd party libraries
builder.Services.AddBlazoredLocalStorage();
// Cấu hình CORS - Chỉ cho phép các Web được chỉ định gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("SecurityCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "https://localhost:5001", // Thay bằng Port lúc chạy chạy Blazor Web của bạn
                "http://localhost:5000",
                "https://ten-mien-khi-deploy-cua-ban.com" // Sau này public lên mạng thì điền domain vào đây
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Rất quan trọng nếu hệ thống dùng Token/Cookie
    });
});
// 3. Configure HTTP Client (Chuẩn Production)
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // Lấy URL từ appsettings.json hoặc set cứng khi dev
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/");
});

// 4. Configure Application State
// AuthState là custom state manager (KHÔNG phải ASP.NET Core AuthenticationStateProvider)
// Dùng AddScoped để mỗi Blazor circuit có một instance riêng
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<AuthService>();

// 5. Configure Business Services
builder.Services.AddScoped<IPoiService, PoiService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();

// 6. Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseStaticFiles(); // Đổi lại dùng UseStaticFiles nếu bị lỗi ở MapStaticAssets
app.UseAntiforgery();
app.UseCors("SecurityCorsPolicy");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();