using Microsoft.EntityFrameworkCore;
using WeatherWise.Application.DTOs.Favorites;
using WeatherWise.Application.Services;
using WeatherWise.Domain.Entities;
using WeatherWise.Infrastructure.Data;

namespace WeatherWise.Infrastructure.Services;

public class FavoritesService : IFavoritesService
{
    private readonly ApplicationDbContext _context;

    public FavoritesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FavoritesResponseDTO> GetFavoriteLocationsAsync(string userId)
    {
        var favorites = await _context.FavoriteLocations
            .Where(fl => fl.UserId == userId)
            .OrderByDescending(fl => fl.SavedAt)
            .Select(fl => new FavoriteLocationDTO
            {
                Name = fl.LocationName,
                SavedAt = fl.SavedAt.ToString("O")
            })
            .ToListAsync();

        return new FavoritesResponseDTO
        {
            Locations = favorites
        };
    }

    public async Task<FavoriteResponseDTO> AddToFavoritesAsync(string userId, string location, double latitude, double longitude)
    {
        var existingFavorite = await _context.FavoriteLocations
            .FirstOrDefaultAsync(fl => fl.UserId == userId && fl.LocationName == location);

        if (existingFavorite == null)
        {
            var favorite = new FavoriteLocation
            {
                UserId = userId,
                LocationName = location,
                Latitude = latitude,
                Longitude = longitude
            };

            _context.FavoriteLocations.Add(favorite);
            await _context.SaveChangesAsync();
        }

        var favorites = await GetFavoriteLocationsAsync(userId);
        return new FavoriteResponseDTO
        {
            Success = true,
            Locations = favorites.Locations
        };
    }

    public async Task<FavoriteResponseDTO> RemoveFromFavoritesAsync(string userId, string location)
    {
        var favorite = await _context.FavoriteLocations
            .FirstOrDefaultAsync(fl => fl.UserId == userId && fl.LocationName == location);

        if (favorite != null)
        {
            _context.FavoriteLocations.Remove(favorite);
            await _context.SaveChangesAsync();
            
            var favorites = await GetFavoriteLocationsAsync(userId);
            return new FavoriteResponseDTO
            {
                Success = true,
                Locations = favorites.Locations
            };
        }

        return new FavoriteResponseDTO
        {
            Success = false,
            Locations = (await GetFavoriteLocationsAsync(userId)).Locations
        };
    }

    public async Task<bool> IsLocationFavoriteAsync(string userId, string location)
    {
        return await _context.FavoriteLocations
            .AnyAsync(fl => fl.UserId == userId && fl.LocationName == location);
    }
} 