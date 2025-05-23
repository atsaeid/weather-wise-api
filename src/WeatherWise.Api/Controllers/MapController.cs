using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using WeatherWise.Application.DTOs.Maps;
using WeatherWise.Application.Services;

namespace WeatherWise.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class MapController : ControllerBase
{
    private readonly IMapService _mapService;

    public MapController(IMapService mapService)
    {
        _mapService = mapService;
    }

    [HttpGet("static")]
    public async Task<ActionResult<StaticMapResponseDTO>> GetStaticMap(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int? zoom = null,
        [FromQuery] int? width = null,
        [FromQuery] int? height = null)
    {
        try
        {
            var settings = new MapSettingsDTO
            {
                Latitude = latitude,
                Longitude = longitude,
                ZoomLevel = zoom ?? 12,
                Width = width ?? 400,
                Height = height ?? 300
            };

            var result = await _mapService.GetStaticMapAsync(settings);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
} 