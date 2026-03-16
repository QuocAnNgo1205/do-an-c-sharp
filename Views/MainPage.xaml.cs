using VinhKhanhFoodTour.ViewModels;

namespace VinhKhanhFoodTour;

/// <summary>
/// Main page of the Vinh Khanh Food Tour application.
/// Provides UI for location tracking and narration log display.
/// </summary>
public partial class MainPage : ContentPage
{
    private LocationTrackingViewModel _viewModel = null!;

    public MainPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called when the page is loaded.
    /// Initializes the ViewModel and sets up data binding.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Get ViewModel from dependency injection container
        _viewModel = IPlatformApplication.Current?.Services.GetService<LocationTrackingViewModel>()
            ?? throw new InvalidOperationException("LocationTrackingViewModel not found in DI container");

        // Set the data context for XAML binding
        BindingContext = _viewModel;
    }

    /// <summary>
    /// Event handler for the "Start Tracking" button.
    /// Initiates location tracking and POI monitoring.
    /// </summary>
    private async void OnStartTrackingClicked(object sender, EventArgs e)
    {
        try
        {
            await _viewModel.StartTrackingAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to start tracking: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Event handler for the "Stop Tracking" button.
    /// Stops location tracking and cancels any ongoing TTS playback.
    /// </summary>
    private void OnStopTrackingClicked(object sender, EventArgs e)
    {
        try
        {
            _viewModel.StopTracking();
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to stop tracking: {ex.Message}", "OK");
        }
    }
}
