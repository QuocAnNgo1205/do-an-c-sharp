using System.Diagnostics;
using Plugin.Maui.Audio;
using VinhKhanhFoodTour.App.Data;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services;

/// <summary>
/// Service quản lý "Thuyết minh thông minh" (Audio Guide) cho các POI
/// 
/// 🎯 Luồng Fallback 3 Lớp (Smart Audio Guide):
/// 1. MP3 Player (Plugin.Maui.Audio)
/// 2. Text-to-Speech (MAUI Default)
/// 3. Error handling
/// </summary>
public class AudioGuideService
{
    private readonly IAudioManager _audioManager;
    private readonly ApiService _apiService;
    private IAudioPlayer? _activePlayer;
    private bool _isPlaying = false;
    private CancellationTokenSource? _playingCancellation;

    // 🛑 MỚI: Event phát tín hiệu khi âm thanh bắt đầu hoặc dừng (Cho UI cập nhật)
    public event EventHandler<(int PoiId, bool IsPlaying)>? PlaybackStateChanged;
    private int? _currentPlayingPoiId;

    public AudioGuideService(IAudioManager audioManager, ApiService apiService)
    {
        _audioManager = audioManager;
        _apiService = apiService;
    }

    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Phát âm thanh cho POI với logic Fallback và Log thống kê
    /// </summary>
    /// <param name="poi">Đối tượng POI cần phát</param>
    /// <param name="onStateChanged">Callback thông báo trạng thái (true=đang phát, false=dừng)</param>
    public async Task PlayAudioAsync(Poi poi, Action<bool>? onStateChanged = null, bool showErrors = true)
    {
        // 🛑 MỚI: Tự động lấy ngôn ngữ từ Preferences (Local Storage)
        string languageCode = Preferences.Default.Get("PreferredLanguage", Constants.DEFAULT_LANGUAGE_CODE);
        
        var translation = GetPreferredTranslation(poi, languageCode);
        if (translation == null)
        {
            Debug.WriteLine($"[AudioGuide ERROR] No translation found for POI: {poi?.Name} (ID: {poi?.Id}) in Language: {languageCode}");
            if (poi?.Translations == null || poi.Translations.Count == 0)
            {
                Debug.WriteLine("[AudioGuide ERROR] POI Translations list is NULL or EMPTY.");
            }
            onStateChanged?.Invoke(false);
            if (showErrors)
                await ShowErrorMessage($"Không tìm thấy nội dung thuyết minh ({languageCode}) cho địa điểm này.");
            return;
        }

        try
        {
            // 🛑 Bước 1: Dừng âm thanh đang phát
            await StopAudioAsync();

            _isPlaying = true;
            _currentPlayingPoiId = poi.Id;
            _playingCancellation = new CancellationTokenSource();
            
            // Báo cho UI biết là quán này bắt đầu phát
            PlaybackStateChanged?.Invoke(this, (poi.Id, true));

            // 🎵 Bước 2: Thử phát MP3 (Layer 1)
            if (!string.IsNullOrWhiteSpace(translation.AudioFilePath))
            {
                var audioUrl = BuildAudioUrl(translation.AudioFilePath);
                Debug.WriteLine($"[AudioGuide] 🎵 Attempting MP3 playback: {audioUrl}");

                try
                {
                    _activePlayer = _audioManager.CreatePlayer(audioUrl);
                    _activePlayer.PlaybackEnded += (s, e) => 
                    {
                        _isPlaying = false;
                        onStateChanged?.Invoke(false);
                        PlaybackStateChanged?.Invoke(this, (poi.Id, false));
                    };
                    _activePlayer.Play();
                    onStateChanged?.Invoke(true);
                    
                    // Ghi log thống kê (Fire-and-forget)
                    _ = LogListenAsync(poi.Id);
                    
                    return; // Thành công lớp 1
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioGuide] MP3 Player failed: {ex.Message}. Falling back to TTS.");
                }
            }

            // 🔊 Bước 3: Fallback TTS (Layer 2)
            Debug.WriteLine($"[AudioGuide] 🔊 Falling back to TTS for {languageCode}");
            var textToRead = $"{translation.Title}. {translation.Description}";
            
            // Tìm Locale phù hợp (vi-VN, en-US, ...)
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var targetLocale = locales.FirstOrDefault(l => 
                l.Language.Equals(languageCode, StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains(GetLocaleName(languageCode)));

            onStateChanged?.Invoke(true);
            await TextToSpeech.Default.SpeakAsync(textToRead, new SpeechOptions
            {
                Locale = targetLocale,
                Pitch = 1.0f,
                Volume = 1.0f
            }, cancelToken: _playingCancellation.Token);

            onStateChanged?.Invoke(false);

            // Ghi log thống kê (Fire-and-forget)
            _ = LogListenAsync(poi.Id);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[AudioGuide] Playback cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioGuide] ❌ Error in AudioGuideService: {ex.Message}");
            if (showErrors)
                await ShowErrorMessage($"Lỗi phát thuyết minh: {ex.Message}");
        }
        finally
        {
            _isPlaying = false;
            // Lưu ý: Đối với MP3 player, onStateChanged(false) được gọi trong PlaybackEnded.
            // Ở đây ta gọi nếu là TTS hoặc có lỗi xảy ra trước khi MP3 kịp bắt đầu.
            if (_activePlayer == null || !_activePlayer.IsPlaying)
            {
                onStateChanged?.Invoke(false);
                PlaybackStateChanged?.Invoke(this, (poi.Id, false));
            }
        }
    }

    /// <summary>
    /// Dừng phát âm thanh ngay lập tức
    /// </summary>
    public async Task StopAudioAsync()
    {
        try
        {
            // Stop TTS
            _playingCancellation?.Cancel();
            _playingCancellation = null;

            // Stop MP3 Player
            if (_activePlayer != null)
            {
                if (_activePlayer.IsPlaying)
                    _activePlayer.Stop();
                
                _activePlayer.Dispose();
                _activePlayer = null;
            }

            _isPlaying = false;
            
            if (_currentPlayingPoiId.HasValue)
            {
                PlaybackStateChanged?.Invoke(this, (_currentPlayingPoiId.Value, false));
                _currentPlayingPoiId = null;
            }

            Debug.WriteLine("[AudioGuide] ⏹️ All audio stopped");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioGuide] Error stopping audio: {ex.Message}");
        }
    }

    /// <summary>
    /// Ghi log lượt nghe về Server (Chạy ngầm)
    /// </summary>
    private async Task LogListenAsync(int poiId)
    {
        try
        {
            var deviceId = Preferences.Get("AppDeviceId", Guid.NewGuid().ToString());
            Preferences.Set("AppDeviceId", deviceId);

            var logData = new
            {
                PoiId = poiId,
                DeviceId = deviceId,
                Timestamp = DateTime.UtcNow
            };

            // Gọi API endpoint: POST api/v1/Sync/logs
            await _apiService.PostAsync("Sync/logs", logData);
        }
        catch (Exception ex)
        {
            // Tuyệt đối không throw lỗi để tránh ảnh hưởng trải nghiệm nghe
            Debug.WriteLine($"[AudioGuide] Failed to log listen stats: {ex.Message}");
        }
    }

    private string BuildAudioUrl(string audioPath)
    {
        if (audioPath.StartsWith("http")) return audioPath;
        // Constants.API_BASE_URL thường kết thúc bằng /api/v1 -> cần trỏ về root server
        var rootUrl = Constants.API_BASE_URL.Replace("/api/v1", "");
        return $"{rootUrl.TrimEnd('/')}/{audioPath.TrimStart('/')}";
    }

    private string GetLocaleName(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "vi" => "Vietnam",
            "en" => "English",
            _ => "English"
        };
    }

    private async Task ShowErrorMessage(string message)
    {
        var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (currentPage != null)
            await currentPage.DisplayAlertAsync("⚠️ Thông báo", message, "OK");
    }

    public PoiTranslation? GetPreferredTranslation(Poi poi, string? preferredLanguage = null)
    {
        if (poi?.Translations == null || poi.Translations.Count == 0) return null;
        
        var lang = preferredLanguage ?? Constants.DEFAULT_LANGUAGE_CODE;
        
        // 1. Tìm đúng ngôn ngữ ưu tiên
        var preferred = poi.Translations.FirstOrDefault(t => t.LanguageCode.Equals(lang, StringComparison.OrdinalIgnoreCase));
        if (preferred != null) return preferred;

        // 2. Fallback 1: Nếu không có tiếng Anh/Hàn... thì quay về Tiếng Việt
        if (lang != Constants.DEFAULT_LANGUAGE_CODE)
        {
            var fallback = poi.Translations.FirstOrDefault(t => t.LanguageCode.Equals(Constants.DEFAULT_LANGUAGE_CODE, StringComparison.OrdinalIgnoreCase));
            if (fallback != null) return fallback;
        }

        // 3. Fallback 2: Lấy bản dịch đầu tiên bất kỳ nếu vẫn không có tiếng Việt
        return poi.Translations.FirstOrDefault();
    }
}
