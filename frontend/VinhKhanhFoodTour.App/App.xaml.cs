namespace VinhKhanhFoodTour.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Giao quyền điều hướng lại cho AppShell (Nơi chúng ta đã đặt MainPage làm trang chủ)
            MainPage = new AppShell();
        }
    }
}