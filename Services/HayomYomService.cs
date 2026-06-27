using System;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BeitKnessetDisplay.Services
{
    public static class HayomYomService
    {
        private static readonly HttpClient _http = new();

        public static async Task<string> GetTodayQuoteAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tdate = today.ToString("M/d/yyyy", CultureInfo.InvariantCulture);

                var sourceUrl = $"https://he.chabad.org/dailystudy/hayomyom.asp?tdate={tdate}";
                var url = "https://r.jina.ai/http://" + sourceUrl;

                var markdown = await _http.GetStringAsync(url);

                var quote = ExtractQuote(markdown);

                return string.IsNullOrWhiteSpace(quote) ? "" : quote;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HayomYom] {ex.Message}");
                return "";
            }
        }

        private static string ExtractQuote(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "";

            var lines = markdown
                .Replace("\r", "")
                .Split('\n');

            var startIndex = -1;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = CleanLine(lines[i]);

                if (line.StartsWith("**שיעורים:**") || line.StartsWith("שיעורים:"))
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex < 0)
                return "";

            var result = "";

            for (var i = startIndex; i < lines.Length; i++)
            {
                var line = CleanLine(lines[i]);

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Contains("אודות הספר") ||
                    line.Contains("HaYom Yom") ||
                    line.Contains("לרכישת הספר") ||
                    line.StartsWith("שבת ") ||
                    line.StartsWith("ראשון ") ||
                    line.StartsWith("שני ") ||
                    line.StartsWith("שלישי ") ||
                    line.StartsWith("רביעי ") ||
                    line.StartsWith("חמישי ") ||
                    line.StartsWith("שישי "))
                {
                    if (result.Length > 80)
                        break;
                }

                result += line + " ";
            }

            result = Regex.Replace(result, @"\s+", " ").Trim();

            if (result.Length > 520)
                result = result.Substring(0, 520).Trim() + "…";

            return result;
        }

        private static string CleanLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return "";

            line = Regex.Replace(line, @"!\[[^\]]*\]\([^)]+\)", "");
            line = Regex.Replace(line, @"\[(.*?)\]\([^)]+\)", "$1");
            line = line.Replace("**", "");
            line = Regex.Replace(line, @"\s+", " ").Trim();

            return line;
        }
    }
}
