using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UserCrudApp.Data;
using UserCrudApp.Models;

namespace UserCrudApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _db = context;
        }

        public IActionResult Index()
        {
            ViewBag.UserCount = _db.Users.Count();
            var today = DateTime.Today;
            ViewBag.ApiUsage = _db.ApiLog.Count(log => log.TimeStamp >= today);
            ViewBag.RecentUsers = _db.Users.OrderByDescending(u => u.createdt).Take(5).ToList();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
