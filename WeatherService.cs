using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class WeatherService
{
    private static readonly HttpClient client = new HttpClient();

    // קואורדינטות לדוגמה (תל אביב). ניתן לשנות לפי מיקום בית הכנסת
    private const double Latitude = 32.0853;
    private const double Longitude = 34.7818;

    public async Task<(double Temperature, string Condition)> GetCurrentWeatherAsync()
    {
        try
        {
            string url = $"https://open-meteo.com{Latitude}&longitude={Longitude}&current_weather=true";
            string response = await client.GetStringAsync(url);

            using (JsonDocument doc = JsonDocument.Parse(response))
            {
                var current = doc.RootElement.GetProperty("current_weather");
                double temp = current.GetProperty("temperature").GetDouble();
                int weatherCode = current.GetProperty("weathercode").GetInt32();

                string condition = MapWeatherCodeToHebrew(weatherCode);
                return (temp, condition);
            }
        }
        catch
        {
            // במקרה של שגיאה או חוסר באינטרנט (מצב אופליין)
            return (22.0, "בהיר");
        }
    }

    private string MapWeatherCodeToHebrew(int code)
    {
        return code switch
        {
            0 => "בהיר",
            1 or 2 or 3 => "מעונן חלקית",
            45 or 48 => "ערפילי",
            51 or 53 or 55 => "טפטוף",
            61 or 63 or 65 => "גשום",
            71 or 73 or 75 => "שלג",
            80 or 81 or 82 => "מטרים של גשם",
            95 or 96 or 99 => "סופות רעמים",
            _ => "בהיר"
        };
    }
}
