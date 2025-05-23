using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherWise.Application.DTOs.Weather;
using WeatherWise.Application.Services;

namespace WeatherWise.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        HttpClient httpClient,
        IConfiguration configuration,
        IFavoritesService favoritesService,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenWeatherMap:ApiKey"] ?? throw new InvalidOperationException("OpenWeatherMap API key is not configured");
        _favoritesService = favoritesService;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://api.openweathermap.org/");
    }

    public async Task<WeatherDataDTO> GetWeatherDataAsync(string location, string? userId = null)
    {
        // First, get coordinates for the location
        var searchResult = await SearchLocationsAsync(location);
        var locationData = searchResult.Locations.FirstOrDefault() ?? 
            throw new InvalidOperationException($"Location '{location}' not found");

        // Get weather data using coordinates
        var weatherData = await GetWeatherDataByCoordinatesAsync(locationData.Coordinates.Lat, locationData.Coordinates.Lon, userId);

        // Override the location name with the one from search to maintain consistency
        return weatherData with { Location = locationData.Name };
    }

    public async Task<WeatherDataDTO> GetWeatherDataByCoordinatesAsync(double latitude, double longitude, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Fetching weather data for coordinates: {Latitude}, {Longitude}", latitude, longitude);

            // Get current weather data
            var weatherUrl = $"data/2.5/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric";
            _logger.LogDebug("Requesting current weather data from: {Url}", weatherUrl);

            var response = await _httpClient.GetAsync(weatherUrl);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get weather data. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, error);
                throw new InvalidOperationException($"Weather API error: {response.StatusCode} - {error}");
            }

            var weatherData = await response.Content.ReadFromJsonAsync<OpenWeatherOneCallResponse>();
            if (weatherData == null)
                throw new InvalidOperationException("Failed to parse weather data response");

            // Get location name from reverse geocoding
            var geoUrl = $"geo/1.0/reverse?lat={latitude}&lon={longitude}&limit=1&appid={_apiKey}";
            _logger.LogDebug("Requesting location data from: {Url}", geoUrl);

            var geoResponse = await _httpClient.GetAsync(geoUrl);
            if (!geoResponse.IsSuccessStatusCode)
            {
                var error = await geoResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get location data. Status: {StatusCode}, Error: {Error}", 
                    geoResponse.StatusCode, error);
            }

            var reverseGeoResponse = await geoResponse.Content.ReadFromJsonAsync<OpenWeatherGeoResponse[]>();
            var locationName = reverseGeoResponse?.FirstOrDefault()?.Name ?? $"Location at ({latitude:F2}, {longitude:F2})";

            // Get forecast data
            var forecastUrl = $"data/2.5/forecast?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric";
            _logger.LogDebug("Requesting forecast data from: {Url}", forecastUrl);

            var forecastResponse = await _httpClient.GetAsync(forecastUrl);
            if (!forecastResponse.IsSuccessStatusCode)
            {
                var error = await forecastResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get forecast data. Status: {StatusCode}, Error: {Error}", 
                    forecastResponse.StatusCode, error);
                throw new InvalidOperationException($"Forecast API error: {forecastResponse.StatusCode} - {error}");
            }

            var forecastData = await forecastResponse.Content.ReadFromJsonAsync<OpenWeatherForecastResponse>();
            if (forecastData == null)
                throw new InvalidOperationException("Failed to parse forecast data response");

            var isFavorite = userId != null && await _favoritesService.IsLocationFavoriteAsync(userId, locationName);

            return new WeatherDataDTO
            {
                Location = locationName,
                Temperature = weatherData.Main.Temp,
                Condition = weatherData.Weather[0].Description,
                FeelsLike = weatherData.Main.FeelsLike,
                Humidity = weatherData.Main.Humidity,
                WindSpeed = weatherData.Wind.Speed,
                UvIndex = 0, // Not available in the free tier
                Pressure = weatherData.Main.Pressure,
                Timezone = weatherData.Timezone.ToString(),
                LocalTime = DateTimeOffset.FromUnixTimeSeconds(weatherData.Dt)
                    .ToOffset(TimeSpan.FromSeconds(weatherData.Timezone))
                    .ToString("O"),
                MapLocation = new CoordinatesDTO(latitude, longitude),
                HourlyForecasts = forecastData.List.Take(24).Select(h => new HourlyForecastDTO
                {
                    Time = DateTimeOffset.FromUnixTimeSeconds(h.Dt)
                        .ToOffset(TimeSpan.FromSeconds(weatherData.Timezone))
                        .ToString("O"),
                    Temperature = h.Main.Temp,
                    Condition = h.Weather[0].Description,
                    Precipitation = h.Pop * 100 // Convert probability to percentage
                }),
                DailyForecasts = GetDailyForecasts(forecastData.List, weatherData.Timezone),
                IsFavorite = isFavorite
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weather data for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            throw new InvalidOperationException($"Failed to get weather data: {ex.Message}", ex);
        }
    }

    private IEnumerable<DailyForecastDTO> GetDailyForecasts(List<ForecastItem> forecasts, int timezoneOffset)
    {
        return forecasts
            .GroupBy(f => DateTimeOffset.FromUnixTimeSeconds(f.Dt)
                .ToOffset(TimeSpan.FromSeconds(timezoneOffset))
                .Date)
            .Take(7)
            .Select(g =>
            {
                var dayForecasts = g.ToList();
                return new DailyForecastDTO
                {
                    Day = g.Key.ToString("dddd"),
                    Date = g.Key.ToString("O"),
                    HighTemp = dayForecasts.Max(f => f.Main.TempMax),
                    LowTemp = dayForecasts.Min(f => f.Main.TempMin),
                    Condition = dayForecasts[dayForecasts.Count / 2].Weather[0].Description,
                    Precipitation = dayForecasts.Average(f => f.Pop) * 100
                };
            });
    }

    public async Task<LocationSearchResultDTO> SearchLocationsAsync(string query)
    {
        var response = await _httpClient.GetFromJsonAsync<OpenWeatherGeoResponse[]>(
            $"geo/1.0/direct?q={Uri.EscapeDataString(query)}&limit=5&appid={_apiKey}");

        if (response == null)
            return new LocationSearchResultDTO { Locations = Array.Empty<LocationDTO>() };

        return new LocationSearchResultDTO
        {
            Locations = response.Select(l => new LocationDTO
            {
                Name = $"{l.Name}, {l.State ?? ""}, {l.Country}".TrimStart(' ', ','),
                Country = l.Country,
                Coordinates = new CoordinatesDTO(l.Lat, l.Lon),
                Timezone = TimeZoneInfo.GetSystemTimeZones()
                    .FirstOrDefault(tz => tz.BaseUtcOffset == TimeSpan.FromSeconds(l.Timezone ?? 0))
                    ?.Id ?? "UTC"
            })
        };
    }

    // OpenWeatherMap API response models
    private class OpenWeatherOneCallResponse
    {
        public MainData Main { get; set; } = null!;
        public WindData Wind { get; set; } = null!;
        public Weather[] Weather { get; set; } = null!;
        public long Dt { get; set; }
        public int Timezone { get; set; }
    }

    private class OpenWeatherForecastResponse
    {
        public List<ForecastItem> List { get; set; } = null!;
    }

    private class ForecastItem
    {
        public long Dt { get; set; }
        public MainData Main { get; set; } = null!;
        public Weather[] Weather { get; set; } = null!;
        public double Pop { get; set; }
    }

    private class MainData
    {
        public double Temp { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }
    }

    private class WindData
    {
        public double Speed { get; set; }
    }

    private class Weather
    {
        public string Description { get; set; } = null!;
    }

    private class OpenWeatherGeoResponse
    {
        public string Name { get; set; } = null!;
        public string? State { get; set; }
        public string Country { get; set; } = null!;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int? Timezone { get; set; }
    }
} 