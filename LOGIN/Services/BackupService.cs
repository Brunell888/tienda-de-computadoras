using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;

namespace LOGIN.Services
{
    public class BackupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackupService> _logger;
        private readonly string _carpetaBackups;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(24); // cada 24h
        private const int MAX_BACKUPS = 7; // conserva los últimos 7

        public BackupService(IServiceScopeFactory scopeFactory, ILogger<BackupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _carpetaBackups = Path.Combine(AppContext.BaseDirectory, "Backups");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Directory.CreateDirectory(_carpetaBackups);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerarBackupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generando el backup automático");
                }

                await Task.Delay(_intervalo, stoppingToken);
            }
        }

        private async Task GenerarBackupAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var backup = new
            {
                FechaBackup = DateTime.Now,
                Usuarios = await db.Usuarios.AsNoTracking().ToListAsync(),
                Productos = await db.Productos.AsNoTracking().ToListAsync()
            };

            var nombreArchivo = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var ruta = Path.Combine(_carpetaBackups, nombreArchivo);

            var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(ruta, json);

            _logger.LogInformation("Backup generado: {Archivo}", nombreArchivo);

            LimpiarBackupsAntiguos();
        }

        private void LimpiarBackupsAntiguos()
        {
            var archivos = Directory.GetFiles(_carpetaBackups, "backup_*.json")
                                     .OrderByDescending(f => f)
                                     .ToList();

            foreach (var viejo in archivos.Skip(MAX_BACKUPS))
                File.Delete(viejo);
        }
    }
}
