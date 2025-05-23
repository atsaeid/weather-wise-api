namespace WeatherWise.Application.DTOs.Maps;

public record MapSettingsDTO
{
    public int ZoomLevel { get; init; } = 12;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int Width { get; init; } = 400;
    public int Height { get; init; } = 300;
}

public record StaticMapResponseDTO
{
    public required string Base64Image { get; init; }
    public required bool IsDefaultMap { get; init; }
} 