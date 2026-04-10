using VinhKhanhFoodTour.App.PageModels;

namespace VinhKhanhFoodTour.App.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfilePageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is ProfilePageModel model)
        {
            model.LoadProfileDataCommand.Execute(null);
        }
    }
}