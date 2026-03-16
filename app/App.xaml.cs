namespace VinhKhanhFoodTour;

/// <summary>
/// MAUI Application class.
/// Sets up the main shell and initial page when the app starts.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Set the MainPage to the MainPage view
        // This is the entry point for the UI
        MainPage = new MainPage();
    }
}
