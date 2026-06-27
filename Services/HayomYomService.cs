using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace BeitKnessetDisplay.Services
{
    public static class HayomYomService
    {
        private static readonly HttpClient _http = new();
        private static readonly SemaphoreSlim _gate = new(1, 1);

        private static string? _memoryDate;
        private static string? _memoryQuote;
        private static DateTime _nextAllowedRequestUtc = DateTime.MinValue;

        private static string TodayKey => DateTime.Now.ToString("yyyy-MM-dd");

        private static string CacheFile
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BeitKnesetBoard", "Cache");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "hayom-yom.json");
            }
        }

        public static async Task<string> GetTodayQuoteAsync()
        {
            var today = TodayKey;

            if (_memoryDate == today && !string.IsNullOrWhiteSpace(_memoryQuote))
                return _memoryQuote!;

            var cached = await ReadCacheAsync();
            if (cached?.Date == today && !string.IsNullOrWhiteSpace(cached.Quote))
            {
                _memoryDate = cached.Date;
                _memoryQuote = cached.Quote;
                return cached.Quote!;
            }

            if (DateTime.UtcNow < _nextAllowedRequestUtc)
                return "";

            await _gate.WaitAsync();
            try
            {
                if (_memoryDate == today && !string.IsNullOrWhiteSpace(_memoryQuote))
                    return _memoryQuote!;

                var tdate = DateTime.Now.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
                var url = $"https://r.jina.ai/https://he.chabad.org/dailystudy/hayomyom.asp?tdate={tdate}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 BeitKnesetBoard/1.0");
                request.Headers.Accept.ParseAdd("text/plain");

                using var response = await _http.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _nextAllowedRequestUtc = DateTime.UtcNow.AddMinutes(30);
                    System.Diagnostics.Debug.WriteLine("[HayomYom] 429 from jina.");
                    return "";
                }

                if (!response.IsSuccessStatusCode)
                {
                    _nextAllowedRequestUtc = DateTime.UtcNow.AddMinutes(15);
                    System.Diagnostics.Debug.WriteLine($"[HayomYom] HTTP {(int)response.StatusCode}");
                    return "";
                }

                var markdown = await response.Content.ReadAsStringAsync();
                var quote = ExtractQuote(markdown);

                if (string.IsNullOrWhiteSpace(quote))
                {
                    _nextAllowedRequestUtc = DateTime.UtcNow.AddMinutes(30);
                    System.Diagnostics.Debug.WriteLine("[HayomYom] empty quote after extraction.");
                    return "";
                }

                _memoryDate = today;
                _memoryQuote = quote;
                await WriteCacheAsync(new HayomYomCache { Date = today, Quote = quote });
                return quote;
            }
            catch (Exception ex)
            {
                _nextAllowedRequestUtc = DateTime.UtcNow.AddMinutes(15);
                System.Diagnostics.Debug.WriteLine($"[HayomYom] {ex.Message}");
                return "";
            }
            finally
            {
                _gate.Release();
            }
        }

        private static string ExtractQuote(string md)
        {
            if (string.IsNullOrWhiteSpace(md))
                return "";

            var text = md.Replace("\r\n", "\n").Replace("\r", "\n");

            var marker = "**שיעורים:**";
            var start = text.IndexOf(marker, StringComparison.Ordinal);

            if (start < 0)
                start = text.IndexOf("שיעורים:", StringComparison.Ordinal);

            if (start < 0)
            {
                System.Diagnostics.Debug.WriteLine("[HayomYom] שיעורים marker not found.");
                return "";
            }

            var slice = text.Substring(start + marker.Length);
            var lines = slice.Split('\n');

            var quoteLines = new List<string>();
            var startedQuote = false;

            foreach (var raw in lines)
            {
                var line = CleanLine(raw);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!startedQuote)
                {
                    if (IsDailyStudyLine(line))
                        continue;

                    startedQuote = true;
                }

                if (IsEndOfHayomYom(line))
                    break;

                if (ShouldSkipHayomYomLine(line))
                    continue;

                quoteLines.Add(line);

                if (string.Join(" ", quoteLines).Length > 1200)
                    break;
            }

            var result = string.Join(Environment.NewLine + Environment.NewLine, quoteLines).Trim();

            if (result.Length > 1300)
                result = result.Substring(0, 1300) + "…";

            return result;
        }

        private static bool IsDailyStudyLine(string line)
        {
            return line.StartsWith("שיעורים:", StringComparison.Ordinal)
                || line.StartsWith("חומש:", StringComparison.Ordinal)
                || line.StartsWith("תהילים:", StringComparison.Ordinal)
                || line.StartsWith("תניא:", StringComparison.Ordinal)
                || line.StartsWith("רמב", StringComparison.Ordinal);
        }

        private static bool IsEndOfHayomYom(string line)
        {
            if (line.StartsWith("אודות הספר", StringComparison.Ordinal))
                return true;

            if (line.StartsWith("שיעורי לימוד יומיים", StringComparison.Ordinal))
                return true;

            if (line.StartsWith("חומש עם רש", StringComparison.Ordinal))
                return true;

            // לדוגמה: שבת י״ב תמוז ה׳תשפ״ו / 27 יוני 2026
            if (Regex.IsMatch(line, @"^(ראשון|שני|שלישי|רביעי|חמישי|שישי|שבת)\s") &&
                line.Contains("/") &&
                Regex.IsMatch(line, @"\d{4}"))
            {
                return true;
            }

            return false;
        }

        private static bool ShouldSkipHayomYomLine(string line)
        {
            if (line.Contains("chabad.org", StringComparison.OrdinalIgnoreCase))
                return true;

            if (line.StartsWith("#", StringComparison.Ordinal))
                return true;

            if (line.StartsWith("===", StringComparison.Ordinal) ||
                line.StartsWith("---", StringComparison.Ordinal))
                return true;

            if (line == "היום")
                return true;

            return false;
        }


        private static string CleanLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return "";
            line = Regex.Replace(line, @"!\[[^\]]*\]\([^)]+\)", "");
            line = Regex.Replace(line, @"\[(.*?)\]\([^)]+\)", "$1");
            line = line.Replace("**", "").Replace("__", "");
            line = WebUtility.HtmlDecode(line);
            line = Regex.Replace(line, @"\s+", " ").Trim();
            return line;
        }

        private static async Task<HayomYomCache?> ReadCacheAsync()
        {
            try
            {
                if (!File.Exists(CacheFile)) return null;
                var json = await File.ReadAllTextAsync(CacheFile);
                return JsonSerializer.Deserialize<HayomYomCache>(json);
            }
            catch { return null; }
        }

        private static async Task WriteCacheAsync(HayomYomCache cache)
        {
            try
            {
                var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(CacheFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HayomYom Cache] {ex.Message}");
            }
        }

        private sealed class HayomYomCache
        {
            public string? Date { get; set; }
            public string? Quote { get; set; }
        }
    }
}
