using System.Security.Claims;
using backend.Database;
using backend.DTOs;
using backend.Models;
using backend.Services.Interfaces;
using backend.Services.ServiceDef;
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
    private readonly IRateLimiterService _rateLimiter;

    public AuthController(
        AppDbContext db,
        IJwtService jwt,
        IConfiguration config,
        IRateLimiterService rateLimiter)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
        _rateLimiter = rateLimiter;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            return BadRequest(new { error = "Password must be at least 8 characters." });

        if (await _db.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
            return BadRequest(new { error = "Username or email already in use." });

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            UserRole = Role.User
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!_rateLimiter.CanPerformAction(Guid.Empty, $"login:{dto.Username}"))
            return StatusCode(429);

        _rateLimiter.RecordAction(Guid.Empty, $"login:{dto.Username}");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized();

        return await IssueTokensAsync(user);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var rawToken) ||
            string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized();

        var tokenHash = JwtService.RefreshTokenHasher.Hash(rawToken);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r =>
                r.Token == tokenHash &&
                !r.IsRevoked &&
                r.ExpiresAt > DateTime.UtcNow);

        if (existing == null)
            return Unauthorized();

        existing.IsRevoked = true;

        var newRawToken = _jwt.GenerateRefreshToken();
        var newToken = new RefreshToken
        {
            Token = JwtService.RefreshTokenHasher.Hash(newRawToken),
            UserId = existing.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        SetCookie("refreshToken", newRawToken, 7 * 24 * 60, true);

        var accessToken = _jwt.GenerateAccessToken(existing.User);

        return Ok(new AuthResponseDto(
            accessToken,
            null,
            DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:AccessTokenMinutes"]!)
            )
        ));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out var rawToken))
        {
            var tokenHash = JwtService.RefreshTokenHasher.Hash(rawToken);

            var token = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == tokenHash && !r.IsRevoked);

            if (token != null)
            {
                token.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }

        DeleteAuthCookies();
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId))
            return Unauthorized();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            Role = user.UserRole.ToString()
        });
    }

    private async Task<IActionResult> IssueTokensAsync(User user)
    {
        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = JwtService.RefreshTokenHasher.Hash(refreshToken),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _db.SaveChangesAsync();

        SetCookie("refreshToken", refreshToken, 7 * 24 * 60, true);

        return Ok(new AuthResponseDto(
            accessToken,
            null,
            DateTime.UtcNow.AddMinutes(
                double.Parse(_config["Jwt:AccessTokenMinutes"]!)
            )
        ));
    }

    private void SetCookie(string name, string value, int minutes, bool httpOnly)
    {
        Response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = httpOnly,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(minutes),
            Path = "/"
        });
    }

    private void DeleteAuthCookies()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });
    }
}
