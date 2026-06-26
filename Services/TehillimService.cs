using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeitKnessetDisplay.Services
{
    public class TehillimService
    {
        private static readonly HttpClient _http = new();

        public async Task<string> GetChapterTextAsync(int chapter)
        {
            try
            {
                var url = $"https://www.sefaria.org/api/texts/Psalms.{chapter}?lang=he&context=0";
                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var he = doc.RootElement.GetProperty("he");
                var sb = new System.Text.StringBuilder();
                int v = 1;
                foreach (var verse in he.EnumerateArray())
                {
                    var txt = System.Text.RegularExpressions.Regex.Replace(verse.GetString() ?? "", "<.*?>", "");
                    sb.AppendLine($"({HebrewNumber.Range(v, v)}) {txt}");
                    v++;
                }
                return sb.ToString();
            }
            catch { return "לא ניתן לטעון את פרק התהילים כעת."; }
        }
    }
}
