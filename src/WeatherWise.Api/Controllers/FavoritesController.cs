using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using WeatherWise.Application.DTOs.Favorites;
using WeatherWise.Application.Services;

namespace WeatherWise.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoritesService _favoritesService;
    private readonly IWeatherService _weatherService;

    public FavoritesController(IFavoritesService favoritesService, IWeatherService weatherService)
    {
        _favoritesService = favoritesService;
        _weatherService = weatherService;
    }

    [HttpGet]
    public async Task<ActionResult<FavoritesResponseDTO>> GetFavorites()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var result = await _favoritesService.GetFavoriteLocationsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{location}")]
    public async Task<ActionResult<FavoriteResponseDTO>> AddToFavorites(string location)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            // Get location coordinates from weather service
            var searchResult = await _weatherService.SearchLocationsAsync(location);
            var locationData = searchResult.Locations.FirstOrDefault() ??
                throw new InvalidOperationException($"Location '{location}' not found");

            var result = await _favoritesService.AddToFavoritesAsync(
                userId,
                locationData.Name,
                locationData.Coordinates.Lat,
                locationData.Coordinates.Lon);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{location}")]
    public async Task<ActionResult<FavoriteResponseDTO>> RemoveFromFavorites(string location)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var result = await _favoritesService.RemoveFromFavoritesAsync(userId, location);
            if (!result.Success)
            {
                return NotFound(new { error = $"Favorite location '{location}' not found for the current user." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
} 