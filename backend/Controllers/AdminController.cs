using backend.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // ================== GET USERS ==================
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100); // prevent huge queries

        var total = await _db.Users.CountAsync();

        var users = await _db.Users
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.Username, u.Email, Role = u.UserRole.ToString() })
            .ToListAsync();

        return Ok(new { total, users, page, pageSize });
    }

    // ================== DELETE USER ==================
    [HttpDelete("user/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        // Prevent self-deletion
        var currentAdminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentAdminId != null && Guid.TryParse(currentAdminId, out var adminId) && adminId == id)
            return BadRequest(new { error = "You cannot delete yourself." });

        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.Images)
            .Include(u => u.TextFiles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound(new { error = "User not found." });
        if (user.UserRole == Role.Admin)
            return BadRequest(new { error = "Cannot delete another admin." });

        // Optional: transaction to be safe
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.RefreshTokens.RemoveRange(user.RefreshTokens);
            _db.Images.RemoveRange(user.Images);
            _db.TextFiles.RemoveRange(user.TextFiles);
            _db.Users.Remove(user);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = "User deleted successfully." });
        }
        catch
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { error = "Failed to delete user." });
        }
    }
}
