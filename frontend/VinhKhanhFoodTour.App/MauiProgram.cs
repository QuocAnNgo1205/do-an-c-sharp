using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using VinhKhanhFoodTour.App.Services;
using VinhKhanhFoodTour.App.Pages;
using VinhKhanhFoodTour.App.PageModels;
using Microsoft.Maui.Controls.Hosting;
using Plugin.Maui.Audio;

# if ANDROID
using VinhKhanhFoodTour.App.Platforms.Android;
# elif IOS
using VinhKhanhFoodTour.App.Platforms.iOS;
# endif

namespace VinhKhanhFoodTour.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()  // Includes MediaElement support
            .UseMauiMaps()
            .ConfigureSyncfusionToolkit()
            .ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
                {
                    handler.PlatformView.SingleSelectionFollowsFocus = false;
                });
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
            });

        // 1. ĐĂNG KÝ SERVICES
        builder.Services.AddHttpClient<IPoiService, PoiService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<AuthService>(); // 🔐 MỚI: Dịch vụ xác thực
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<AudioGuideService>();  // 🔴 MỚI: Thuyết minh thông minh
        builder.Services.AddSingleton<SeedDataService>();
        builder.Services.AddSingleton<ModalErrorHandler>();
        builder.Services.AddSingleton<ProjectRepository>();

        // 🛰️ MỚI: GPS Tracking & Geofencing
#if ANDROID
        builder.Services.AddSingleton<ILocationTrackingService, AndroidLocationTrackingService>();
#elif IOS
        builder.Services.AddSingleton<ILocationTrackingService, iOSLocationTrackingService>();
#else
        // Mock cho các nền tảng khác
        builder.Services.AddSingleton<ILocationTrackingService, MockLocationService>();
#endif
        builder.Services.AddSingleton<GeofenceManager>();

        // 2. ĐĂNG KÝ PAGES VÀ VIEWMODELS
        // Gợi ý: Đăng ký ViewModel trước Page để hệ thống DI hoạt động mượt hơn
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<MainPage>();

        builder.Services.AddTransient<MapPage>();
        builder.Services.AddSingleton<ProfilePageModel>();
        builder.Services.AddSingleton<ProfilePage>();

        // 🔐 MỚI: Auth Pages
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();

        // Đăng ký AppShell để quản lý các Tab
        builder.Services.AddSingleton<AppShell>();

        // 3. ĐĂNG KÝ ROUTES (Trang chi tiết)
        builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}