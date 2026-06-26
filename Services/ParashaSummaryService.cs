using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeitKnesset.Services
{
    public class ParshaInfo
    {
        public string Name { get; set; } = "";
        public string Summary { get; set; } = "";
    }

    public class ParashaSummaryService
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public async Task<ParshaInfo> GetWeeklyParshaAsync()
        {
            var info = new ParshaInfo();
            try
            {
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var url = $"https://www.sefaria.org/api/calendars?year={DateTime.Now.Year}&month={DateTime.Now.Month}&day={DateTime.Now.Day}";
                var json = await _http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("calendar_items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("title", out var title) &&
                            title.TryGetProperty("he", out var he) &&
                            item.TryGetProperty("displayValue", out var disp) &&
                            disp.TryGetProperty("he", out var heVal) &&
                            title.GetProperty("en").GetString() == "Parashat Hashavua")
                        {
                            info.Name = heVal.GetString() ?? "";
                            break;
                        }
                    }
                }

                info.Summary = "פרשת " + info.Name + " — לחצו לסיכום מלא בעלון השבועי.";
            }
            catch
            {
                info.Name = "";
                info.Summary = "";
            }
            return info;
        }
        public async Task<(string verse, string rashi)> GetFirstVerseWithRashiAsync(string parashaEnglishRef)
        {
            // parashaEnglishRef = למשל "Genesis.1.1"
            var http = new HttpClient();
            var verseJson = await http.GetStringAsync($"https://www.sefaria.org/api/texts/{parashaEnglishRef}?lang=he");
            var rashiJson = await http.GetStringAsync($"https://www.sefaria.org/api/texts/Rashi_on_{parashaEnglishRef}?lang=he");
            using var v = JsonDocument.Parse(verseJson);
            using var r = JsonDocument.Parse(rashiJson);
            var verse = v.RootElement.GetProperty("he").GetString() ?? "";
            var rashi = r.RootElement.GetProperty("he").ValueKind == JsonValueKind.Array
                ? string.Join(" ", r.RootElement.GetProperty("he").EnumerateArray().Select(x => x.GetString()))
                : r.RootElement.GetProperty("he").GetString() ?? "";
            var clean = System.Text.RegularExpressions.Regex.Replace(rashi, "<.*?>", "");
            return (verse, clean);
        }


    }
}
