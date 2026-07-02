using Microsoft.AspNetCore.Mvc;

namespace LOGIN.Controllers
{
    public class GarantiaController : Controller
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
