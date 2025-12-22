using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserCrudApp.Data;
using UserCrudApp.Models;
using System.Security.Claims;

namespace UserCrudApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List all users
        public async Task<IActionResult> AllUsers()
        {
            var users = await _context.Users
                .FromSqlRaw("EXEC Usp_GetAllUsers")
                .AsNoTracking()
                .ToListAsync();
            return View(users);
        }

        // Edit any user
        public async Task<IActionResult> EditUser(int? id)
        {
            if (id == null) return BadRequest();
            var user = await _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, Users model)
        {
            if (id != model.Id)
                return BadRequest();

            // Keep password as-is
            var user = await _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC Usp_UpdateUser @p0, @p1, @p2, @p3, @p4, @p5, @p6",
                id,
                model.UserName,
                model.Email,
                user.PasswordHash,
                model.Role,
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                DateTime.Now
            );
            return RedirectToAction(nameof(AllUsers));
        }

        // Delete any user (soft delete)
        public async Task<IActionResult> DeleteUser(int? id)
        {
            if (id == null) return BadRequest();
            var user = await _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC Usp_DeleteUser @p0, @p1, @p2",
                id,
                adminId,
                DateTime.Now
            );
            return RedirectToAction(nameof(AllUsers));
        }
    }
}