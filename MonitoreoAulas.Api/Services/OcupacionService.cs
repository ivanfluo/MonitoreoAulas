using Microsoft.EntityFrameworkCore;
using MonitoreoAulas.Api.Data;
using MonitoreoAulas.Api.DTOs.Responses;
using MonitoreoAulas.Api.Services.Interfaces;

namespace MonitoreoAulas.Api.Services
{
    public class OcupacionService : IOcupacionService
    {
        private readonly MonitoreoDbContext _context;

        private static readonly TimeSpan TiempoSinComunicacion = TimeSpan.FromSeconds(30);

        public OcupacionService(MonitoreoDbContext context)
        {
            _context = context;
        }

        public async Task<OcupacionActualResponse?> ObtenerActualAsync(string codigoAula, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(codigoAula))
            {
                return null;
            }

            string codigo = codigoAula.Trim().ToUpperInvariant();

            var aula = await _context.Aulas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Codigo == codigo, cancellationToken);

            if (aula is null)
            {
                return null;
            }

            var ultimoEvento = await _context.EventosOcupacion
                .AsNoTracking()
                .Where(x => x.IdAula == aula.IdAula)
                .OrderByDescending(x => x.FechaHora)
                .ThenByDescending(x => x.IdEvento)
                .FirstOrDefaultAsync(cancellationToken);


            var ultimoMovimiento = await _context.EventosOcupacion
                .AsNoTracking()
                .Where(x =>
                    x.IdAula == aula.IdAula &&
                    (x.TipoEvento == "ENTRADA" ||
                     x.TipoEvento == "SALIDA"))
                .OrderByDescending(x => x.FechaHora)
                .ThenByDescending(x => x.IdEvento)
                .FirstOrDefaultAsync(cancellationToken);

            var ultimoInicio = await _context.EventosOcupacion
                .AsNoTracking()
                .Where(x =>
                    x.IdAula == aula.IdAula &&
                    x.TipoEvento == "INICIO")
                .OrderByDescending(x => x.FechaHora)
                .ThenByDescending(x => x.IdEvento)
                .FirstOrDefaultAsync(cancellationToken);

            DateTime ahoraUtc = DateTime.UtcNow;

            bool conectado = ultimoEvento is not null && ahoraUtc - ultimoEvento.FechaHora <= TiempoSinComunicacion;

            // Si el ultimo arranque es posterior al ultimo movimiento, la ocupacion no esta confirmada.
            bool reinicioPosterior = ultimoInicio is not null && (ultimoMovimiento is null || ultimoInicio.IdEvento > ultimoMovimiento.IdEvento);

            bool ocupacionConfirmada = ultimoMovimiento is not null && !reinicioPosterior;

            int? ocupacion = ocupacionConfirmada ? ultimoMovimiento!.Ocupacion : null;

            decimal? porcentaje = ocupacion.HasValue ? Math.Round((decimal)ocupacion.Value / aula.Capacidad * 100, 2) : null;

            string estado = ocupacion switch
            {
                null => "DESCONOCIDA",
                0 => "VACIA",
                _ when ocupacion.Value >= aula.Capacidad => "LLENA",
                _ => "OCUPADA"
            };

            return new OcupacionActualResponse
            {
                Aula = aula.Codigo,
                Nombre = aula.Nombre,
                Capacidad = aula.Capacidad,
                OcupacionActual = ocupacion,
                PorcentajeOcupacion = porcentaje,
                Estado = estado,
                Conexion = conectado
                    ? "CONECTADO"
                    : "DESCONECTADO",
                OcupacionConfirmada = ocupacionConfirmada,
                UltimaActualizacion = ultimoMovimiento?.FechaHora,
                UltimaComunicacion = ultimoEvento?.FechaHora
            };
        }
    }
}
