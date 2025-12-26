using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using UserCrudApp.Data;
using UserCrudApp.Models;

namespace UserCrudApp.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show current user's profile
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user =  _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", userId)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();

            if (user == null || user.deldt != null)
                return NotFound();

            return View(user);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!id.HasValue || id.ToString() != userId)
                return Unauthorized();

            var user =  _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();
            //.FirstOrDefaultAsync();

            if (user == null || user.deldt != null)
                return NotFound();

            return View(user);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserName,Email,Role")] Users model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id != model.Id || id.ToString() != userId)
                return Unauthorized();

            var user =  _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();  
                //.FirstOrDefaultAsync();

            if (user == null || user.deldt != null)
                return NotFound();

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC Usp_UpdateUser @p0, @p1, @p2, @p3, @p4, @p5, @p6",
                id,
                model.UserName,
                model.Email,
                user.PasswordHash, // unchanged password
                model.Role,
                id, // lmodifyby
                DateTime.Now
            );
            return RedirectToAction(nameof(Index));
        }

        // GET: Delete
        public async Task<IActionResult> Delete(int? id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!id.HasValue || id.ToString() != userId)
                return Unauthorized();

            //var user = await _context.Users
            var user =  _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();
            //.FirstOrDefaultAsync();

            if (user == null || user.deldt != null)
                return NotFound();

            return View(user);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id.ToString() != userId)
                return Unauthorized();

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC Usp_DeleteUser @p0, @p1, @p2",
                id,
                id, // deluid
                DateTime.Now // deldt
            );

            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // Get current user by ID (SP)
            var user = _context.Users
                .FromSqlRaw("EXEC Usp_GetUserById @p0", userId)
                .AsNoTracking()
                .AsEnumerable()
                .FirstOrDefault();

            if (user == null || user.deldt != null)
                return NotFound();

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View(model);
            }


            // Hash new password
            string newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            // Update via SP
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC Usp_ChangeUserPassword @p0, @p1, @p2, @p3",
                userId,
                newHash,
                userId,         // lmodifyby
                DateTime.Now
            );

            ViewBag.Message = "Password changed successfully!";
            return View();
        }

        //private bool IsAdmin()
        //{
        //    return User.IsInRole("Admin"); // Role claim, already set at login
        //}

        //// List all users (admin only)
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AllUsers()
        //{
        //    var users = await _context.Users
        //        .FromSqlRaw("EXEC Usp_GetAllUsers")
        //        .AsNoTracking()
        //        .ToListAsync();
        //    return View(users);
        //}

        //// Edit any user (admin only)
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> EditUser(int? id)
        //{
        //    if (id == null) return BadRequest();
        //    var user = await _context.Users
        //        .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync();
        //    if (user == null) return NotFound();
        //    return View(user);
        //}

        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditUser(int id, Users model)
        //{
        //    if (id != model.Id)
        //        return BadRequest();

        //    // Password remains the same
        //    var user = await _context.Users
        //        .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync();

        //    if (user == null) return NotFound();

        //    await _context.Database.ExecuteSqlRawAsync(
        //        "EXEC Usp_UpdateUser @p0, @p1, @p2, @p3, @p4, @p5, @p6",
        //        id,
        //        model.UserName,
        //        model.Email,
        //        user.PasswordHash,
        //        model.Role,
        //        User.FindFirstValue(ClaimTypes.NameIdentifier), // admin's ID
        //        DateTime.Now
        //    );
        //    return RedirectToAction(nameof(AllUsers));
        //}

        //// Delete any user (admin only, soft delete)
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> DeleteUser(int? id)
        //{
        //    if (id == null) return BadRequest();
        //    var user = await _context.Users
        //        .FromSqlRaw("EXEC Usp_GetUserById @p0", id)
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync();
        //    if (user == null) return NotFound();
        //    return View(user);
        //}

        //[HttpPost, ActionName("DeleteUser")]
        //[Authorize(Roles = "Admin")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteUserConfirmed(int id)
        //{
        //    var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    await _context.Database.ExecuteSqlRawAsync(
        //        "EXEC Usp_DeleteUser @p0, @p1, @p2",
        //        id,
        //        adminId, // deluid
        //        DateTime.Now
        //    );
        //    return RedirectToAction(nameof(AllUsers));
        //}

         
    }
}   