using VinhKhanhFoodTour.App.PageModels;

namespace VinhKhanhFoodTour.App.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
