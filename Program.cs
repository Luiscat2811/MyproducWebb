using Microsoft.AspNetCore.Authentication.Cookies; // 1. Agregado: Librería para cookies
using Microsoft.EntityFrameworkCore;
using MyproducWeb.Models;

    var builder = WebApplication.CreateBuilder(args);

// 1. REGISTRAR LOS SERVICIOS DE LA APLICACIÓN
// Agregar el soporte para Controladores con Vistas (MVC)
builder.Services.AddControllersWithViews();


// CONFIGURACIÓN DE LA BASE DE DATOS:
// Enchufamos tu TiendaProductDbContext con SQL Server usando la cadena de conexión
builder.Services.AddDbContext<TiendaProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSQL")));

// 2. Agregado: CONFIGURACIÓN DE AUTENTICACIÓN (LOGIN)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/Index"; // Ruta a la que te enviará si no estás logueado
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // Tiempo que dura la sesión
    });

var app = builder.Build();

// 2. CONFIGURAR EL PIPELINE DE PETICIONES HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // El valor predeterminado de HSTS es de 30 días.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Agregado: HABILITAR AUTENTICACIÓN (Debe ir exactamente aquí, antes de Authorization)
app.UseAuthentication();
app.UseAuthorization();

// 3. DEFINIR LAS RUTAS DEL SISTEMA
// Cambiamos el inicio para que cargue directamente tu catálogo de Productos
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Productos}/{action=Index}/{id?}");

app.Run();