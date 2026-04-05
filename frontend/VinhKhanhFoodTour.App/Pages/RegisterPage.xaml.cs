using VinhKhanhFoodTour.App.PageModels;

namespace VinhKhanhFoodTour.App.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
