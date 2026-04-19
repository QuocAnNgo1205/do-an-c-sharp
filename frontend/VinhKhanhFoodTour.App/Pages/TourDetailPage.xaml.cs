using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.Pages;

[QueryProperty(nameof(TourData), "TourData")]
public partial class TourDetailPage : ContentPage
{
    private readonly PoiCacheService _poiCache;
    private readonly GeofenceManager _geofenceManager;
    private readonly IPoiService _poiService;

    private Tour? _tourData;
    public Tour? TourData
    {
        get => _tourData;
        set
        {
            _tourData = value;
            OnPropertyChanged();
            if (_tourData != null)
            {
                BindingContext = _tourData;
            }
        }
    }

    public TourDetailPage(PoiCacheService poiCache, GeofenceManager geofenceManager, IPoiService poiService)
    {
        InitializeComponent();
        _poiCache = poiCache;
        _geofenceManager = geofenceManager;
        _poiService = poiService;

        // Khôi phục trạng thái Toggle từ Preferences
        AutoPlaySwitch.IsToggled = Preferences.Default.Get("AutoPlayLocationTracking", false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTourPoisAsync();
    }

    private async Task LoadTourPoisAsync()
    {
        try
        {
            // Tạm thời lấy danh sách tất cả các điểm để tạo thành 1 Tour Mặc định
            var pois = await _poiCache.GetCachedPoisAsync();
            
            if (pois.Count == 0)
            {
                pois = await _poiService.GetPublicPoisAsync();
                if (pois.Count > 0)
                {
                    _ = _poiCache.SavePoisAsync(pois);
                }
            }

            // Đã có POI Cache, giờ lọc theo ID của Tour
            List<Poi> orderedPois = new();
            if (TourData != null && TourData.PoiIds != null && TourData.PoiIds.Count > 0)
            {
                // Giữ đúng thứ tự của PoiIds trong Tour
                foreach (var id in TourData.PoiIds)
                {
                    var p = pois.FirstOrDefault(x => x.Id == id);
                    if (p != null) orderedPois.Add(p);
                }
            }
            else
            {
                // Fallback nếu không có TourData cụ thể
                orderedPois = pois.Where(p => p.Latitude != 0).OrderByDescending(p => p.Longitude).ToList();
            }

            TourList.ItemsSource = orderedPois;

            // VẼ BẢN ĐỒ MINI ROUTE DÀNH RIÊNG CHO TOUR NÀY
            DrawTourRouteOnMap(orderedPois);

            // CẬP NHẬT KHOẢNG CÁCH
            _ = UpdateDistancesAsync(orderedPois);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi load Tour: {ex.Message}");
        }
    }

    private async Task UpdateDistancesAsync(List<Poi> pois)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
            if (location == null) return;

            foreach (var poi in pois)
            {
                if (poi.Latitude == 0 || poi.Longitude == 0) continue;

                var poiLocation = new Location(poi.Latitude, poi.Longitude);
                double distance = Location.CalculateDistance(location, poiLocation, DistanceUnits.Kilometers);

                poi.DistanceDisplay = distance < 1 ? $"{(distance * 1000):F0} m" : $"{distance:F1} km";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Không lấy được vị trí: {ex.Message}");
        }
    }

    private void DrawTourRouteOnMap(List<Poi> routePois)
    {
        tourMap.MapElements.Clear();
        tourMap.Pins.Clear();

        if (routePois.Count == 0) return;

        var polyline = new Polyline
        {
            StrokeColor = Color.FromArgb("#FF5722"), // Đổi qua màu cam cho Tour
            StrokeWidth = 8
        };

        double minLat = double.MaxValue, maxLat = double.MinValue;
        double minLon = double.MaxValue, maxLon = double.MinValue;

        foreach (var poi in routePois)
        {
            if (poi.Latitude == 0 || poi.Longitude == 0) continue;

            var loc = new Location(poi.Latitude, poi.Longitude);
            polyline.Geopath.Add(loc);

            var pin = new Pin
            {
                Location = loc,
                Label = poi.Name,
                Type = PinType.Place
            };
            tourMap.Pins.Add(pin);

            // Tìm bounding box để Zoom map
            if (poi.Latitude < minLat) minLat = poi.Latitude;
            if (poi.Latitude > maxLat) maxLat = poi.Latitude;
            if (poi.Longitude < minLon) minLon = poi.Longitude;
            if (poi.Longitude > maxLon) maxLon = poi.Longitude;
        }

        if (polyline.Geopath.Count > 1)
        {
            tourMap.MapElements.Add(polyline);
        }

        if (minLat != double.MaxValue)
        {
            double centerLat = (minLat + maxLat) / 2;
            double centerLon = (minLon + maxLon) / 2;
            
            // Tính khoảng cách để đảm bảo tất cả vừa trong khung (Mini Map bị lấp 1 xíu ở dưới)
            double latDelta = (maxLat - minLat) * 1.5; // Thêm khoảng trắng
            double lonDelta = (maxLon - minLon) * 1.5;
            
            // Tránh trường hợp mảng quá nhỏ k zoom dc
            if (latDelta < 0.005) latDelta = 0.005;
            if (lonDelta < 0.005) lonDelta = 0.005;

            var mapSpan = new MapSpan(new Location(centerLat, centerLon), latDelta, lonDelta);
            tourMap.MoveToRegion(mapSpan);
        }
    }

    private async void OnAutoPlayToggled(object? sender, ToggledEventArgs e)
    {
        bool isEnabled = e.Value;

        if (isEnabled)
        {
            var status = await CheckAndRequestLocationPermission();

            if (status != PermissionStatus.Granted)
            {
                AutoPlaySwitch.IsToggled = false;
                await DisplayAlert("⚠️ Quyền vị trí", "Vui lòng chọn 'Luôn cho phép' trong Cài đặt để chạy ngầm.", "OK");
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
                await DisplayAlert("🧭 Chạy ngầm", "Để nghe khi tắt màn hình, hãy chọn 'Luôn cho phép' ở màn hình tới.", "Đồng ý");
                alwaysStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
            }
            return alwaysStatus;
        }

        return status;
    }

    private async void OnPoiTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Poi selectedPoi)
        {
            // Điều hướng sang trang chi tiết quán ăn
            await Shell.Current.GoToAsync("project", new Dictionary<string, object>
            {
                { "Poi", selectedPoi }
            });
        }
    }
}
