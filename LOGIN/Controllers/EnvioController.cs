using Microsoft.AspNetCore.Mvc;

namespace LOGIN.Controllers
{
    public class EnvioController : Controller
    {
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }
    }
}
