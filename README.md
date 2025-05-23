# WeatherWise API

WeatherWise API is a robust weather information service that provides weather data, location-based services, and user management features. Built with ASP.NET Core, it implements modern security practices, health monitoring, comprehensive logging, and Progressive Web App (PWA) support.

## 🌟 Features

### Progressive Web App (PWA) Support
- Push notifications for weather alerts
- Offline functionality with caching
- Background sync capabilities
- Service worker support
- Cache-first strategy for static resources
- VAPID-based web push notifications
- Proper cache headers and ETag support

### Weather Services
- Real-time weather data retrieval using OpenWeatherMap API
- Location-based weather forecasts
- Static map generation using LocationIQ
- Fallback mechanisms for map services

### User Management
- Secure user authentication using JWT tokens
- Role-based authorization (Admin/User roles)
- Password policy enforcement
- Account lockout protection
- Email uniqueness validation

### Security Features
- JWT-based authentication with enhanced security
- HTTPS enforcement
- XSS protection
- CSRF prevention
- Content Security Policy (CSP)
- Secure headers implementation
- CORS policy configuration
- HSTS support
- Protection against clickjacking

### Monitoring and Logging
- Health check endpoints for system monitoring
- Database health monitoring
- Structured logging with multiple providers
- JSON-formatted logs with timestamps
- Different log levels for various components
- Performance monitoring
- Error tracking and reporting

### Data Management
- SQLite database integration
- Entity Framework Core for data access
- Automatic database migrations
- Data seeding for initial setup
- Favorite locations management

## 🚀 Getting Started

### Prerequisites
- .NET 7.0 SDK or later
- SQLite

### Installation

1. Clone the repository:
```bash
git clone [repository-url]
cd weather-wise-api
```

2. Install dependencies:
```bash
dotnet restore
```

3. Update the configuration in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/weatherwise.db"
  },
  "Jwt": {
    "Key": "your-secure-key-here",
    "Issuer": "weatherwise-api",
    "Audience": "weatherwise-client",
    "ExpireDays": 7
  },
  "VapidKeys": {
    "Subject": "mailto:your-email@weatherwise.com",
    "PublicKey": "your-vapid-public-key",
    "PrivateKey": "your-vapid-private-key"
  },
  "OpenWeatherMap": {
    "ApiKey": "your-api-key-here"
  },
  "LocationIQ": {
    "ApiKey": "your-api-key-here"
  }
}
```

4. Run database migrations:
```bash
dotnet ef database update
```

5. Start the application:
```bash
dotnet run --project src/WeatherWise.Api
```

## 🔍 API Endpoints

### Push Notifications
- `POST /api/pushnotification/subscribe` - Subscribe to push notifications
- `DELETE /api/pushnotification/unsubscribe` - Unsubscribe from push notifications
- `GET /api/pushnotification/vapid-public-key` - Get VAPID public key

### Authentication
- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and receive JWT token

### Weather
- `GET /api/weather/{location}` - Get weather data by location name
- `GET /api/weather/coordinates?lat={latitude}&lon={longitude}` - Get weather data by geographic coordinates
- `GET /api/weather/search?query={searchQuery}` - Search for locations

### Maps
- `GET /api/map/static/{location}` - Get static map for location

### Favorites
- `GET /api/favorites` - Get user's favorite locations
- `POST /api/favorites` - Add favorite location
- `DELETE /api/favorites/{id}` - Remove favorite location

### Health Monitoring
- `GET /health` - Check application health status

## 🔒 Security Configuration

### JWT Settings
- Minimum key size: 256 bits
- Token expiration: Configurable (default 7 days)
- Clock skew: Zero (strict timing)

### Password Policy
- Minimum length: 12 characters
- Requires: Uppercase, lowercase, numbers, special characters
- Account lockout: 5 failed attempts (15-minute lockout)

### CORS Policy
Default configuration allows requests from:
- `http://localhost:3000` (customizable in `Program.cs`)
- Supports PWA requirements
- Allows credentials
- Exposes necessary headers for PWA

## 📱 PWA Integration Guide

### Setting Up Push Notifications

1. Get the VAPID public key:
```javascript
const response = await fetch('/api/pushnotification/vapid-public-key');
const { publicKey } = await response.json();
```

2. Subscribe to push notifications:
```javascript
// Register service worker
const registration = await navigator.serviceWorker.register('/service-worker.js');

// Subscribe to push
const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: publicKey
});

// Send subscription to server
await fetch('/api/pushnotification/subscribe', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + token
    },
    body: JSON.stringify(subscription)
});
```

### Implementing Caching
Add to your service worker:
```javascript
self.addEventListener('fetch', (event) => {
    event.respondWith(
        caches.match(event.request)
            .then(response => response || fetch(event.request))
    );
});
```

### Background Sync
```javascript
// Register for background sync
const registration = await navigator.serviceWorker.ready;
await registration.sync.register('weatherUpdate');
```

## 📝 Logging

### Log Levels
- Default: Information
- Microsoft.AspNetCore: Warning
- Microsoft.EntityFrameworkCore: Warning

### Log Formats
- JSON structured logging
- UTC timestamps
- Includes scopes and context

## 🔧 Health Monitoring

Health checks are available at `/health` and monitor:
- API availability
- Database connectivity
- System status

Response format:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database is functioning normally"
    },
    {
      "name": "self",
      "status": "Healthy",
      "description": "API is responding"
    }
  ]
}
```

## ⚙️ Configuration Options

### PWA Settings
```json
{
  "VapidKeys": {
    "Subject": "mailto:your-email@weatherwise.com",
    "PublicKey": "your-vapid-public-key",
    "PrivateKey": "your-vapid-private-key"
  }
}
```

### Security Settings
```json
{
  "Security": {
    "RequireHttps": true,
    "EnableXssProtection": true,
    "EnableHsts": true
  }
}
```

### Logging Settings
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "Console": {
      "FormatterName": "json",
      "FormatterOptions": {
        "SingleLine": true,
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss",
        "UseUtcTimestamp": true
      }
    }
  }
}
```

## 🛠️ Development

### Adding Migrations
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Running Tests
```bash
dotnet test
```

## 📈 Performance Monitoring

The application includes built-in performance monitoring:
- Request timing
- Database query performance
- API endpoint response times
- Resource usage tracking
- Cache hit/miss ratios
- Push notification delivery stats

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For support, please open an issue in the repository or contact the development team.

## 🔄 Version History

- 1.1.0
  - Added PWA support
  - Implemented push notifications
  - Enhanced caching capabilities
  - Added offline functionality
- 1.0.0
  - Initial release
  - Basic weather functionality
  - User authentication
  - Favorites management

## 🏗️ Built With

- ASP.NET Core
- Entity Framework Core
- SQLite
- OpenWeatherMap API
- LocationIQ API
- Web Push API

## 📝 Examples

### Getting Weather Data

1. By Location Name:
```bash
curl -X GET "http://localhost:5083/api/weather/London"
```

2. By Coordinates:
```bash
curl -X GET "http://localhost:5083/api/weather/coordinates?lat=51.5074&lon=-0.1278"
```

3. Search Locations:
```bash
curl -X GET "http://localhost:5083/api/weather/search?query=London"
```

Response Format:
```json
{
  "location": "London, GB",
  "temperature": 15.6,
  "condition": "partly cloudy",
  "feelsLike": 14.8,
  "humidity": 76,
  "windSpeed": 4.12,
  "uvIndex": 2.1,
  "pressure": 1015,
  "timezone": "Europe/London",
  "localTime": "2024-03-21T14:30:00+00:00",
  "mapLocation": {
    "lat": 51.5074,
    "lon": -0.1278
  },
  "hourlyForecasts": [...],
  "dailyForecasts": [...],
  "isFavorite": false
}
``` 