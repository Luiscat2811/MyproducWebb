using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyproducWeb.Models;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.IdentityModel.Tokens; // NECESARIO PARA EL CÓDIGO SEGURO (TokenValidationParameters)
using System.Text; // NECESARIO PARA EL CÓDIGO SEGURO (Encoding.UTF8)

namespace MyproducWeb.Controllers
{
    [Authorize]
    public class ProductosController : Controller
    {
        private readonly TiendaProductDbContext _context;

        public ProductosController(TiendaProductDbContext context)
        {
            _context = context;
        }

        // ACCIÓN: Muestra el catálogo de productos con buscador
        public async Task<IActionResult> Index(string buscar)
        {
            ////Busqueda insegura

            //var productos = new List<Producto>();

            //if (!string.IsNullOrEmpty(buscar))
            //{
            //    // ⚠️ VULNERABILIDAD INTENCIONAL: Inyección SQL (SQL Injection)
            //    // Al concatenar la variable 'buscar' directamente con un string, 
            //    // permitimos que caracteres especiales rompan la consulta SQL.
            //    string sqlQuery = $"SELECT * FROM Productos WHERE Nombre LIKE '%{buscar}%' OR Descripcion LIKE '%{buscar}%'";

            //    productos = await _context.Productos
            //        .FromSqlRaw(sqlQuery)
            //        .Include(p => p.Categoria)
            //        .ToListAsync();
            //}
            //else
            //{
            //    productos = await _context.Productos
            //        .Include(p => p.Categoria)
            //        .ToListAsync();
            //}

            //ViewBag.BusquedaActual = buscar;
            //return View(productos);


            //Busqueda Segura

            // 1. Iniciamos la consulta base incluyendo la categoría
            var query = _context.Productos.Include(p => p.Categoria).AsQueryable();

            // 2. SEGURO: Uso de LINQ / Métodos parametrizados
            // Entity Framework traduce esto internamente a parámetros seguros (Prepared Statements),
            // previniendo completamente cualquier intento de Inyección SQL.
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.Nombre.Contains(buscar) || p.Descripcion.Contains(buscar));
            }

            ViewBag.BusquedaActual = buscar;

            // 3. Ejecutamos la consulta de manera segura
            var listaProductos = await query.ToListAsync();
            return View(listaProductos);
        }

        // ACCIÓN 1: Muestra el formulario para crear un nuevo producto (GET)
        public async Task<IActionResult> Create()
        {
            // Cargamos las categorías para el menú desplegable
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View();
        }

        // ACCIÓN 2: Recibe los datos del formulario y los guarda en SQL Server (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();

                // Guardamos el mensaje flotante de éxito
                TempData["MensajeExito"] = $"¡El producto '{producto.Nombre}' se guardó correctamente!";
                return RedirectToAction(nameof(Index));
            }

            // Si algo falla, recargamos las categorías antes de volver a mostrar la vista
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View(producto);
        }

        // =========================================================
        // FLUJO DE EDICIÓN (EDITAR)
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (id != producto.ProductoId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                    TempData["MensajeExito"] = $"¡El producto '{producto.Nombre}' se actualizó correctamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Productos.Any(e => e.ProductoId == producto.ProductoId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            return View(producto);
        }

        // =========================================================
        // FLUJO DE ELIMINACIÓN (ELIMINAR)
        // =========================================================

        [HttpPost]
        //[AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"¡El producto '{producto.Nombre}' fue eliminado con éxito!";
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // LABORATORIO OWASP: VULNERABILIDAD JWT BYPASS
        // =========================================================

        [AllowAnonymous]
        [HttpGet("api/admin/datos-sensibles")]
        public IActionResult ObtenerDatosSecretos([FromHeader(Name = "Authorization")] string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return BadRequest("Falta el token JWT en la cabecera Authorization");

            var token = authHeader.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();

            try
            {
                //// =====================================================================
                //// ⚠️ VULNERABILIDAD INTENCIONAL (COMENTADA PARA DEJAR EL CÓDIGO SEGURO)
                //// =====================================================================

                //// ReadJwtToken solo descodifica el payload (Base64) pero NO verifica la firma secreta.
                //var jwtToken = handler.ReadJwtToken(token);

                //var rolClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                //if (rolClaim == "Admin")
                //{
                //    return Ok(new
                //    {
                //        Exito = true,
                //        Mensaje = "Acceso concedido mediante JWT manipulado.",
                //        DatosSecretos = "Mostrando datos financieros confidenciales de E-Commerce 2026."
                //    });
                //}

                //return StatusCode(403, "Acceso denegado. No tienes privilegios de Administrador.");

                ////Fin código inseguro


                // =====================================================================
                // ✅ CÓDIGO SEGURO: VALIDACIÓN ESTRICTA DE FIRMA CRIPTOGRÁFICA
                // =====================================================================

                // En un proyecto real, esta clave viene de appsettings.json. Aquí la definimos por demostración.
                var claveSecreta = "MI_CLAVE_SECRETA_DEL_SERVIDOR_SÚPER_LARGA_DE_MÁS_DE_32_CARACTERES";

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    RequireSignedTokens = true // ESTO BLOQUEA LOS ATAQUES DE "alg: none"
                };

                // ValidateToken lanza una excepción automáticamente si el token fue manipulado o no tiene la firma correcta
                var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                var rolClaim = principal.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                if (rolClaim == "Admin")
                {
                    return Ok(new
                    {
                        Exito = true,
                        Mensaje = "Acceso concedido. Token validado criptográficamente.",
                        DatosSecretos = "Mostrando datos financieros confidenciales de E-Commerce 2026."
                    });
                }

                return StatusCode(403, "Acceso denegado. No tienes privilegios de Administrador.");

                // Fin metodo seguro
            }
            catch (Exception ex)
            {
                // Si el atacante envía un token sin firma (alg: none) o modificado, el servidor caerá en este bloque catch
                return BadRequest($"Bloqueo de seguridad: El token ha sido manipulado, no tiene firma válida o está mal formado. Detalle: {ex.Message}");
            }
        }


        // =========================================================
        // LABORATORIO OWASP: VULNERABILIDAD SSRF
        // =========================================================

        [AllowAnonymous]
        [HttpGet("api/fetch-image")]
        public async Task<IActionResult> FetchExternalData([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
                return BadRequest("Falta el parámetro 'url'.");

            using var client = new HttpClient();

            try
            {
                // =====================================================================
                // ⚠️ VULNERABILIDAD INTENCIONAL (SSRF ACTIVO PARA PRUEBAS): 
                // El servidor no valida el destino y hace la petición HTTP a donde sea.
                // =====================================================================

                //var response = await client.GetStringAsync(url);
                //return Content(response, "text/plain");


                // =====================================================================
                // ✅ CÓDIGO SEGURO: VALIDACIÓN MEDIANTE LISTA BLANCA (WHITELIST)
                // (Comentado para que puedas hacer el ataque primero)
                // =====================================================================

                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) ||
                    (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                {
                    return BadRequest("Formato de URL inválido.");
                }

                // Validamos que el servidor SOLO pueda hacer peticiones a dominios autorizados
                if (uriResult.Host != "api.proveedor-autorizado.com")
                {
                    return StatusCode(403, "Bloqueo SSRF: Intento de acceso a red interna o dominio no autorizado.");
                }

                var responseSegura = await client.GetStringAsync(uriResult);
                return Content(responseSegura, "text/plain");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al conectar con el destino: {ex.Message}");
            }
        }


        // =========================================================
        // LABORATORIO OWASP: VULNERABILIDAD IDOR & MASS ASSIGNMENT
        // =========================================================

        [AllowAnonymous]
        [HttpPost("api/productos/actualizar-perfil")]
        public async Task<IActionResult> ActualizarPerfilVulnerable(int id, [FromBody] Producto productoModificado)
        {
            // ⚠️ VULNERABILIDAD IDOR: Buscamos el registro directo por ID sin validar permisos del usuario actual
            var productoExistente = await _context.Productos.FindAsync(id);
            if (productoExistente == null) return NotFound("Producto no encontrado.");

            try
            {
                // =====================================================================
                // ⚠️ VULNERABILIDAD MASS ASSIGNMENT (ACTIVA PARA PRUEBAS):
                // Modificamos el objeto asignando directamente campos críticos (como Stock) desde el JSON.
                // =====================================================================

                productoExistente.Nombre = productoModificado.Nombre;
                productoExistente.Descripcion = productoModificado.Descripcion;
                productoExistente.Precio = productoModificado.Precio;

                // Campo vulnerable a Asignación Masiva manipulado externamente:
                productoExistente.Stock = productoModificado.Stock;

                _context.Productos.Update(productoExistente);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Exito = true,
                    Mensaje = "Producto actualizado mediante Mass Assignment / IDOR con éxito.",
                    DatosActualizados = productoExistente
                });


                // =====================================================================
                // ✅ CÓDIGO SEGURO: FILTRADO EXPLÍCITO DE PROPIEDADES (BLOQUEO DE MASS ASSIGNMENT)
                // =====================================================================
                /*
                productoExistente.Nombre = productoModificado.Nombre;
                productoExistente.Descripcion = productoModificado.Descripcion;
                
                // Precio y Stock quedan protegidos de modificaciones masivas externas
                _context.Productos.Update(productoExistente);
                await _context.SaveChangesAsync();
                return Ok(new { Mensaje = "Actualización segura aplicada correctamente." });
                */
            }
            catch (Exception ex)
            {
                return BadRequest($"Error en la actualización: {ex.Message}");
            }
        }


        // =========================================================
        // SHOWCASE OWASP API1:2023: VULNERABILIDAD BOLA / IDOR EN APIS
        // (Configurado para probar entre Usuario ID 1 y Usuario ID 3)
        // =========================================================

        // 1. BOLA DE LECTURA 
        [AllowAnonymous]
        [HttpGet("api/usuarios/perfil/{id}")]
        public async Task<IActionResult> ObtenerPerfilUsuario(int id, [FromHeader(Name = "X-Usuario-Autenticado-Id")] int usuarioActualId)
        {
            if (usuarioActualId <= 0)
                return Unauthorized(new { Error = "Debes iniciar sesión (envía cabecera X-Usuario-Autenticado-Id: 1)." });

            var usuarioObjetivo = await _context.Usuarios.FindAsync(id);
            if (usuarioObjetivo == null)
                return NotFound(new { Error = $"El usuario con ID {id} no existe en la base de datos." });

            //// =====================================================================
            //// ⚠️ CÓDIGO VULNERABLE 
            //// =====================================================================
            //return Ok(new
            //{
            //    Mensaje = "Perfil obtenido exitosamente (VULNERABLE A BOLA / IDOR)",
            //    UsuarioAutenticado = usuarioActualId,
            //    PerfilConsultadoId = id,
            //    DatosExpuestos = usuarioObjetivo
            //});


            // =====================================================================
            // ✅ CÓDIGO SEGURO
            // =====================================================================

            if (id != usuarioActualId)
            {
                return StatusCode(403, new
                {
                    Bloqueo = "403 Forbidden - Acceso Denegado (BOLA Mitigado)",
                    Detalle = $"El usuario con ID {usuarioActualId} no tiene autorización para acceder al perfil del ID {id}."
                });
            }

            return Ok(new
            {
                Mensaje = "Perfil propio obtenido de forma segura",
                Datos = usuarioObjetivo
            });

        }

        // 2. BOLA DE ESCRITURA (Modificación no autorizada de la cuenta ID 3 desde ID 1)
        [AllowAnonymous]
        [HttpPut("api/usuarios/actualizar-correo/{id}")]
        public async Task<IActionResult> ActualizarCorreoBola(int id, [FromHeader(Name = "X-Usuario-Autenticado-Id")] int usuarioActualId, [FromBody] string nuevoCorreo)
        {
            if (usuarioActualId <= 0)
                return Unauthorized("Token o sesión requerida (X-Usuario-Autenticado-Id).");

            var usuarioObjetivo = await _context.Usuarios.FindAsync(id);
            if (usuarioObjetivo == null)
                return NotFound($"Usuario con ID {id} no encontrado.");

            //// =====================================================================
            //// ⚠️ CÓDIGO VULNERABLE  correo del usuario 3 sin control de propiedad.
            //// =====================================================================
            //usuarioObjetivo.Correo = nuevoCorreo;
            //_context.Usuarios.Update(usuarioObjetivo);
            //await _context.SaveChangesAsync();

            //return Ok(new
            //{
            //    Exito = true,
            //    Mensaje = $"Correo del usuario ID {id} modificado sin autorización por el usuario ID {usuarioActualId}."
            //});


            // =====================================================================
            // ✅ CÓDIGO SEGURO 
            // =====================================================================

            if (id != usuarioActualId)
            {
                return StatusCode(403, new
                {
                    Bloqueo = "403 Forbidden - Acceso Denegado (BOLA Mitigado)",
                    Detalle = $"El usuario {usuarioActualId} no puede modificar datos del usuario {id}."
                });
            }

            usuarioObjetivo.Correo = nuevoCorreo;
            _context.Usuarios.Update(usuarioObjetivo);
            await _context.SaveChangesAsync();

            return Ok(new { Exito = true, Mensaje = "Tu correo fue actualizado correctamente." });

        }

    }
}