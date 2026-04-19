using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using VinhKhanhFoodTour.App.Services;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Pages;

[QueryProperty(nameof(Lat), "Lat")]
[QueryProperty(nameof(Lon), "Lon")]
[QueryProperty(nameof(Name), "Name")]
public partial class MapPage : ContentPage
{
    private readonly IPoiService _poiService;
    private readonly GeofenceManager _geofenceManager;
    private readonly AudioGuideService _audioGuideService;
    private readonly PoiCacheService _poiCache;

    // Ánh xạ từ Pin → Poi để biết quán nào khi nhấn pin
    private readonly Dictionary<Pin, Poi> _poiLookup = new();
    private Poi? _selectedPoi;
    private bool _isCardVisible = false;
    private Circle? _activeHighlightCircle; // ✨ Vòng tròn nổi bật POI đang phát âm thanh

    public string? Lat { get; set; }
    public string? Lon { get; set; }
    public string? Name { get; set; }

    public MapPage(IPoiService poiService, GeofenceManager geofenceManager, AudioGuideService audioGuideService, PoiCacheService poiCache)
    {
        InitializeComponent();
        _poiService = poiService;
        _geofenceManager = geofenceManager;
        _audioGuideService = audioGuideService;
        _poiCache = poiCache;

        // Khôi phục trạng thái Toggle từ Preferences
        autoPlaySwitch.IsToggled = Preferences.Default.Get("AutoPlayLocationTracking", false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 🎧 Lắng nghe sự kiện phát âm thanh để làm nổi bật Map
        _audioGuideService.PlaybackStateChanged += OnAudioPlaybackStateChanged;

        // 🔄 Nếu đang phát sẵn (do người dùng bật từ trang chủ), gọi hiển thị vòng tròn luôn
        if (_audioGuideService.IsPlaying && _audioGuideService.CurrentPlayingPoiId.HasValue)
        {
            OnAudioPlaybackStateChanged(this, (_audioGuideService.CurrentPlayingPoiId.Value, true));
        }

        // Bước 1: Luôn nạp tất cả các quán ăn từ Database lên bản đồ trước
        await LoadAllPinsAsync();

        // Bước 2: Xử lý nếu có tọa độ truyền từ trang chủ sang (Zoom đến quán đó)
        if (!string.IsNullOrEmpty(Lat) && !string.IsNullOrEmpty(Lon))
        {
            if (double.TryParse(Lat, out double latitude) && double.TryParse(Lon, out double longitude))
            {
                var targetLocation = new Location(latitude, longitude);
                foodMap.MoveToRegion(MapSpan.FromCenterAndRadius(targetLocation, Distance.FromKilometers(0.3)));

                // Reset tham số
                Lat = Lon = Name = null;
                return;
            }
        }

        // Bước 3: Nếu vào tab bình thường, zoom về vị trí hiện tại của người dùng
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location != null)
            {
                foodMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1.0)));
            }
        }
        catch { }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Xóa lắng nghe để tránh rò rỉ bộ nhớ (Memory Leak)
        _audioGuideService.PlaybackStateChanged -= OnAudioPlaybackStateChanged;
    }

    private void OnAudioPlaybackStateChanged(object? sender, (int PoiId, bool IsPlaying) e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.IsPlaying)
            {
                // Xóa vòng cũ nếu có
                if (_activeHighlightCircle != null)
                {
                    foodMap.MapElements.Remove(_activeHighlightCircle);
                    _activeHighlightCircle = null;
                }

                // Tìm POI đang phát
                var targetPoi = _poiLookup.Values.FirstOrDefault(p => p.Id == e.PoiId);
                if (targetPoi != null && targetPoi.Latitude != 0 && targetPoi.Longitude != 0)
                {
                    var location = new Location(targetPoi.Latitude, targetPoi.Longitude);
                    
                    // Vẽ vòng tròn bán kính 30m màu Cam nổi bật
                    _activeHighlightCircle = new Circle
                    {
                        Center = location,
                        Radius = Distance.FromMeters(30),
                        StrokeColor = Colors.DarkOrange,
                        StrokeWidth = 8,
                        FillColor = Color.FromRgba(255, 140, 0, 80) // Cam trong suốt 30%
                    };
                    
                    foodMap.MapElements.Add(_activeHighlightCircle);
                    
                    // Zoom camera nhẹ nhàng tới điểm đó
                    foodMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.3)));
                }
            }
            else
            {
                // Tắt âm thanh -> xóa highlight
                if (_activeHighlightCircle != null)
                {
                    foodMap.MapElements.Remove(_activeHighlightCircle);
                    _activeHighlightCircle = null;
                }
            }
        });
    }

    private async Task LoadAllPinsAsync()
    {
        try
        {
            // Tải dữ liệu từ Local SQLite Cache trước (tốc độ < 10ms & hỗ trợ offline)
            var pois = await _poiCache.GetCachedPoisAsync();
            
            // Nếu SQLite trống (lần đầu tiên mở app), fallback gọi Data tĩnh từ Public API
            if (pois.Count == 0)
            {
                pois = await _poiService.GetPublicPoisAsync();
                if (pois.Count > 0)
                {
                    _ = _poiCache.SavePoisAsync(pois); // Lưu ngầm xuống CSDL
                }
            }

            foodMap.Pins.Clear();
            _poiLookup.Clear();
            
            // Xóa đường polyline cũ nếu có (bằng cách xóa toàn bộ các line, chỉ giữ lại circle highlight hiện tại nếu có)
            var elementsToRemove = foodMap.MapElements.Where(e => e is Polyline).ToList();
            foreach(var el in elementsToRemove)
            {
                foodMap.MapElements.Remove(el);
            }

            // 🛠 FIX ZIGZAG: Sắp xếp các điểm theo kinh độ (Tây sang Đông) để đường đi mượt mà không bị lộn xộn
            var orderedPois = pois.Where(p => p.Latitude != 0 && p.Longitude != 0)
                                  .OrderByDescending(p => p.Latitude) // Vĩnh Khánh đi hơi chéo, sắp xếp theo vĩ độ giảm dần là chuẩn
                                  .ToList();

            foreach (var poi in orderedPois)
            {
                if (poi.Latitude != 0 && poi.Longitude != 0)
                {
                    var pin = new Pin
                    {
                        Label = poi.Name,
                        Address = poi.Title ?? poi.Description,
                        Location = new Location(poi.Latitude, poi.Longitude),
                        Type = PinType.Place
                    };

                    var capturedPoi = poi; // Closure capture
                    pin.MarkerClicked += async (s, e) =>
                    {
                        e.HideInfoWindow = true; // Tắt popup mặc định của Maps

                        // PHẢI chạy trên Main Thread mới cập nhật UI được
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            _selectedPoi = capturedPoi;

                            // Cập nhật nội dung Info Card
                            CardName.Text = capturedPoi.Name;

                            string description = capturedPoi.Title ?? capturedPoi.Description ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(description))
                            {
                                description = "Một địa điểm ẩm thực hấp dẫn không thể bỏ lỡ tại phố ẩm thực Vĩnh Khánh. Hãy nhấn vào chi tiết để khám phá ngay!";
                            }
                            CardTitle.Text = description;
                            CardImage.Source = !string.IsNullOrEmpty(capturedPoi.ImageUrl)
                                ? ImageSource.FromUri(new Uri(capturedPoi.ImageUrl))
                                : null;

                            // Trượt card lên
                            await ShowInfoCardAsync();

                            // Zoom bản đồ về quán đó
                            foodMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                                new Location(capturedPoi.Latitude, capturedPoi.Longitude),
                                Distance.FromKilometers(0.3)));
                        });
                    };

                    _poiLookup[pin] = poi;
                    foodMap.Pins.Add(pin);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi load ghim: {ex.Message}");
        }
    }

    // ===== ANIMATION TRƯỢT LÊN =====
    private async Task ShowInfoCardAsync()
    {
        if (_isCardVisible) return;
        _isCardVisible = true;

        // Bắt buộc Android phải measure kích thước bằng cách đặt ngoài tầm nhìn rồi mới hiện
        InfoCard.TranslationY = 500;
        InfoCard.IsVisible = true;

        DimOverlay.IsVisible = true;
        DimOverlay.Opacity = 0;

        await Task.WhenAll(
            InfoCard.TranslateTo(0, 0, 300, Easing.CubicOut),
            DimOverlay.FadeTo(1, 200)
        );
    }

    // ===== ANIMATION TRƯỢT XUỐNG =====
    private async Task HideInfoCardAsync()
    {
        if (!_isCardVisible) return;
        _isCardVisible = false;

        await Task.WhenAll(
            InfoCard.TranslateTo(0, 500, 250, Easing.CubicIn),
            DimOverlay.FadeTo(0, 200)
        );

        DimOverlay.IsVisible = false;
        InfoCard.IsVisible = false;
        _selectedPoi = null;
    }

    // Nhấn nền mờ để đóng card
    private async void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        await HideInfoCardAsync();
    }

    // ===== NÚT NGHE THUYẾT MINH =====
    private async void OnSpeakClicked(object? sender, EventArgs e)
    {
        if (_selectedPoi == null) return;
        Preferences.Default.Set("PendingSpeakPoiId", _selectedPoi.Id);
        await HideInfoCardAsync();
        await Shell.Current.GoToAsync("//MainPage");
    }

    // ===== NÚT XEM CHI TIẾT =====
    private async void OnDetailClicked(object? sender, EventArgs e)
    {
        if (_selectedPoi == null) return;
        var poi = _selectedPoi;
        await HideInfoCardAsync();

        await Shell.Current.GoToAsync("project", new Dictionary<string, object>
        {
            { "Poi", poi }
        });
    }

    private async void OnAutoPlayToggled(object? sender, ToggledEventArgs e)
    {
        bool isEnabled = e.Value;

        if (isEnabled)
        {
            var status = await CheckAndRequestLocationPermission();

            if (status != PermissionStatus.Granted)
            {
                autoPlaySwitch.IsToggled = false;
                await ShowLocationAlertAsync("⚠️ Quyền vị trí", "Vui lòng chọn 'Luôn cho phép' trong Cài đặt để chạy ngầm.", "OK");
                return;
            }

            _geofenceManager.Start();
            Preferences.Default.Set("AutoPlayLocationTracking", true);
        }
        else
        {
            _geofenceManager.Stop();
            Preferences.Default.Set("AutoPlayLocationTracking", false);
        }
    }

    private async Task<PermissionStatus> CheckAndRequestLocationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (status == PermissionStatus.Granted)
        {
            var alwaysStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (alwaysStatus != PermissionStatus.Granted)
            {
                await ShowLocationAlertAsync("🧭 Chạy ngầm", "Để nghe khi tắt màn hình, hãy chọn 'Luôn cho phép' ở màn hình tới.", "Đồng ý");
                alwaysStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
            }
            return alwaysStatus;
        }

        return status;
    }

    private async Task ShowLocationAlertAsync(string title, string message, string cancel)
    {
        await DisplayAlert(title, message, cancel);
    }
}