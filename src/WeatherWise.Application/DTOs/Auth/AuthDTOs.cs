namespace WeatherWise.Application.DTOs.Auth;

public record LoginDTO(string Email, string Password);

public record RegisterDTO(string Username, string Email, string Password);

public record RefreshTokenDTO(string RefreshToken);

public record UserDTO
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Jwt { get; init; }
}

public record TokensDTO
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required int ExpiresIn { get; init; }
}

public record AuthResponseDTO
{
    public required UserDTO User { get; init; }
    public required TokensDTO Tokens { get; init; }
}

public record RefreshTokenResponseDTO
{
    public required TokensDTO Tokens { get; init; }
} 