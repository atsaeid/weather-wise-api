using WeatherWise.Application.DTOs.Favorites;

namespace WeatherWise.Application.Services;

public interface IFavoritesService
{
    Task<FavoritesResponseDTO> GetFavoriteLocationsAsync(string userId);
    Task<FavoriteResponseDTO> AddToFavoritesAsync(string userId, string location, double latitude, double longitude);
    Task<FavoriteResponseDTO> RemoveFromFavoritesAsync(string userId, string location);
    Task<bool> IsLocationFavoriteAsync(string userId, string location);
} 