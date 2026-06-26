using BeitKnesetBoard.Models;
using BeitKnesetDisplay.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeitKnesetBoard.Services;

public static class YahrzeitService
{
    private static readonly string CacheDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "yahrzeit");

    public static async Task<IReadOnlyList<Tzaddik>> GetTodayAsync()
    {
        try
        {
            var heb = await _hebcal.GetHebrewDateAsync(DateTime.Today);
            var cacheFile = Path.Combine(_cacheDir, $"{heb.Month}-{heb.Day}.json");

            if (File.Exists(cacheFile))
            {
                var cached = JsonSerializer.Deserialize<List<Tzaddik>>(
                    await File.ReadAllTextAsync(cacheFile));
                if (cached is { Count: > 0 }) return cached;
            }

            if (!string.IsNullOrWhiteSpace(_openAi?.ApiKey))
            {
                var list = await _openAi.GetTzaddikimAsync(heb.Month, heb.Day);
                if (list is { Count: > 0 })
                {
                    await File.WriteAllTextAsync(cacheFile, JsonSerializer.Serialize(list));
                    return list;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Yahrzeit] {ex.Message}");
        }

        // Fallback — תמיד מחזיר משהו כדי שהדף לא יהיה ריק
        return new List<Tzaddik>
    {
        new() { Name = "הבעל שם טוב", Years = "תנ\"ח – תק\"כ",
                Bio = "מייסד תנועת החסידות, רבי ישראל בן אליעזר. לימד את דרך עבודת ה' מתוך שמחה, אהבת ישראל ודבקות." },
        new() { Name = "רבי נחמן מברסלב", Years = "תקל\"ב – תקע\"א",
                Bio = "נינו של הבעש\"ט. לימד את חשיבות ההתבודדות, האמונה הפשוטה והשמחה. מחבר \"ליקוטי מוהר\"ן\"." },
        new() { Name = "האר\"י הקדוש", Years = "ש\"ד – של\"ב",
                Bio = "רבי יצחק לוריא, מגדולי המקובלים בצפת. תורתו (קבלת האר\"י) משמשת בסיס לקבלה המודרנית." },
        new() { Name = "הרמב\"ם", Years = "תתצ\"ח – תתקס\"ה",
                Bio = "רבי משה בן מימון. גדול הפוסקים והפילוסופים, מחבר \"משנה תורה\" ו\"מורה נבוכים\". רופא ומנהיג." }
    };
    }

}
