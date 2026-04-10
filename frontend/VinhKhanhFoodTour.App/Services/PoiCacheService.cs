using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text.Json;
using VinhKhanhFoodTour.App.Data;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services;

/// <summary>
/// Service cache dữ liệu POI vào SQLite nội bộ điện thoại.
/// - Lần đầu có mạng → Gọi API, lưu kết quả vào SQLite.
/// - Những lần sau → Đọc SQLite ngay lập tức (0.01s), dù có mạng hay không.
/// </summary>
public class PoiCacheService
{
    private const string TABLE_POI = "CachePoi";
    private bool _initialized = false;

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        await using var conn = new SqliteConnection(Constants.DatabasePath);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {TABLE_POI} (
                Id          INTEGER PRIMARY KEY,
                Name        TEXT NOT NULL,
                Title       TEXT,
                Description TEXT,
                Latitude    REAL,
                Longitude   REAL,
                ImageUrl    TEXT,
                Status      INTEGER,
                TranslationsJson TEXT
            );";
        await cmd.ExecuteNonQueryAsync();
        _initialized = true;
        Debug.WriteLine("[PoiCache] 🗄️ SQLite table ready.");
    }

    /// <summary>
    /// Đọc danh sách POI từ SQLite. Trả về rỗng nếu chưa có cache.
    /// </summary>
    public async Task<List<Poi>> GetCachedPoisAsync()
    {
        await EnsureInitializedAsync();
        var list = new List<Poi>();
        try
        {
            await using var conn = new SqliteConnection(Constants.DatabasePath);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT Id, Name, Title, Description, Latitude, Longitude, ImageUrl, Status, TranslationsJson FROM {TABLE_POI} ORDER BY Id";
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var poi = new Poi
                {
                    Id          = reader.GetInt32(0),
                    Name        = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Title       = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Latitude    = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                    Longitude   = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                    ImageUrl    = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Status      = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                };

                if (!reader.IsDBNull(8))
                {
                    try
                    {
                        var json = reader.GetString(8);
                        poi.Translations = JsonSerializer.Deserialize<List<PoiTranslation>>(json) ?? new();
                    }
                    catch { poi.Translations = new(); }
                }

                list.Add(poi);
            }

            Debug.WriteLine($"[PoiCache] ✅ Loaded {list.Count} POIs from SQLite cache.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiCache] ❌ Error reading cache: {ex.Message}");
        }

        return list;
    }

    /// <summary>
    /// Lưu danh sách POI vào SQLite (Ghi đè toàn bộ).
    /// </summary>
    public async Task SavePoisAsync(IEnumerable<Poi> pois)
    {
        await EnsureInitializedAsync();
        try
        {
            await using var conn = new SqliteConnection(Constants.DatabasePath);
            await conn.OpenAsync();

            // Xóa data cũ và ghi lại toàn bộ
            await using (var clearCmd = conn.CreateCommand())
            {
                clearCmd.CommandText = $"DELETE FROM {TABLE_POI}";
                await clearCmd.ExecuteNonQueryAsync();
            }

            foreach (var poi in pois)
            {
                await using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = $@"
                    INSERT INTO {TABLE_POI} (Id, Name, Title, Description, Latitude, Longitude, ImageUrl, Status, TranslationsJson)
                    VALUES (@Id, @Name, @Title, @Description, @Latitude, @Longitude, @ImageUrl, @Status, @TranslationsJson)";

                insertCmd.Parameters.AddWithValue("@Id",          poi.Id);
                insertCmd.Parameters.AddWithValue("@Name",        poi.Name);
                insertCmd.Parameters.AddWithValue("@Title",       (object?)poi.Title ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Description", (object?)poi.Description ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Latitude",    poi.Latitude);
                insertCmd.Parameters.AddWithValue("@Longitude",   poi.Longitude);
                insertCmd.Parameters.AddWithValue("@ImageUrl",    (object?)poi.ImageUrl ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Status",      poi.Status);
                insertCmd.Parameters.AddWithValue("@TranslationsJson",
                    poi.Translations?.Count > 0
                        ? JsonSerializer.Serialize(poi.Translations)
                        : DBNull.Value);

                await insertCmd.ExecuteNonQueryAsync();
            }

            Debug.WriteLine($"[PoiCache] 💾 Saved {pois.Count()} POIs to SQLite cache.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PoiCache] ❌ Error saving cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Kiểm tra xem cache có dữ liệu chưa.
    /// </summary>
    public async Task<bool> HasCacheAsync()
    {
        await EnsureInitializedAsync();
        try
        {
            await using var conn = new SqliteConnection(Constants.DatabasePath);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {TABLE_POI}";
            var count = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }
        catch { return false; }
    }
}
