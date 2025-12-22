using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserCrudApp.Data;
using UserCrudApp.Models;

namespace UserCrudApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // REGISTER

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users
                .FromSqlRaw("EXEC Usp_GetUserByEmail @p0", model.Email)
                .AsNoTracking()
                .AsEnumerable()                    
                .FirstOrDefault();             

                if (user != null)
                {
                    ModelState.AddModelError(string.Empty, "Email already registered.");
                    return View(model);
                }

                // Hash password with BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Call SP to insert user
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC Usp_AddUser @p0, @p1, @p2, @p3, @p4, @p5",
                    model.UserName,
                    model.Email,
                    hashedPassword,
                    "User",      // role
                   -1, // createuid
                    DateTime.Now
                );

                return RedirectToAction("Login");
            }
            return View(model);
        }

        // LOGIN

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user =  _context.Users
                    .FromSqlRaw("EXEC Usp_GetUserByEmail @p0", model.Email)
                    .AsNoTracking()
                    .AsEnumerable()
                    .FirstOrDefault();

                if (user != null && user.deldt == null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Create claims for login session
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role ?? "User")
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Users");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        // LOGOUT

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}