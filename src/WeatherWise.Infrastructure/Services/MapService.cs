using System.Net.Http;
using Microsoft.Extensions.Configuration;
using WeatherWise.Application.DTOs.Maps;
using WeatherWise.Application.Services;

namespace WeatherWise.Infrastructure.Services;

public class MapService : IMapService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _defaultMapPath;

    public MapService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["LocationIQ:ApiKey"] ?? throw new InvalidOperationException("LocationIQ API key is not configured");
        _defaultMapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "default-map.png");
        
        // Ensure the Resources directory exists
        var resourcesDir = Path.GetDirectoryName(_defaultMapPath);
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir!);
        }

        // Create a simple default map if it doesn't exist
        if (!File.Exists(_defaultMapPath))
        {
            CreateDefaultMap(_defaultMapPath);
        }
    }

    private void CreateDefaultMap(string path)
    {
        // Create a simple 400x300 blank map image
        using var bitmap = new System.Drawing.Bitmap(400, 300);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        
        // Fill with light gray background
        graphics.Clear(System.Drawing.Color.FromArgb(240, 240, 240));
        
        // Draw a grid pattern
        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 200));
        for (int x = 0; x < 400; x += 20)
        {
            graphics.DrawLine(pen, x, 0, x, 300);
        }
        for (int y = 0; y < 300; y += 20)
        {
            graphics.DrawLine(pen, 0, y, 400, y);
        }

        // Add text
        using var font = new System.Drawing.Font("Arial", 12);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Gray);
        var text = "Map Unavailable";
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(text, font, brush, 
            (400 - size.Width) / 2, 
            (300 - size.Height) / 2);

        // Save the image
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    public async Task<StaticMapResponseDTO> GetStaticMapAsync(MapSettingsDTO settings)
    {
        try
        {
            var url = $"https://maps.locationiq.com/v3/staticmap?" +
                     $"key={_apiKey}&" +
                     $"center={settings.Latitude},{settings.Longitude}&" +
                     $"zoom={settings.ZoomLevel}&" +
                     $"size={settings.Width}x{settings.Height}&" +
                     $"format=png&" +
                     $"markers=icon:small-red-cutout|{settings.Latitude},{settings.Longitude}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return new StaticMapResponseDTO
                {
                    Base64Image = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(_defaultMapPath)),
                    IsDefaultMap = true
                };
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            return new StaticMapResponseDTO
            {
                Base64Image = "data:image/png;base64," + Convert.ToBase64String(imageBytes),
                IsDefaultMap = false
            };
        }
        catch (Exception)
        {
            return new StaticMapResponseDTO
            {
                Base64Image = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(_defaultMapPath)),
                IsDefaultMap = true
            };
        }
    }
} 