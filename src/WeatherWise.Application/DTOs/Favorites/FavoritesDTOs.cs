namespace WeatherWise.Application.DTOs.Favorites;

public record FavoriteLocationDTO
{
    public required string Name { get; init; }
    public required string SavedAt { get; init; }
}

public record FavoritesResponseDTO
{
    public required IEnumerable<FavoriteLocationDTO> Locations { get; init; }
}

public record FavoriteResponseDTO
{
    public required bool Success { get; init; }
    public required IEnumerable<FavoriteLocationDTO> Locations { get; init; }
} 