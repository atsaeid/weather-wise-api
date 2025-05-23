namespace WeatherWise.Application.DTOs.Weather;

public record CoordinatesDTO(double Lat, double Lon);

public record HourlyForecastDTO
{
    public required string Time { get; init; }
    public required double Temperature { get; init; }
    public required string Condition { get; init; }
    public required double Precipitation { get; init; }
}

public record DailyForecastDTO
{
    public required string Day { get; init; }
    public required string Date { get; init; }
    public required double HighTemp { get; init; }
    public required double LowTemp { get; init; }
    public required string Condition { get; init; }
    public required double Precipitation { get; init; }
}

public record WeatherDataDTO
{
    public required string Location { get; init; }
    public required double Temperature { get; init; }
    public required string Condition { get; init; }
    public required double FeelsLike { get; init; }
    public required double Humidity { get; init; }
    public required double WindSpeed { get; init; }
    public required double UvIndex { get; init; }
    public required double Pressure { get; init; }
    public required string Timezone { get; init; }
    public required string LocalTime { get; init; }
    public required CoordinatesDTO MapLocation { get; init; }
    public required IEnumerable<HourlyForecastDTO> HourlyForecasts { get; init; }
    public required IEnumerable<DailyForecastDTO> DailyForecasts { get; init; }
    public required bool IsFavorite { get; init; }
}

public record LocationDTO
{
    public required string Name { get; init; }
    public required string Country { get; init; }
    public required CoordinatesDTO Coordinates { get; init; }
    public required string Timezone { get; init; }
}

public record LocationSearchResultDTO
{
    public required IEnumerable<LocationDTO> Locations { get; init; }
} 