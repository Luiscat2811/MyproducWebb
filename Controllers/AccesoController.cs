using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MyproducWeb.Models;

namespace MyproducWeb.Controllers
{
    public class AccesoController : Controller
    {
        private readonly TiendaProductDbContext _context;

        public AccesoController(TiendaProductDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel modelo)
        {
            // Busca al usuario en la base de datos
            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Correo == modelo.Correo && u.Clave == modelo.Clave);

            if (usuario != null)
            {
                // Si existe, creamos las credenciales (Claims)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim(ClaimTypes.Email, usuario.Correo)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Iniciamos la sesión en el navegador
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                // Redirigimos al catálogo de productos
                return RedirectToAction("Index", "Productos");
            }

            ViewBag.Error = "Correo o contraseña incorrectos";
            return View();
        }

        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Acceso");
        }
    }
}