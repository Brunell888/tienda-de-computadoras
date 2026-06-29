using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;

namespace LOGIN.Controllers
{
    public class ReporteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReporteController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
                return RedirectToAction("Login", "Account");

            var productos = await _context.Productos.ToListAsync();

            ViewBag.TotalProductos    = productos.Count;
            ViewBag.TotalStock        = productos.Sum(p => p.Cantidad);
            ViewBag.ValorInventario   = productos.Sum(p => p.Precio * p.Cantidad);
            ViewBag.ProductoMasCaro   = productos.OrderByDescending(p => p.Precio).FirstOrDefault()?.Nombre ?? "—";
            ViewBag.ProductoMasStock  = productos.OrderByDescending(p => p.Cantidad).FirstOrDefault()?.Nombre ?? "—";
            ViewBag.SinStock          = productos.Count(p => p.Cantidad == 0);

            return View(productos);
        }
    }
}
