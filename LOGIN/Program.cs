using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;

AppContext.SetSwitch("System.Net.DisableIPv6", true);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Agregar controladores MVC (vistas) y API controllers
builder.Services.AddControllersWithViews();
builder.Services.AddControllers(); // ← NUEVO para la API

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("TiendaPC");

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHostedService<LOGIN.Services.BackupService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Registrar el tipo MIME del manifest de la PWA (no viene por defecto)
var staticFileProvider = new FileExtensionContentTypeProvider();
staticFileProvider.Mappings[".webmanifest"] = "application/manifest+json";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileProvider
});

app.UseRouting();

app.UseSession();
app.UseAuthorization();

// Mapeo tanto de rutas de vistas como de API
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapControllers(); // ← NUEVO (para endpoints /api/...)

app.Run();
