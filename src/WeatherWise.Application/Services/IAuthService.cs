using WeatherWise.Application.DTOs.Auth;
using WeatherWise.Domain.Entities;

namespace WeatherWise.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto);
    Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto);
    Task<bool> LogoutAsync(string userId);
    Task<UserDTO> GetCurrentUserAsync(string userId);
    Task<RefreshTokenResponseDTO> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
} 