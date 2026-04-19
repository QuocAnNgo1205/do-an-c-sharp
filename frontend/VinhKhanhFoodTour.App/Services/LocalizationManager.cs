using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VinhKhanhFoodTour.App.Services;

public class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private string _currentLanguage = "vi";

    private readonly Dictionary<string, Dictionary<string, string>> _localizedTexts = new()
    {
        {
            "vi", new Dictionary<string, string>
            {
                { "SearchPlaceholder", "Tìm món ngon..." },
                { "FoodStreetTitle", "📍 Phố ẩm thực Vĩnh Khánh" },
                { "FoodStreetDesc", "Thiên đường ẩm thực Quận 4" },
                { "DiscoverTour", "Khám phá Phố Vĩnh Khánh" },
                { "AllPlaces", "Tất cả địa điểm ngon" },
                { "AudioPlay", "🎧 Thuyết minh" },
                { "AudioStop", "⏹ Dừng phát" },
                { "ViewDetail", "✨ Xem Chi Tiết" },
                { "AddressLabel", "Địa chỉ nhận diện:" },
                { "MapButton", "📍 Bản Đồ" },
                { "TourStops", "Điểm dừng tour" },
                { "SimulateMove", "Tự động phát khi đến gần (GPS)" },
                { "TabPlaces", "Địa điểm" },
                { "TabMap", "Bản đồ" },
                { "TabProfile", "Hồ sơ" },
                { "LanguageTitle", "🌍 QUỐC GIA & NGÔN NGỮ" },
                { "LanguageHint", "💡 Hệ thống sẽ tự động phát bằng ngôn ngữ này khi bạn đến gần quán." },
                { "AutoPlaySubtitle", "Tự động thuyết minh chạy ngầm" },
                { "AvailableTours", "tour có sẵn" },
                { "StopsText", "điểm dừng" },
                { "MinsText", "phút" },
                { "TourDetailTitle", "Chi tiết Tour" },
                { "PlaceDetailTitle", "Thông tin chi tiết" }
            }
        },
        {
            "en", new Dictionary<string, string>
            {
                { "SearchPlaceholder", "Search delicious food..." },
                { "FoodStreetTitle", "📍 Vinh Khanh Food Street" },
                { "FoodStreetDesc", "District 4 Food Paradise" },
                { "DiscoverTour", "Discover Vinh Khanh" },
                { "AllPlaces", "All delicious places" },
                { "AudioPlay", "🎧 Play Audio" },
                { "AudioStop", "⏹ Stop Audio" },
                { "ViewDetail", "✨ View Details" },
                { "AddressLabel", "Location address:" },
                { "MapButton", "📍 Map" },
                { "TourStops", "Tour stops" },
                { "SimulateMove", "Auto-play when nearby (GPS)" },
                { "TabPlaces", "Places" },
                { "TabMap", "Map" },
                { "TabProfile", "Profile" },
                { "LanguageTitle", "🌍 COUNTRY & LANGUAGE" },
                { "LanguageHint", "💡 The system will auto-play audio in this language when nearby." },
                { "AutoPlaySubtitle", "Background auto-play audio" },
                { "AvailableTours", "available tours" },
                { "StopsText", "stops" },
                { "MinsText", "mins" },
                { "TourDetailTitle", "Tour Details" },
                { "PlaceDetailTitle", "Place Details" }
            }
        },
        {
            "ko", new Dictionary<string, string>
            {
                { "SearchPlaceholder", "맛있는 음식 검색..." },
                { "FoodStreetTitle", "📍 빈칸 푸드 스트리트" },
                { "FoodStreetDesc", "4군 음식 천국" },
                { "DiscoverTour", "빈칸 둘러보기" },
                { "AllPlaces", "모든 맛집" },
                { "AudioPlay", "🎧 음성 가이드" },
                { "AudioStop", "⏹ 중지" },
                { "ViewDetail", "✨ 상세 보기" },
                { "AddressLabel", "주소:" },
                { "MapButton", "📍 지도" },
                { "TourStops", "투어 정류장" },
                { "SimulateMove", "근처에서 자동 재생 (GPS)" },
                { "TabPlaces", "장소" },
                { "TabMap", "지도" },
                { "TabProfile", "프로필" },
                { "LanguageTitle", "🌍 국가 및 언어" },
                { "LanguageHint", "💡 근처에 가면 이 언어로 자동 재생됩니다." },
                { "AutoPlaySubtitle", "백그라운드 자동 재생" },
                { "AvailableTours", "이용 가능한 투어" },
                { "StopsText", "정거장" },
                { "MinsText", "분" },
                { "TourDetailTitle", "투어 상세정보" },
                { "PlaceDetailTitle", "장소 상세정보" }
            }
        },
        {
            "ja", new Dictionary<string, string>
            {
                { "SearchPlaceholder", "美味しい食べ物を検索..." },
                { "FoodStreetTitle", "📍 ビンカン・フードストリート" },
                { "FoodStreetDesc", "第4区の食の楽園" },
                { "DiscoverTour", "ビンカンを発見" },
                { "AllPlaces", "すべての美味しい店" },
                { "AudioPlay", "🎧 音声ガイド" },
                { "AudioStop", "⏹ 停止" },
                { "ViewDetail", "✨ 詳細を見る" },
                { "AddressLabel", "住所:" },
                { "MapButton", "📍 マップ" },
                { "TourStops", "ツアーストップ" },
                { "SimulateMove", "近くで自動再生 (GPS)" },
                { "TabPlaces", "場所" },
                { "TabMap", "マップ" },
                { "TabProfile", "プロフィール" },
                { "LanguageTitle", "🌍 国とゼ言語" },
                { "LanguageHint", "💡 近くにいると自動的にこの言語で再生します。" },
                { "AutoPlaySubtitle", "バックグラウンド自動再生" },
                { "AvailableTours", "利用可能なツアー" },
                { "StopsText", "ストップ" },
                { "MinsText", "分" },
                { "TourDetailTitle", "ツアー詳細" },
                { "PlaceDetailTitle", "場所の詳細" }
            }
        }
    };

    /// <summary>
    /// Indexer cho phép WPF/MAUI XAML truy cập động dạng Binding [Key]
    /// </summary>
    public string this[string text]
    {
        get
        {
            if (_localizedTexts.ContainsKey(_currentLanguage) && _localizedTexts[_currentLanguage].ContainsKey(text))
            {
                return _localizedTexts[_currentLanguage][text];
            }
            
            // Tự động rớt về tiếng việt nếu chưa dịch kịp
            if (_localizedTexts["vi"].ContainsKey(text))
            {
                return _localizedTexts["vi"][text];
            }

            return text; // Trả về nội dung nguyên thủy nếu không có trong file dịch
        }
    }

    /// <summary>
    /// Chuyển đổi ngôn ngữ và thông báo sự kiện thay đổi cho toàn hệ thống XAML
    /// </summary>
    public void SetLanguage(string languageCode)
    {
        if (_currentLanguage != languageCode)
        {
            _currentLanguage = languageCode;
            // null: Bắt tất cả Properties phải vẽ lại -> UI sẽ bị giật một phát để map text mới!
            OnPropertyChanged(null); 
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
