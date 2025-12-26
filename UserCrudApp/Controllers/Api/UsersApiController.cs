using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserCrudApp.Data;
using UserCrudApp.Models;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsersApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _context.Users
            .Where(u => u.deldt == null)
            .Select(u => new { u.Id, u.UserName, u.Email, u.Role })
            .ToList();

        _context.ApiLog.Add(new ApiLog
        {
            Path = Request.Path,
            Method = Request.Method,
            User = User.Identity?.Name ?? "Anonymous",
            TimeStamp = DateTime.UtcNow
        });
        _context.SaveChanges();
        return Ok(users);
    }
}