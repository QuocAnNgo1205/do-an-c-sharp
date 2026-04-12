namespace VinhKhanhFoodTour.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        /// <summary>
        /// MAUI gọi method này khi app được mở / resume bằng một URI (deep link).
        /// Xử lý được cả 2 trường hợp:
        ///   - Cold Start: app chưa chạy, user mở từ QR Code
        ///   - Background Resume: app đang chạy ngầm, được đưa lên foreground
        /// </summary>
        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            System.Diagnostics.Debug.WriteLine($"[App] OnAppLinkRequestReceived: {uri}");

            // Lấy AppShell hiện tại và delegate xử lý
            if (MainPage is AppShell shell)
            {
                await shell.HandleDeepLinkAsync(uri);
            }
            else
            {
                // Edge case: Shell chưa sẵn sàng (init race) — delay nhỏ rồi thử lại
                await Task.Delay(500);
                if (MainPage is AppShell retryShell)
                {
                    await retryShell.HandleDeepLinkAsync(uri);
                }
            }
        }
    }
}