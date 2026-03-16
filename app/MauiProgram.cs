using VinhKhanhFoodTour;
using VinhKhanhFoodTour.Interfaces;
using VinhKhanhFoodTour.Services;
using VinhKhanhFoodTour.ViewModels;
using Microsoft.Maui.Controls.Hosting;

namespace VinhKhanhFoodTour;

/// <summary>
/// MAUI application configuration and dependency injection setup.
/// Configures services, viewmodels, and pages for the application.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application with dependency injection.
    /// 
    /// Service Registration:
    /// 1. ILocationService -> LocationService (GPS tracking)
    /// 2. INarrationService -> NarrationService (Distance calc, TTS, cooldown)
    /// 3. LocationTrackingViewModel (Main ViewModel)
    /// 4. MainPage (UI)
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            // Configure MAUI application base settings
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            // Register application services
            .Services
                // Register location service for GPS tracking
                .AddSingleton<ILocationService, LocationService>()
                // Register narration service for distance calc and TTS
                .AddSingleton<INarrationService, NarrationService>()
                // Register ViewModel for dependency injection into Pages
                .AddSingleton<LocationTrackingViewModel>()
                // Register MainPage
                .AddSingleton<MainPage>();

        return builder.Build();
    }
}
