namespace WeatherWise.Domain.Entities;

public class FavoriteLocation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string UserId { get; set; }
    public required string LocationName { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ApplicationUser User { get; set; } = null!;
} 