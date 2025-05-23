using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPush;
using WeatherWise.Api.Services;

namespace WeatherWise.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PushNotificationController : ControllerBase
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<PushNotificationController> _logger;

    public PushNotificationController(
        IPushNotificationService pushNotificationService,
        ILogger<PushNotificationController> logger)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscription subscription)
    {
        try
        {
            var userId = User.Identity?.Name ?? throw new InvalidOperationException("User not found");
            await _pushNotificationService.SaveSubscriptionAsync(subscription, userId);
            return Ok(new { message = "Successfully subscribed to push notifications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to push notifications");
            return StatusCode(500, new { message = "Failed to subscribe to push notifications" });
        }
    }

    [HttpDelete("unsubscribe")]
    public async Task<IActionResult> Unsubscribe()
    {
        try
        {
            var userId = User.Identity?.Name ?? throw new InvalidOperationException("User not found");
            await _pushNotificationService.RemoveSubscriptionAsync(userId);
            return Ok(new { message = "Successfully unsubscribed from push notifications" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from push notifications");
            return StatusCode(500, new { message = "Failed to unsubscribe from push notifications" });
        }
    }

    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("VapidKeys:PublicKey");

        if (string.IsNullOrEmpty(publicKey))
        {
            return NotFound(new { message = "VAPID public key not configured" });
        }

        return Ok(new { publicKey });
    }
} 