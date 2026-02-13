using Microsoft.AspNetCore.Mvc;

namespace UserCrudApp.Controllers
{
    public class UserTransactionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
