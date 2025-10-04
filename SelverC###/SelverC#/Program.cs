using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Enter city name: ");
        string city_name = Console.ReadLine();
        Console.WriteLine("Choose one of the next 16 days including today : ");
        string day = Console.ReadLine();
        string city_lat_api = $"https://geocoding-api.open-meteo.com/v1/search?name={city_name}&count=1&language=en&format=json";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Get latitude and longitude from geocoding API
                HttpResponseMessage city_api_response = await client.GetAsync(city_lat_api);
                city_api_response.EnsureSuccessStatusCode();
                string city_api_body = await city_api_response.Content.ReadAsStringAsync();

                var geoResult = JsonConvert.DeserializeObject<GeocodingResponse>(city_api_body);

                if (geoResult?.results != null && geoResult.results.Length > 0)
                {
                    double latitude = geoResult.results[0].latitude;
                    double longitude = geoResult.results[0].longitude;

                    Console.WriteLine($"Latitude: {latitude}, Longitude: {longitude}");

                    // Build weather API URL with found coordinates (no current)
                    string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=weather_code,temperature_2m_max,temperature_2m_min,rain_sum,precipitation_sum,sunrise,sunset&forecast_days=16&timezone=auto";

                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<WeatherResponse>(responseBody);

                    if (result?.daily != null && result.daily.time != null)
                    {
                        int dayIndex;
                        if (!int.TryParse(day, out dayIndex) || dayIndex < 0 || dayIndex >= result.daily.time.Length)
                        {
                            Console.WriteLine("Invalid day selection. Please enter a number between 0 and 15.");
                        }
                        else
                        {
                            float rainAmount = result.daily.rain_sum[dayIndex];
                            string weatherStatus;

                            if (rainAmount == 0)
                            {
                                weatherStatus = GetWeatherStatus(result.daily.weather_code[dayIndex]);
                            }
                            else if (rainAmount < 2f)
                            {
                                weatherStatus = "Drizzle";
                            }
                            else if (rainAmount < 10f)
                            {
                                weatherStatus = "Rainy";
                            }
                            else
                            {
                                weatherStatus = "Heavy Rain";
                            }

                            string tempStatus = GetTemperatureStatus(result.daily.temperature_2m_max[dayIndex]);

                            Console.WriteLine($"Date: {result.daily.time[dayIndex]}");
                            Console.WriteLine($"  Max Temp: {result.daily.temperature_2m_max[dayIndex]}°C ({tempStatus})");
                            Console.WriteLine($"  Min Temp: {result.daily.temperature_2m_min[dayIndex]}°C");
                            Console.WriteLine($"  Rain: {result.daily.rain_sum[dayIndex]} mm");
                            Console.WriteLine($"  Precipitation: {result.daily.precipitation_sum[dayIndex]} mm");
                            Console.WriteLine($"  Weather: {weatherStatus}");
                            Console.WriteLine($"  Sunrise: {result.daily.sunrise[dayIndex]}");
                            Console.WriteLine($"  Sunset: {result.daily.sunset[dayIndex]}");
                            Console.WriteLine();

                            Console.WriteLine("Will it rain on my parade?");
                            if (rainAmount > 0)
                            {
                                Console.WriteLine("Yes, it will rain on your parade!");
                            }
                            else
                            {
                                Console.WriteLine("No, it will not rain on your parade!");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Daily forecast data not found.");
                    }
                }
                else
                {
                    Console.WriteLine("City not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling API: " + ex.Message);
            }
        }
    }

    static string GetWeatherStatus(int code)
    {
        // Based on Open-Meteo weather codes
        switch (code)
        {
            case 0: return "Clear";
            case 1: case 2: case 3: return "Partly Cloudy";
            case 45: case 48: return "Foggy";
            case 51: case 53: case 55: return "Drizzle";
            case 56: case 57: return "Freezing Drizzle";
            case 61: case 63: case 65: return "Rainy";
            case 66: case 67: return "Freezing Rain";
            case 71: case 73: case 75: return "Snowfall";
            case 77: return "Snow Grains";
            case 80: case 81: case 82: return "Heavy Rain";
            case 85: case 86: return "Heavy Snow";
            case 95: return "Thunderstorm";
            case 96: case 99: return "Thunderstorm with Hail";
            default: return "Unknown";
        }
    }

    static string GetTemperatureStatus(float temp)
    {
        if (temp < 5) return "Very Cold";
        if (temp < 15) return "Cold";
        if (temp < 25) return "Warm";
        if (temp < 32) return "Hot";
        return "Very Hot";
    }
}

public class GeocodingResponse
{
    public GeocodingResult[] results { get; set; }
}

public class GeocodingResult
{
    public double latitude { get; set; }
    public double longitude { get; set; }
}

public class DailyWeather
{
    public string[] time { get; set; }
    public float[] temperature_2m_max { get; set; }
    public float[] temperature_2m_min { get; set; }
    public float[] rain_sum { get; set; }
    public float[] precipitation_sum { get; set; }
    public int[] weather_code { get; set; }
    public string[] sunrise { get; set; }
    public string[] sunset { get; set; }
}

public class WeatherResponse
{
    public string timezone { get; set; }
    public DailyWeather daily { get; set; }
}