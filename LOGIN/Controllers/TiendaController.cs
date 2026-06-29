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

       public async Task<IActionResult> Index(string? busqueda, bool soloStock = false)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            return RedirectToAction("Login", "Account");

            var query = _context.Productos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(p => p.Nombre.Contains(busqueda) || p.Descripcion!.Contains(busqueda));

            if (soloStock)
            query = query.Where(p => p.Cantidad > 0);

            ViewBag.Busqueda = busqueda;
            ViewBag.SoloStock = soloStock;

            return View(await query.ToListAsync());
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

            if (cantidad < 1) cantidad = 1;

            var carrito = ObtenerCarrito();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);
            int yaEnCarrito = item?.Cantidad ?? 0;

            // No permitir agregar más de lo que hay en stock
            if (yaEnCarrito + cantidad > producto.Cantidad)
            {
                int disponible = producto.Cantidad - yaEnCarrito;
                if (disponible <= 0)
                {
                    TempData["Mensaje"] = $"No hay más stock disponible de {producto.Nombre}";
                }
                else
                {
                    if (item != null)
                        item.Cantidad += disponible;
                    else
                        carrito.Add(new CarritoItem
                        {
                            ProductoId = producto.Id,
                            Nombre = producto.Nombre,
                            Precio = producto.Precio,
                            Cantidad = disponible
                        });

                    GuardarCarrito(carrito);
                    TempData["Mensaje"] = $"Solo quedaban {disponible} unidades de {producto.Nombre}, se agregaron todas";
                }
                return RedirectToAction("Index");
            }

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

        [HttpPost]
        public async Task<IActionResult> ConfirmarCompra()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
                return RedirectToAction("Login", "Account");

            var carrito = ObtenerCarrito();

            if (!carrito.Any())
            {
                TempData["Error"] = "Tu carrito está vacío.";
                return RedirectToAction("Carrito");
            }

            // Validar que todavía haya stock suficiente para cada producto
            foreach (var item in carrito)
            {
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto == null || producto.Cantidad < item.Cantidad)
                {
                    TempData["Error"] = $"No hay suficiente stock de {item.Nombre}. Actualiza tu carrito.";
                    return RedirectToAction("Carrito");
                }
            }

            // Descontar el stock real en la base de datos (Supabase)
            foreach (var item in carrito)
            {
                var producto = await _context.Productos.FindAsync(item.ProductoId);
                if (producto != null)
                {
                    producto.Cantidad -= item.Cantidad;
                }
            }

            await _context.SaveChangesAsync();

            // Vaciar el carrito después de comprar
            GuardarCarrito(new List<CarritoItem>());

            TempData["Mensaje"] = "¡Gracias por tu compra! 🎉 Tu pedido fue procesado correctamente.";
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
