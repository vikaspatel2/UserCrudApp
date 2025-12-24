using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using System.Security.Claims;
using System.Text;
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
                    "EXEC Usp_AddUser @p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7",
                    model.UserName,
                    model.Email,
                    hashedPassword,
                    "User",      // role
                   -1, // createuid
                    DateTime.Now,
                    0,      // TwoFactorEnabled
                    null    // AuthenticatorKey

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
                var user = _context.Users
                    .FromSqlRaw("EXEC Usp_GetUserByEmail @p0", model.Email)
                    .AsNoTracking()
                    .AsEnumerable()
                    .FirstOrDefault();

                if (user != null
                    && user.deldt == null
                    && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Only trigger 2FA if enabled and AuthenticatorKey is present
                    if (user.TwoFactorEnabled == true && !string.IsNullOrEmpty(user.AuthenticatorKey))
                    {
                        TempData["Pending2FA"] = user.Id;
                        return RedirectToAction("Login2FA");
                    }

                    // Normal login (no 2FA required)
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

        // GET: /Account/Enable2FA
        [Authorize]
        public IActionResult Enable2FA()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.Find(int.Parse(userId));

            if (user.TwoFactorEnabled)
                return View("AlreadyEnabled2FA");

            if (string.IsNullOrEmpty(user.AuthenticatorKey))
            {
                var secretBytes = KeyGeneration.GenerateRandomKey(20);
                user.AuthenticatorKey = Base32Encoding.ToString(secretBytes);
                _context.SaveChanges();
            }

            string issuer = "UserCrudApp";
            string otpauth =
                $"otpauth://totp/{issuer}:{user.Email}?secret={user.AuthenticatorKey}&issuer={issuer}";

            using var qrGenerator = new QRCoder.QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(otpauth, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);

            ViewBag.SharedKey = user.AuthenticatorKey;
            ViewBag.QrCodeImage =
                "data:image/png;base64," +
                Convert.ToBase64String(qrCode.GetGraphic(10));

            return View();
        }



        //// POST: /Account/Enable2FA
        //[HttpPost]
        //public IActionResult Enable2FA(string code)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var user = _context.Users.Find(int.Parse(userId));
        //    var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(user.AuthenticatorKey));
        //    bool isValid = totp.VerifyTotp(code, out long _);

        //    if (isValid)
        //    {
        //        user.TwoFactorEnabled = true;
        //        _context.SaveChanges();
        //        ViewBag.Message = "Two-Factor Authentication enabled! Next login will require code from your app.";
        //        return View("Enable2FAFinished");
        //    }
        //    else
        //    {
        //        ViewBag.SharedKey = user.AuthenticatorKey;
        //        ViewBag.QrCodeImage = ViewBag.QrCodeImage; // Regenerate this as above if needed
        //        ViewBag.Error = "Invalid code, retry!";
        //        return View();
        //    }
        //}
        // POST: /Account/Enable2FA
        [HttpPost]
        [Authorize]
        public IActionResult Enable2FA(string code)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.Find(int.Parse(userId));

            if (string.IsNullOrEmpty(user.AuthenticatorKey))
            {
                ModelState.AddModelError("", "Session expired. Please reload the QR page.");
                return RedirectToAction("Enable2FA");
            }

            var totp = new Totp(
                Base32Encoding.ToBytes(user.AuthenticatorKey)
            );

            bool isValid = totp.VerifyTotp(
                code?.Trim(),
                out _,
                VerificationWindow.RfcSpecifiedNetworkDelay
            );

            if (!isValid)
            {
                ViewBag.Error = "Invalid code, please try again.";
                return View();
            }

            user.TwoFactorEnabled = true;
            _context.SaveChanges();

            return View("Enable2FAFinished");
        }




        private static string ToBase32(byte[] data)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            StringBuilder result = new StringBuilder((data.Length + 4) / 5 * 8);
            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;
            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[next++] & 0xFF;
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }
                int index = 0x1F & (buffer >> (bitsLeft - 5));
                bitsLeft -= 5;
                result.Append(base32Chars[index]);
            }
            return result.ToString();
        }

        [HttpPost]
        public IActionResult Verify2FASetup(string code)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.Find(int.Parse(userId));

            var totp = new OtpNet.Totp(Base32Encoding.ToBytes(user.AuthenticatorKey));
            bool isValid = totp.VerifyTotp(code, out long _, OtpNet.VerificationWindow.RfcSpecifiedNetworkDelay);

            if (isValid)
            {
                user.TwoFactorEnabled = true;
                _context.SaveChanges();
                return RedirectToAction("Index", "Users");
            }
            else
            {
                // Show QR again with error
                string issuer = "UserCrudApp";
                string otpauth = $"otpauth://totp/{issuer}:{user.Email}?secret={user.AuthenticatorKey}&issuer={issuer}";
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(otpauth, QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                string qrCodeImage = "data:image/png;base64," + Convert.ToBase64String(qrCode.GetGraphic(10));
                ViewBag.SharedKey = user.AuthenticatorKey;
                ViewBag.QrCodeImage = qrCodeImage;
                ViewBag.Error = "Code invalid, please try again.";
                return View("Enable2FA");
            }
        }

        public IActionResult Login2FA() => View();

        [HttpPost]
        public async Task<IActionResult> Login2FA(string code)
        {
            if (TempData["Pending2FA"] == null)
                return RedirectToAction("Login");

            int userId = (int)TempData["Pending2FA"];
            var user = _context.Users.Find(userId);

            var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(user.AuthenticatorKey));
            bool isValid = totp.VerifyTotp(code, out long _, OtpNet.VerificationWindow.RfcSpecifiedNetworkDelay);
            if (isValid)
            {
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
            ViewBag.Error = "Invalid code.";
            return View();
        }

    }
}