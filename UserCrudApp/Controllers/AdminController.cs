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
        public async Task<IActionResult> AllUsers(string searchString, int page = 1, int pageSize = 10)
        {
            var users = _context.Users
                .FromSqlRaw("EXEC Usp_GetAllUsers")
                .AsNoTracking()
                .AsEnumerable();

            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    (u.UserName != null && u.UserName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Email != null && u.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                );
            }

            var totalUsers = users.Count();
            var pagedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.SearchString = searchString;

            return View(pagedUsers);
        }

        // Edit any user
        public async Task<IActionResult> EditUser(int? id)
        {
            if (id == null) return BadRequest();
            var user =  _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();
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
            var user =  _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();

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
            var user =  _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();
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