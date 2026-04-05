using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using VinhKhanhFoodTour.App.Services;
using VinhKhanhFoodTour.App.Pages;
using VinhKhanhFoodTour.App.PageModels;
using Microsoft.Maui.Controls.Hosting;

namespace VinhKhanhFoodTour.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
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
        builder.Services.AddSingleton<SeedDataService>();
        builder.Services.AddSingleton<ModalErrorHandler>();
        builder.Services.AddSingleton<ProjectRepository>();

        // 2. ĐĂNG KÝ PAGES VÀ VIEWMODELS
        // Gợi ý: Đăng ký ViewModel trước Page để hệ thống DI hoạt động mượt hơn
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<MainPage>();

        // SỬA TẠI ĐÂY: Chuyển từ AddSingleton sang AddTransient để MapPage luôn làm mới dữ liệu và hiện ghim mới
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddSingleton<ProfilePage>();

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