using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using WeatherWise.Application.DTOs.Weather;
using WeatherWise.Application.Services;

namespace WeatherWise.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors("AllowAll")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("{location}")]
    public async Task<ActionResult<WeatherDataDTO>> GetWeatherData(string location)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _weatherService.GetWeatherDataAsync(location, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("coordinates")]
    public async Task<ActionResult<WeatherDataDTO>> GetWeatherDataByCoordinates([FromQuery] double lat, [FromQuery] double lon)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _weatherService.GetWeatherDataByCoordinatesAsync(lat, lon, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<LocationSearchResultDTO>> SearchLocations([FromQuery] string query)
    {
        try
        {
            var result = await _weatherService.SearchLocationsAsync(query);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
} 