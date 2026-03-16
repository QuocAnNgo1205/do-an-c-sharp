using Android.App;
using Android.Content.PM;
using Microsoft.Maui;

namespace VinhKhanhFoodTour;

// Dòng [Activity] này rất quan trọng, MainLauncher = true báo cho Android biết đây là màn hình đầu tiên cần mở
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}