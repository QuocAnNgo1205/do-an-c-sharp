using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace VinhKhanhFoodTour.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    // Đã thêm chữ "static" để các màn hình khác có thể gọi được
    public static async Task DisplayToastAsync(string message)
    {
        var toast = Toast.Make(message, ToastDuration.Short, 14);
        await toast.Show();
    }
}