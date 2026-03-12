using ApiContabsv.Models.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace ApiContabsv.Services
{
    public interface IDispositivoService
    {
        Task<bool> EsDispositivoConfiableAsync(int idUsuario, string tokenDispositivo);
        Task RegistrarDispositivoAsync(int idUsuario, string tokenDispositivo);
    }

    public class DispositivoService : IDispositivoService
    {
        private readonly SeguridadContext _context;

        public DispositivoService(SeguridadContext context)
        {
            _context = context;
        }

        public async Task<bool> EsDispositivoConfiableAsync(int idUsuario, string tokenDispositivo)
        {
            return await _context.DispositivosConfiables
                .AnyAsync(d => d.IdUsuario == idUsuario
                            && d.TokenDispositivo == tokenDispositivo
                            && d.FechaExpiracion > DateTime.Now);
        }

        public async Task RegistrarDispositivoAsync(int idUsuario, string tokenDispositivo)
        {
            var dispositivo = new DispositivosConfiable
            {
                IdUsuario = idUsuario,
                TokenDispositivo = tokenDispositivo,
                FechaRegistro = DateTime.Now,
                FechaExpiracion = DateTime.Now.AddDays(30)
            };

            _context.DispositivosConfiables.Add(dispositivo);
            await _context.SaveChangesAsync();
        }
    }
}