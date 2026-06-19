using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BeitKnesetBoard.Models;

namespace BeitKnesetBoard.Services;

public static class YahrzeitService
{
    private static readonly string CacheDir =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "yahrzeit");

    public static async Task<YahrzeitDay> GetTodayAsync()
    {
        Directory.CreateDirectory(CacheDir);
        var heb = await HebcalDateService.GetTodayHebrewAsync();
        var cacheFile = Path.Combine(CacheDir, $"{heb.Month}-{heb.Day}.json");

        if (File.Exists(cacheFile))
        {
            try { return JsonSerializer.Deserialize<YahrzeitDay>(File.ReadAllText(cacheFile))!; }
            catch { /* fall through and refetch */ }
        }

        var system = "אתה היסטוריון יהודי. החזר תשובה ב-JSON בלבד.";
        var user = $@"תן לי 3-4 צדיקים מפורסמים שיום ההילולא (יום השנה לפטירה) שלהם הוא ב-{heb.Day} ב{heb.Month} (תאריך עברי).
                    החזר JSON בפורמט הזה בדיוק:
                    {{ ""hebDate"": ""{heb.Hebrew}"", ""tzaddikim"": [ {{ ""name"": ""..."", ""years"": ""תק..-תר.."", ""bio"": ""2-3 משפטים בעברית"" }} ] }}";

        try
        {
            var content = await OpenAiClient.AskJsonAsync(system, user);
            var day = JsonSerializer.Deserialize<YahrzeitDay>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            day.HebDate = heb.Hebrew;
            File.WriteAllText(cacheFile, JsonSerializer.Serialize(day,
                new JsonSerializerOptions { WriteIndented = true }));
            return day;
        }
        catch
        {
            return new YahrzeitDay { HebDate = heb.Hebrew };
        }
    }
}
