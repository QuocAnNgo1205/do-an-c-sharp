using VinhKhanhFoodTour.App.PageModels;

namespace VinhKhanhFoodTour.App.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfilePageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Dừng âm thanh khi rời khỏi tab Hồ sơ
        if (BindingContext is ProfilePageModel model)
        {
            model.StopAudioCommand.Execute(null);
        }
    }
}