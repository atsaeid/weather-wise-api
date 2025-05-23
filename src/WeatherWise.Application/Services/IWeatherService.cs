using WeatherWise.Application.DTOs.Weather;

namespace WeatherWise.Application.Services;

public interface IWeatherService
{
    Task<WeatherDataDTO> GetWeatherDataAsync(string location, string? userId = null);
    Task<WeatherDataDTO> GetWeatherDataByCoordinatesAsync(double latitude, double longitude, string? userId = null);
    Task<LocationSearchResultDTO> SearchLocationsAsync(string query);
} 