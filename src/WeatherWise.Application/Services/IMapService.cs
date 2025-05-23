using WeatherWise.Application.DTOs.Maps;

namespace WeatherWise.Application.Services;

public interface IMapService
{
    Task<StaticMapResponseDTO> GetStaticMapAsync(MapSettingsDTO settings);
} 