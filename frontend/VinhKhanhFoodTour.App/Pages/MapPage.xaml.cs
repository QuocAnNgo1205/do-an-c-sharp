using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.Pages;

[QueryProperty(nameof(Lat), "Lat")]
[QueryProperty(nameof(Lon), "Lon")]
[QueryProperty(nameof(Name), "Name")]
public partial class MapPage : ContentPage
{
    private readonly IPoiService _poiService;
    public string Lat { get; set; }
    public string Lon { get; set; }
    public string Name { get; set; }

    public MapPage(IPoiService poiService)
    {
        InitializeComponent();
        _poiService = poiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

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

    private async Task LoadAllPinsAsync()
    {
        try
        {
            var pois = await _poiService.GetPublicPoisAsync();
            foodMap.Pins.Clear();

            foreach (var poi in pois)
            {
                if (poi.Latitude != 0 && poi.Longitude != 0)
                {
                    var pin = new Pin
                    {
                        Label = poi.Name,
                        // 👉 THÊM: Hiện tiêu đề/mô tả dưới tên quán
                        Address = poi.Title ?? poi.Description,
                        Location = new Location(poi.Latitude, poi.Longitude),
                        Type = PinType.Place
                    };

                    foodMap.Pins.Add(pin);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi load ghim: {ex.Message}");
        }
    }
}