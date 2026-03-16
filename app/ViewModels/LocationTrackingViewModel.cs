using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VinhKhanhFoodTour.Interfaces;
using VinhKhanhFoodTour.Models;
using VinhKhanhFoodTour.Services;

namespace VinhKhanhFoodTour.ViewModels
{
    /// <summary>
    /// ViewModel for location tracking and POI narration.
    /// Implements MVVM pattern with INotifyPropertyChanged for data binding.
    /// 
    /// Responsibilities:
    /// 1. Manage location tracking timer
    /// 2. Periodically request user location (every 5 seconds)
    /// 3. Check each POI for geofence entry
    /// 4. Trigger narration if conditions are met
    /// 5. Maintain narration log for UI display
    /// </summary>
    public class LocationTrackingViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Location tracking interval in milliseconds.
        /// Request location every 5 seconds for PoC.
        /// </summary>
        private const int LOCATION_UPDATE_INTERVAL_MS = 5000;

        private readonly ILocationService _locationService;
        private readonly INarrationService _narrationService;

        private Timer? _locationTrackingTimer;
        private CancellationTokenSource? _narrationCancellationTokenSource;

        private double _currentLatitude;
        private double _currentLongitude;
        private bool _isTracking;
        private string _statusMessage;
        private string _currentLocationText;

        /// <summary>
        /// Observable collection of narration log entries displayed in the UI.
        /// </summary>
        public ObservableCollection<NarrationLogEntry> NarrationLog { get; }

        /// <summary>
        /// Mock list of Points of Interest in Ho Chi Minh City.
        /// </summary>
        private List<PoiModel> _pointsOfInterest;

        public LocationTrackingViewModel(ILocationService locationService, INarrationService narrationService)
        {
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _narrationService = narrationService ?? throw new ArgumentNullException(nameof(narrationService));

            NarrationLog = new ObservableCollection<NarrationLogEntry>();
            _statusMessage = "Ready to track";
            _currentLocationText = "No location yet";
            _isTracking = false;

            InitializeMockPois();
        }

        /// <summary>
        /// Initialize mock POIs in Ho Chi Minh City (District 4 area).
        /// In a real app, these would be loaded from a database or API.
        /// </summary>
        private void InitializeMockPois()
        {
            _pointsOfInterest = new List<PoiModel>
            {
                new PoiModel
                {
                    Id = 1,
                    Name = "Ben Thanh Market",
                    Latitude = 10.7725,
                    Longitude = 106.6992,
                    DescriptionText = "Welcome to Ben Thanh Market, a historic landmark in Ho Chi Minh City. " +
                        "This iconic market has been serving locals and tourists since 1914. " +
                        "You can find souvenirs, textiles, and fresh produce here.",
                    Radius = 100 // Trigger narration within 100 meters
                },
                new PoiModel
                {
                    Id = 2,
                    Name = "Bitexco Financial Tower",
                    Latitude = 10.7598,
                    Longitude = 106.7031,
                    DescriptionText = "You are at Bitexco Financial Tower, the tallest building in Vietnam. " +
                        "The helipad on the roof is famous for the opening scene of the film Kong Skull Island. " +
                        "Great views of the Saigon River from here.",
                    Radius = 120
                },
                new PoiModel
                {
                    Id = 3,
                    Name = "Dong Khoi Street",
                    Latitude = 10.7700,
                    Longitude = 106.7050,
                    DescriptionText = "Welcome to Dong Khoi Street, the most famous shopping street in Ho Chi Minh City. " +
                        "You'll find luxury brands, boutiques, and restaurants along this historic avenue. " +
                        "Perfect for shopping and dining experiences.",
                    Radius = 150
                }
            };
        }

        #region Properties with INotifyPropertyChanged

        public double CurrentLatitude
        {
            get => _currentLatitude;
            set
            {
                if (_currentLatitude != value)
                {
                    _currentLatitude = value;
                    OnPropertyChanged();
                }
            }
        }

        public double CurrentLongitude
        {
            get => _currentLongitude;
            set
            {
                if (_currentLongitude != value)
                {
                    _currentLongitude = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTracking
        {
            get => _isTracking;
            set
            {
                if (_isTracking != value)
                {
                    _isTracking = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentLocationText
        {
            get => _currentLocationText;
            set
            {
                if (_currentLocationText != value)
                {
                    _currentLocationText = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Location Tracking Control

        /// <summary>
        /// Starts the location tracking timer and background location updates.
        /// Called when user taps the "Start Tracking" button.
        /// </summary>
        public async Task StartTrackingAsync()
        {
            if (IsTracking)
            {
                StatusMessage = "Already tracking...";
                return;
            }

            try
            {
                // Verify location permission and service availability
                bool hasPermission = await _locationService.CheckAndRequestPermissionAsync();
                if (!hasPermission)
                {
                    StatusMessage = "Location permission denied";
                    return;
                }

                bool isServiceEnabled = await _locationService.IsLocationServiceEnabledAsync();
                if (!isServiceEnabled)
                {
                    StatusMessage = "Location services disabled";
                    return;
                }

                IsTracking = true;
                StatusMessage = "Tracking active...";
                NarrationLog.Clear();
                Log("Location tracking started");

                // Start the location tracking timer
                // Timer callback executes every 5 seconds
                _locationTrackingTimer = new Timer(
                    async _ => await UpdateLocationAndCheckPoisAsync(),
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromMilliseconds(LOCATION_UPDATE_INTERVAL_MS)
                );
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                IsTracking = false;
            }
        }

        /// <summary>
        /// Stops the location tracking timer and cancels any ongoing TTS playback.
        /// Called when user taps the "Stop Tracking" button.
        /// </summary>
        public void StopTracking()
        {
            if (!IsTracking)
            {
                return;
            }

            try
            {
                // Stop the location tracking timer
                _locationTrackingTimer?.Dispose();
                _locationTrackingTimer = null;

                // Cancel any ongoing TTS narration
                _narrationCancellationTokenSource?.Cancel();
                _narrationCancellationTokenSource?.Dispose();
                _narrationCancellationTokenSource = null;

                IsTracking = false;
                StatusMessage = "Tracking stopped";
                Log("Location tracking stopped");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Stop error: {ex.Message}";
            }
        }

        #endregion

        #region Location & POI Processing

        /// <summary>
        /// Called every 5 seconds by the location tracking timer.
        /// 
        /// Main workflow:
        /// 1. Fetch current device location
        /// 2. Update UI with current coordinates
        /// 3. Check each POI to see if user entered its geofence
        /// 4. Trigger narration for POIs that meet all conditions
        /// </summary>
        private async Task UpdateLocationAndCheckPoisAsync()
        {
            try
            {
                // Request current location from device
                var currentLocation = await _locationService.GetCurrentLocationAsync();

                if (currentLocation == null)
                {
                    StatusMessage = "Unable to get location";
                    return;
                }

                // Update current coordinates
                CurrentLatitude = currentLocation.Latitude;
                CurrentLongitude = currentLocation.Longitude;
                CurrentLocationText = $"Lat: {CurrentLatitude:F6}, Lon: {CurrentLongitude:F6}";

                // Check each POI for geofence entry
                await CheckAllPoisAsync(currentLocation.Latitude, currentLocation.Longitude);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location Update Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks all POIs to determine which ones should trigger narration.
        /// For each POI that meets the trigger conditions:
        /// 1. Log the event
        /// 2. Mark POI as triggered (start cooldown)
        /// 3. Play TTS narration
        /// </summary>
        /// <param name="userLatitude">Current user latitude.</param>
        /// <param name="userLongitude">Current user longitude.</param>
        private async Task CheckAllPoisAsync(double userLatitude, double userLongitude)
        {
            foreach (var poi in _pointsOfInterest)
            {
                // Check if this POI should trigger narration
                // ShouldTriggerNarration checks:
                // 1. Is user within geofence radius?
                // 2. Is the POI not in cooldown period?
                if (_narrationService.ShouldTriggerNarration(poi, userLatitude, userLongitude))
                {
                    await TriggerNarrationAsync(poi, userLatitude, userLongitude);
                }
            }
        }

        /// <summary>
        /// Executes the complete narration workflow for a triggered POI.
        /// </summary>
        /// <param name="poi">The POI to narrate.</param>
        /// <param name="userLatitude">User's current latitude at time of trigger.</param>
        /// <param name="userLongitude">User's current longitude at time of trigger.</param>
        private async Task TriggerNarrationAsync(PoiModel poi, double userLatitude, double userLongitude)
        {
            try
            {
                // Calculate distance for logging
                double distance = _narrationService.CalculateDistance(userLatitude, userLongitude, poi.Latitude, poi.Longitude);

                // Create log entry for this narration event
                var logEntry = new NarrationLogEntry
                {
                    PoiName = poi.Name,
                    TriggeredAt = DateTime.Now,
                    NarrationText = poi.DescriptionText,
                    UserLocation = $"Lat: {userLatitude:F6}, Lon: {userLongitude:F6}"
                };

                // Add to UI log (on main thread)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    NarrationLog.Insert(0, logEntry); // Insert at beginning for newest first
                    StatusMessage = $"Narrating: {poi.Name}";
                });

                // Mark POI as triggered to start cooldown period
                var narrationService = _narrationService as NarrationService;
                narrationService?.MarkPoiAsTriggered(poi.Id);

                // Create new cancellation token source for this narration
                _narrationCancellationTokenSource?.Dispose();
                _narrationCancellationTokenSource = new CancellationTokenSource();

                // Play the narration via TTS
                await _narrationService.PlayNarrationAsync(poi.DescriptionText, _narrationCancellationTokenSource.Token);

                Log($"Narrated: {poi.Name} (Distance: {distance:F1}m)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Narration Error: {ex.Message}");
            }
        }

        #endregion

        #region Logging

        /// <summary>
        /// Adds a timestamped log message to the narration log.
        /// Used for diagnostic and information messages.
        /// </summary>
        private void Log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NarrationLog.Insert(0, new NarrationLogEntry
                {
                    PoiName = "System",
                    TriggeredAt = DateTime.Now,
                    NarrationText = message,
                    UserLocation = ""
                });
            });
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
