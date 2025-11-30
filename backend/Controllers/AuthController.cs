using System.Security.Claims;
using backend.Database;
using backend.DTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IJwtService jwt, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
            return BadRequest("Username or email already in use.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            UserRole = Role.User
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();

        // Store refresh token in DB only
        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        // Set HttpOnly refresh token cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // ensure HTTPS, your nginx uses SSL
            SameSite = SameSiteMode.None, // if frontend is on different origin; otherwise Lax/Strict as appropriate
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/api/auth/refresh" // restrict path
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

        var expiresMinutes = double.Parse(_config["Jwt:AccessTokenMinutes"] ?? "60");
        return Ok(new AuthResponseDto(accessToken, null, DateTime.UtcNow.AddMinutes(expiresMinutes)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        // read refresh token from cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest("Refresh token required.");

        var token = await _db.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            return Unauthorized("Invalid refresh token.");

        token.IsRevoked = true; // revoke old refresh token

        var newAccess = _jwt.GenerateAccessToken(token.User);
        var newRefresh = _jwt.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefresh,
            UserId = token.User.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        // rotate cookie with new refresh token
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7),
            Path = "/api/auth/refresh"
        };
        Response.Cookies.Delete("refreshToken"); // remove old cookie first
        Response.Cookies.Append("refreshToken", newRefresh, cookieOptions);

        await _db.SaveChangesAsync();

        var expiresMinutes = double.Parse(_config["Jwt:AccessTokenMinutes"] ?? "60");
        return Ok(new AuthResponseDto(newAccess, null, DateTime.UtcNow.AddMinutes(expiresMinutes)));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // read cookie and revoke corresponding DB refresh token if present
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
            if (token != null)
            {
                token.IsRevoked = true;
                _db.RefreshTokens.Update(token);
                await _db.SaveChangesAsync();
            }
        }

        // delete cookie on client
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth/refresh"
        });

        return Ok();
    }
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        // Extract user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return Unauthorized();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            Role = user.UserRole.ToString()
        });
    }
}
