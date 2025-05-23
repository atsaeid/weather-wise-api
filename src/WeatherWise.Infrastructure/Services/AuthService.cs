using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WeatherWise.Application.DTOs.Auth;
using WeatherWise.Application.Services;
using WeatherWise.Domain.Entities;
using WeatherWise.Infrastructure.Data;

namespace WeatherWise.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ApplicationDbContext context)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto)
    {
        // Validate input
        if (string.IsNullOrEmpty(registerDto.Email)) throw new ArgumentException("Email is required");
        if (string.IsNullOrEmpty(registerDto.Password)) throw new ArgumentException("Password is required");
        if (string.IsNullOrEmpty(registerDto.Username)) throw new ArgumentException("Username is required");

        // Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = new ApplicationUser
        {
            UserName = registerDto.Username,
            Email = registerDto.Email,
            FirstName = registerDto.Username, // You might want to add FirstName and LastName to RegisterDTO
            LastName = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new InvalidOperationException($"Registration failed: {string.Join(", ", errors)}");
        }

        // Assign default "User" role
        await _userManager.AddToRoleAsync(user, "User");

        // Generate tokens
        var accessToken = await GenerateJwtTokenAsync(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new AuthResponseDTO
        {
            User = new UserDTO
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                Jwt = accessToken
            },
            Tokens = new TokensDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = (int)(refreshToken.ExpiresAt - DateTime.UtcNow).TotalSeconds
            }
        };
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto)
    {
        if (string.IsNullOrEmpty(loginDto.Email)) throw new ArgumentException("Email is required");
        if (string.IsNullOrEmpty(loginDto.Password)) throw new ArgumentException("Password is required");

        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            throw new InvalidOperationException("Invalid email or password");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!isPasswordValid)
        {
            throw new InvalidOperationException("Invalid email or password");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var accessToken = await GenerateJwtTokenAsync(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new AuthResponseDTO
        {
            User = new UserDTO
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                Jwt = accessToken
            },
            Tokens = new TokensDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = (int)(refreshToken.ExpiresAt - DateTime.UtcNow).TotalSeconds
            }
        };
    }

    public async Task<bool> LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // Revoke all active refresh tokens
        var activeTokens = await _context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = "Logged out";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserDTO> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var accessToken = await GenerateJwtTokenAsync(user);

        return new UserDTO
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            Jwt = accessToken
        };
    }

    public async Task<RefreshTokenResponseDTO> RefreshTokenAsync(string refreshToken)
    {
        var token = await _context.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null)
        {
            throw new InvalidOperationException("Invalid refresh token");
        }

        if (!token.IsActive)
        {
            throw new InvalidOperationException("Refresh token is expired or revoked");
        }

        // Revoke the current refresh token
        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "Refreshed";

        // Generate new tokens
        var newAccessToken = await GenerateJwtTokenAsync(token.User);
        var newRefreshToken = await GenerateRefreshTokenAsync(token.UserId);

        token.ReplacedByToken = newRefreshToken.Token;
        await _context.SaveChangesAsync();

        return new RefreshTokenResponseDTO
        {
            Tokens = new TokensDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresIn = (int)(newRefreshToken.ExpiresAt - DateTime.UtcNow).TotalSeconds
            }
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "Revoked without replacement";
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        
        var userRoles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? throw new InvalidOperationException("User email cannot be null")),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? throw new InvalidOperationException("Username cannot be null")),
            new Claim(ClaimTypes.Email, user.Email ?? throw new InvalidOperationException("User email cannot be null"))
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(30); // Access tokens expire in 30 minutes

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured"),
            _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured"),
            claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // Refresh tokens valid for 7 days
        };

        _context.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }
} 