using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeitKnesetBoard.Services;

public static class HebcalDateService
{
    private static readonly HttpClient _http = new();

    public record HebDate(string Hebrew, string Month, int Day, int Year);

    public static async Task<HebDate> GetTodayHebrewAsync()
    {
        var now = DateTime.Today;
        var url = $"https://www.hebcal.com/converter?cfg=json&gy={now.Year}&gm={now.Month}&gd={now.Day}&g2h=1";
        var json = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new HebDate(
            root.GetProperty("hebrew").GetString() ?? "",
            root.GetProperty("hm").GetString() ?? "",
            root.GetProperty("hd").GetInt32(),
            root.GetProperty("hy").GetInt32()
        );
    }
}
