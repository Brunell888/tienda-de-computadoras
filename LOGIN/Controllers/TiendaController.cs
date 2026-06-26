using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;
using LOGIN.Models;
using System.Text.Json;

namespace LOGIN.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TiendaController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
                return RedirectToAction("Login", "Account");

            var productos = await _context.Productos.Where(p => p.Cantidad > 0).ToListAsync();
            return View(productos);
        }

        public IActionResult Carrito()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
                return RedirectToAction("Login", "Account");

            var carrito = ObtenerCarrito();
            return View(carrito);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int id, int cantidad = 1)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
                return RedirectToAction("Login", "Account");

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            var carrito = ObtenerCarrito();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);

            if (item != null)
                item.Cantidad += cantidad;
            else
                carrito.Add(new CarritoItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Cantidad = cantidad
                });

            GuardarCarrito(carrito);
            TempData["Mensaje"] = $"{producto.Nombre} agregado al carrito";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult EliminarDelCarrito(int id)
        {
            var carrito = ObtenerCarrito();
            carrito.RemoveAll(c => c.ProductoId == id);
            GuardarCarrito(carrito);
            return RedirectToAction("Carrito");
        }

        [HttpPost]
        public IActionResult VaciarCarrito()
        {
            GuardarCarrito(new List<CarritoItem>());
            TempData["Mensaje"] = "Carrito vaciado";
            return RedirectToAction("Carrito");
        }

        private List<CarritoItem> ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString("Carrito");
            return string.IsNullOrEmpty(json)
                ? new List<CarritoItem>()
                : JsonSerializer.Deserialize<List<CarritoItem>>(json)!;
        }

        private void GuardarCarrito(List<CarritoItem> carrito)
        {
            HttpContext.Session.SetString("Carrito", JsonSerializer.Serialize(carrito));
        }
    }
}
