using System.Text.Json;
using Microsoft.Extensions.Options;
using WebPush;

namespace WeatherWise.Api.Services;

public interface IPushNotificationService
{
    Task SaveSubscriptionAsync(PushSubscription subscription, string userId);
    Task SendNotificationAsync(string userId, string message);
    Task RemoveSubscriptionAsync(string userId);
}

public class PushNotificationService : IPushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;
    private readonly WebPushClient _webPushClient;
    private readonly Dictionary<string, PushSubscription> _subscriptions;
    private readonly VapidDetails _vapidDetails;

    public PushNotificationService(
        ILogger<PushNotificationService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _webPushClient = new WebPushClient();
        _subscriptions = new Dictionary<string, PushSubscription>();

        var vapidSubject = configuration["VapidKeys:Subject"] ?? throw new InvalidOperationException("Vapid subject is not configured");
        var vapidPublicKey = configuration["VapidKeys:PublicKey"] ?? throw new InvalidOperationException("Vapid public key is not configured");
        var vapidPrivateKey = configuration["VapidKeys:PrivateKey"] ?? throw new InvalidOperationException("Vapid private key is not configured");

        _vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
    }

    public async Task SaveSubscriptionAsync(PushSubscription subscription, string userId)
    {
        await Task.Run(() =>
        {
            _subscriptions[userId] = subscription;
            _logger.LogInformation("Push subscription saved for user {UserId}", userId);
        });
    }

    public async Task SendNotificationAsync(string userId, string message)
    {
        try
        {
            if (!_subscriptions.TryGetValue(userId, out var subscription))
            {
                _logger.LogWarning("No push subscription found for user {UserId}", userId);
                return;
            }

            var payload = JsonSerializer.Serialize(new { message });
            await _webPushClient.SendNotificationAsync(subscription, payload, _vapidDetails);
            _logger.LogInformation("Push notification sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
            if (ex is WebPushException webPushEx && webPushEx.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                await RemoveSubscriptionAsync(userId);
            }
            throw;
        }
    }

    public async Task RemoveSubscriptionAsync(string userId)
    {
        await Task.Run(() =>
        {
            _subscriptions.Remove(userId);
            _logger.LogInformation("Push subscription removed for user {UserId}", userId);
        });
    }
} 