using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeitKnessetDisplay
{
    public class SefariaService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly Dictionary<string, string> _cache = new();
        private DateTime _cacheDate = DateTime.MinValue;

        public async Task LoadAsync()
        {
            // cache יומי
            if (_cacheDate.Date == DateTime.Today && _cache.Count > 0) return;

            try
            {
                var url = "https://www.sefaria.org/api/calendars?diaspora=0";
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                _cache.Clear();
                if (doc.RootElement.TryGetProperty("calendar_items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        string? title = item.TryGetProperty("title", out var t) &&
                                        t.TryGetProperty("en", out var en) ? en.GetString() : null;
                        string? heRef = item.TryGetProperty("displayValue", out var dv) &&
                                        dv.TryGetProperty("he", out var he) ? he.GetString() : null;
                        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(heRef))
                        {
                            if (_cache.ContainsKey(title!))
                                _cache[title!] = _cache[title!] + " · " + heRef;
                            else
                                _cache[title!] = heRef!;
                        }
                    }
                }
                _cacheDate = DateTime.Today;
            }
            catch
            {
                // נכשל — נשתמש ב-fallback
            }
        }

        public string Get(string sefariaKey, string fallback)
        {
            return _cache.TryGetValue(sefariaKey, out var v) && !string.IsNullOrWhiteSpace(v)
                ? v : fallback;
        }
        public async Task<string> GetRashiOnParshaAsync()
        {
            // מביא את פירוש רש"י על פסוק א' של הפרשה השבועית
            var url = "https://www.sefaria.org/api/texts/Rashi_on_Genesis.1.1?context=0&commentary=0";
            var json = await _http.GetStringAsync(url);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("he", out var he))
            {
                if (he.ValueKind == System.Text.Json.JsonValueKind.Array && he.GetArrayLength() > 0)
                    return System.Text.RegularExpressions.Regex.Replace(he[0].GetString() ?? "", "<.*?>", "");
                if (he.ValueKind == System.Text.Json.JsonValueKind.String)
                    return System.Text.RegularExpressions.Regex.Replace(he.GetString() ?? "", "<.*?>", "");
            }
            return "—";
        }

    }
}
