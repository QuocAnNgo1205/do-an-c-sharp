using System.Diagnostics;
using System.Threading;
using Plugin.Maui.Audio;
using VinhKhanhFoodTour.App.Data;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services;

/// <summary>
/// Service quản lý "Thuyết minh thông minh" (Audio Guide) cho các POI
/// 
/// 🎯 Luồng Fallback 3 Lớp cho mỗi ngôn ngữ:
/// [Layer 1 - MP3]  : Có sẵn file audio upload lên server → phát MP3
/// [Layer 2 - TTS]  : Không có MP3 → Dùng TextToSpeech với đúng Locale
///     - Lớp 2a: Có bản dịch targetLang trong poi.Translations → đọc TTS đúng ngôn ngữ
///     - Lớp 2b: Không có bản dịch → AutoTranslate text tiếng Việt → đọc TTS đúng ngôn ngữ
///     - Lớp 2c: Dịch thất bại / mất mạng → Fallback tiếng Việt với Locale "vi" cứng
/// </summary>
public class AudioGuideService
{
    private readonly IAudioManager _audioManager;
    private readonly ApiService _apiService;
    private IAudioPlayer? _activePlayer;
    private bool _isPlaying = false;
    private CancellationTokenSource? _playingCancellation;
    
    // Hàng chờ phát thuyết minh
    private readonly Queue<Poi> _playbackQueue = new();
    private bool _isProcessingQueue = false;

    // Mutex chống spam nút gẫy bộ nhớ
    private readonly SemaphoreSlim _playLock = new SemaphoreSlim(1, 1);

    // Event phát tín hiệu khi âm thanh bắt đầu hoặc dừng (Cho UI cập nhật)
    public event EventHandler<(int PoiId, bool IsPlaying)>? PlaybackStateChanged;
    private int? _currentPlayingPoiId;

    // HttpClient dùng riêng cho việc auto-dịch (tránh vòng lặp với ApiService)
    private static readonly HttpClient _translateClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public AudioGuideService(IAudioManager audioManager, ApiService apiService)
    {
        _audioManager = audioManager;
        _apiService = apiService;
    }

    public bool IsPlaying => _isPlaying;
    public int? CurrentPlayingPoiId => _currentPlayingPoiId;
    public int QueueCount => _playbackQueue.Count;

    /// <summary>
    /// Thêm POI vào hàng chờ phát.
    /// Nếu đang không phát gì, sẽ tự động bắt đầu hàng chờ.
    /// </summary>
    public void EnqueuePoi(Poi poi)
    {
        if (poi == null) return;
        
        // Nếu đã có trong hàng chờ hoặc đang phát chính nó -> Bỏ qua tránh lặp
        if (_currentPlayingPoiId == poi.Id || _playbackQueue.Any(p => p.Id == poi.Id))
            return;

        _playbackQueue.Enqueue(poi);
        Debug.WriteLine($"[AudioGuide] ➕ Enqueued: {poi.Name}. Queue size: {_playbackQueue.Count}");

        // Nếu đang không phát, kích hoạt xử lý hàng chờ
        if (!_isPlaying && !_isProcessingQueue)
        {
            _ = ProcessQueueAsync();
        }
    }

    private async Task ProcessQueueAsync()
    {
        // Guard: Nếu đang trong vòng lặp xử lý hàng chờ rồi thì thoát để tránh 2 luồng chạy song song
        if (_isProcessingQueue) return;
        _isProcessingQueue = true;

        try
        {
            // Không kiểm tra !_isPlaying trong while vì PlayAudioAsync sẽ tự đợi lock
            // Vòng lặp chỉ thoát khi hàng chờ trống hoặc bị xóa (StopAudioAsync)
            while (_playbackQueue.Count > 0)
            {
                var nextPoi = _playbackQueue.Dequeue();
                Debug.WriteLine($"[AudioGuide] ⏭️ Next from queue: {nextPoi.Name}");
                await PlayAudioAsync(nextPoi);
                
                // Kiểm tra nếu hàng chờ bị xóa giữa chừng (người dùng nhấn Dừng)
                // _playbackQueue.Count sẽ là 0 sau StopAudioAsync() → while tự thoát
            }
        }
        finally
        {
            _isProcessingQueue = false;
        }
    }

    /// <summary>
    /// Phát âm thanh cho POI với logic Fallback 3 Lớp hoàn chỉnh
    /// </summary>
    public async Task PlayAudioAsync(Poi poi, Action<bool>? onStateChanged = null, bool showErrors = true)
    {
        if (poi == null) return;

        // ĐỢI/KHOÁ tiến trình nếu đang có tác vụ Play khác xử lý (Chống Spam click nút)
        await _playLock.WaitAsync();

        // Lấy ngôn ngữ đang chọn của App từ Preferences
        string targetLang = Preferences.Default.Get("PreferredLanguage", Constants.DEFAULT_LANGUAGE_CODE);

        try
        {
            await StopAudioAsync();

            _isPlaying = true;
            _currentPlayingPoiId = poi.Id;
            _playingCancellation = new CancellationTokenSource();
            PlaybackStateChanged?.Invoke(this, (poi.Id, true));

            // ============================================================
            // [LAYER 1] Tìm translation có sẵn cho targetLang
            // ============================================================
            var translation = poi.Translations?
                .FirstOrDefault(t => t.LanguageCode.Equals(targetLang, StringComparison.OrdinalIgnoreCase));

            // [LAYER 1 - MP3] Nếu có file audio → phát MP3, xong
            if (translation != null && !string.IsNullOrWhiteSpace(translation.AudioFilePath))
            {
                var audioUrl = BuildAudioUrl(translation.AudioFilePath);
                Debug.WriteLine($"[AudioGuide] 🎵 [L1-MP3] Playing: {audioUrl}");

                try
                {
                    _activePlayer = _audioManager.CreatePlayer(audioUrl);
                    _activePlayer.PlaybackEnded += (s, e) =>
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            _isPlaying = false;
                            onStateChanged?.Invoke(false);
                            PlaybackStateChanged?.Invoke(this, (poi.Id, false));
                            
                            // Tự động kiểm tra hàng chờ khi kết thúc
                            await Task.Delay(1000); // Nghỉ 1s giữa 2 quán
                            _ = ProcessQueueAsync();
                        });
                    };
                    _activePlayer.Play();
                    onStateChanged?.Invoke(true);
                    _ = LogListenAsync(poi.Id);
                    return; // ✅ Thành công Layer 1
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioGuide] MP3 failed: {ex.Message}. Falling to TTS.");
                }
            }

            // ============================================================
            // [LAYER 2] Không có MP3 → Dùng TTS với Locale đúng ngôn ngữ
            // ============================================================
            
            // Bước 2.1: Lấy danh sách giọng hỗ trợ từ thiết bị
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            
            string textToSpeak;
            string localeToUse;

            // [LAYER 2a] Có bản dịch targetLang (nhưng không có MP3) → đọc bản dịch đó
            if (translation != null && !string.IsNullOrWhiteSpace(translation.Title))
            {
                textToSpeak = $"{translation.Title}. {translation.Description}";
                localeToUse = targetLang;
                Debug.WriteLine($"[AudioGuide] 🔊 [L2a-TTS] Using existing translation for '{targetLang}'");
            }
            else
            {
                // [LAYER 2b] Không có bản dịch → Lấy text tiếng Việt + Auto-dịch
                var vietTranslation = poi.Translations?
                    .FirstOrDefault(t => t.LanguageCode.Equals(Constants.DEFAULT_LANGUAGE_CODE, StringComparison.OrdinalIgnoreCase))
                    ?? poi.Translations?.FirstOrDefault();

                var vietText = vietTranslation != null
                    ? $"{vietTranslation.Title}. {vietTranslation.Description}"
                    : $"{poi.Name}. {poi.Description}";

                if (!targetLang.Equals(Constants.DEFAULT_LANGUAGE_CODE, StringComparison.OrdinalIgnoreCase))
                {
                    // Thử tự động dịch
                    Debug.WriteLine($"[AudioGuide] 🌐 [L2b-AutoTranslate] Translating from 'vi' to '{targetLang}'...");
                    var translated = await AutoTranslateAsync(vietText, Constants.DEFAULT_LANGUAGE_CODE, targetLang);

                    if (translated != null)
                    {
                        // Dịch thành công → đọc bằng ngôn ngữ targetLang
                        textToSpeak = translated;
                        localeToUse = targetLang;
                        Debug.WriteLine($"[AudioGuide] ✅ AutoTranslate success for '{targetLang}'");
                    }
                    else
                    {
                        // [LAYER 2c] Dịch thất bại → Fallback an toàn về tiếng Việt
                        textToSpeak = vietText;
                        localeToUse = Constants.DEFAULT_LANGUAGE_CODE; // Bắt buộc "vi" để tránh giọng Nhật đọc chữ Việt
                        Debug.WriteLine($"[AudioGuide] ⚠️ [L2c-Fallback] Translate failed. Using Vietnamese text with 'vi' locale.");
                    }
                }
                else
                {
                    // targetLang chính là tiếng Việt, dùng luôn
                    textToSpeak = vietText;
                    localeToUse = Constants.DEFAULT_LANGUAGE_CODE;
                }
            }

            // Bước 2.2: Tìm đúng Locale object trên thiết bị khớp với localeToUse
            var speechLocale = FindBestLocale(locales, localeToUse);
            Debug.WriteLine($"[AudioGuide] 🗣️ TTS Locale selected: {speechLocale?.Language ?? "null"} / {speechLocale?.Name ?? "null"} (wanted: {localeToUse})");

            // Nếu không tìm được locale (thiết bị không cài tiếng đó) → ép về tiếng Việt
            if (speechLocale == null && !localeToUse.Equals(Constants.DEFAULT_LANGUAGE_CODE))
            {
                Debug.WriteLine($"[AudioGuide] ⚠️ Locale '{localeToUse}' not found on device. Falling to Vietnamese locale.");
                var vietTranslation = poi.Translations?
                    .FirstOrDefault(t => t.LanguageCode.Equals(Constants.DEFAULT_LANGUAGE_CODE, StringComparison.OrdinalIgnoreCase))
                    ?? poi.Translations?.FirstOrDefault();
                textToSpeak = vietTranslation != null
                    ? $"{vietTranslation.Title}. {vietTranslation.Description}"
                    : $"{poi.Name}. {poi.Description}";

                speechLocale = FindBestLocale(locales, Constants.DEFAULT_LANGUAGE_CODE);
            }

            onStateChanged?.Invoke(true);
            await TextToSpeech.Default.SpeakAsync(textToSpeak, new SpeechOptions
            {
                Locale = speechLocale, // null = giọng mặc định thiết bị (chấp nhận được)
                Pitch = 1.0f,
                Volume = 1.0f
            }, cancelToken: _playingCancellation.Token);

            onStateChanged?.Invoke(false);
            _ = LogListenAsync(poi.Id);

            // TTS kết thúc (SpeakAsync đã hoàn thành) -> Kiểm tra hàng chờ
            await Task.Delay(1000);
            _ = ProcessQueueAsync();
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[AudioGuide] ⏹️ Playback cancelled by user");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioGuide] ❌ Error: {ex.Message}");
            if (showErrors)
                await ShowErrorMessage($"Lỗi phát thuyết minh: {ex.Message}");
        }
        finally
        {
            // BUG FIX: Chỉ reset _isPlaying nếu player MP3 KHÔNG còn đang chạy.
            // Nếu player đang chạy (Layer 1 MP3), _isPlaying sẽ được set về false
            // bởi sự kiện PlaybackEnded — KHÔNG reset sớm ở đây.
            if (_activePlayer == null || !_activePlayer.IsPlaying)
            {
                _isPlaying = false;
                onStateChanged?.Invoke(false);
                PlaybackStateChanged?.Invoke(this, (poi.Id, false));
            }

            // Giải phóng Mutex để lần Play tiếp theo được chạy
            _playLock.Release();
        }
    }

    /// <summary>
    /// Tìm Locale tốt nhất trên thiết bị khớp với mã ngôn ngữ cho trước.
    /// Ưu tiên: Language khớp chính xác > Country code > tên locale chứa từ khoá.
    /// </summary>
    private static Locale? FindBestLocale(IEnumerable<Locale> locales, string languageCode)
    {
        var code = languageCode.ToLowerInvariant();

        // Thử 1: Language code khớp chính xác (VD: "vi", "en", "ja", "ko")
        var match = locales.FirstOrDefault(l =>
            l.Language.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        // Thử 2: Country code (VD: locale "vi-VN" → Country = "VN" nhưng Language = "vi")
        // Một số thiết bị trả Language = "vi", Country = "VN"
        match = locales.FirstOrDefault(l =>
            l.Country?.Equals(GetCountryCode(code), StringComparison.OrdinalIgnoreCase) == true);
        if (match != null) return match;

        // Thử 3: Tên locale chứa tên ngôn ngữ đầy đủ
        var langName = GetLocaleName(code);
        match = locales.FirstOrDefault(l =>
            l.Name.Contains(langName, StringComparison.OrdinalIgnoreCase));
        return match;
    }

    /// <summary>
    /// Tự động dịch văn bản dùng MyMemory API (miễn phí, không cần key).
    /// Giới hạn: 5000 ký tự/request, 500 request/ngày với IP tự do.
    /// </summary>
    private static async Task<string?> AutoTranslateAsync(string text, string sourceLang, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            // Cắt ngắn để tránh vượt giới hạn API
            var safeText = text.Length > 500 ? text[..500] + "..." : text;
            var langPair = $"{sourceLang}|{targetLang}";
            var encodedText = Uri.EscapeDataString(safeText);
            var url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={langPair}";

            var response = await _translateClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            // Parse thủ công để không cần thêm dependency
            // Response: {"responseData":{"translatedText":"...","match":1},"responseStatus":200,...}
            var startKey = "\"translatedText\":\"";
            var startIdx = json.IndexOf(startKey);
            if (startIdx < 0) return null;

            startIdx += startKey.Length;
            var endIdx = json.IndexOf("\"", startIdx);
            if (endIdx < 0) return null;

            var translated = json[startIdx..endIdx];
            // Unescape JSON unicode
            translated = System.Text.RegularExpressions.Regex.Unescape(translated);

            if (string.IsNullOrWhiteSpace(translated) || translated == safeText) return null;

            Debug.WriteLine($"[AutoTranslate] ✅ {sourceLang}→{targetLang}: \"{translated[..Math.Min(60, translated.Length)]}...\"");
            return translated;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoTranslate] ❌ Failed: {ex.Message}");
            return null; // Caller sẽ dùng fallback tiếng Việt
        }
    }

    /// <summary>Dừng phát âm thanh ngay lập tức</summary>
    public async Task StopAudioAsync()
    {
        try
        {
            _playingCancellation?.Cancel();
            _playingCancellation = null;

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

            // Xoá hàng chờ khi người dùng bấm Dừng thủ công
            _playbackQueue.Clear();

            Debug.WriteLine("[AudioGuide] ⏹️ All audio stopped and queue cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioGuide] Error stopping audio: {ex.Message}");
        }
    }

    /// <summary>Ghi log lượt nghe về Server (Chạy ngầm, không throw)</summary>
    private async Task LogListenAsync(int poiId)
    {
        try
        {
            var deviceId = Preferences.Get("AppDeviceId", Guid.NewGuid().ToString());
            Preferences.Set("AppDeviceId", deviceId);

            await _apiService.PostAsync("Sync/logs", new
            {
                PoiId = poiId,
                DeviceId = deviceId,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioGuide] Failed to log listen stats: {ex.Message}");
        }
    }

    /// <summary>Lấy bản dịch ưu tiên theo ngôn ngữ, fallback về 'vi' rồi bản dịch đầu tiên</summary>
    public PoiTranslation? GetPreferredTranslation(Poi poi, string? preferredLanguage = null)
    {
        if (poi?.Translations == null || poi.Translations.Count == 0) return null;

        var lang = preferredLanguage ?? Constants.DEFAULT_LANGUAGE_CODE;

        var preferred = poi.Translations.FirstOrDefault(t =>
            t.LanguageCode.Equals(lang, StringComparison.OrdinalIgnoreCase));
        if (preferred != null) return preferred;

        if (!lang.Equals(Constants.DEFAULT_LANGUAGE_CODE, StringComparison.OrdinalIgnoreCase))
        {
            var fallback = poi.Translations.FirstOrDefault(t =>
                t.LanguageCode.Equals(Constants.DEFAULT_LANGUAGE_CODE, StringComparison.OrdinalIgnoreCase));
            if (fallback != null) return fallback;
        }

        return poi.Translations.FirstOrDefault();
    }

    private string BuildAudioUrl(string audioPath)
    {
        if (audioPath.StartsWith("http")) return audioPath;
        var rootUrl = Constants.API_BASE_URL.Replace("/api/v1", "");
        return $"{rootUrl.TrimEnd('/')}/{audioPath.TrimStart('/')}";
    }

    private static string GetLocaleName(string languageCode) => languageCode.ToLower() switch
    {
        "vi" => "Vietnam",
        "en" => "English",
        "ja" => "Japanese",
        "ko" => "Korean",
        "zh" => "Chinese",
        "fr" => "French",
        "de" => "German",
        "es" => "Spanish",
        _ => languageCode
    };

    private static string GetCountryCode(string languageCode) => languageCode.ToLower() switch
    {
        "vi" => "VN",
        "en" => "US",
        "ja" => "JP",
        "ko" => "KR",
        "zh" => "CN",
        "fr" => "FR",
        "de" => "DE",
        "es" => "ES",
        _ => languageCode.ToUpper()
    };

    private async Task ShowErrorMessage(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (currentPage != null)
                await currentPage.DisplayAlertAsync("⚠️ Thông báo", message, "OK");
        });
    }
}
