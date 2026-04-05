using VinhKhanhFoodTour.App.PageModels;

namespace VinhKhanhFoodTour.App.Pages;

public partial class MainPage : ContentPage
{
    // Sử dụng Dependency Injection để tiêm ViewModel vào
    public MainPage(MainPageModel viewModel)
    {
        InitializeComponent();

        // Gán BindingContext để dữ liệu từ API hiện lên màn hình
        BindingContext = viewModel;
    }
}