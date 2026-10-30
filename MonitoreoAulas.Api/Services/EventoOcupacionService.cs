using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MonitoreoAulas.Api.Data;
using MonitoreoAulas.Api.Domain.Entities;
using MonitoreoAulas.Api.DTOs.Requests;
using MonitoreoAulas.Api.Hubs;
using MonitoreoAulas.Api.Services.Interfaces;

namespace MonitoreoAulas.Api.Services
{
    public class EventoOcupacionService : IEventoOcupacionService
    {
        private readonly MonitoreoDbContext _context;
        private readonly ILogger<EventoOcupacionService> _logger;
        private readonly IHubContext<OcupacionHub> _hubContext;

        private static readonly HashSet<string> TiposPermitidos =
        [
            "INICIO",
            "ENTRADA",
            "SALIDA",
            "ESTADO",
            "CAPACIDAD_MAXIMA",
            "AULA_VACIA"
        ];

        public EventoOcupacionService(MonitoreoDbContext context, ILogger<EventoOcupacionService> logger, IHubContext<OcupacionHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<(int Status, object Resultado)> RegistrarAsync(RegistrarEventoRequest request, CancellationToken cancellationToken)
        {
            string codigoAula = request.Aula.Trim().ToUpperInvariant();
            string tipoEvento = request.Evento.Trim().ToUpperInvariant();

            if (!TiposPermitidos.Contains(tipoEvento))
            {
                return (400, new
                {
                    mensaje = "Tipo de evento no permitido."
                });
            }

            var aula = await _context.Aulas
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Codigo == codigoAula,
                    cancellationToken);

            if (aula is null)
            {
                return (404, new
                {
                    mensaje = "El aula no existe."
                });
            }

            if (!aula.Activa)
            {
                return (409, new
                {
                    mensaje = "El aula se encuentra inactiva."
                });
            }

            int ocupacion = request.Ocupacion!.Value;
            int capacidad = request.Capacidad!.Value;
            long tiempoMs = request.TiempoMs!.Value;

            if (capacidad != aula.Capacidad)
            {
                return (400, new
                {
                    mensaje = "La capacidad enviada no coincide con el aula."
                });
            }

            if (ocupacion > aula.Capacidad)
            {
                return (400, new
                {
                    mensaje = "La ocupacion supera la capacidad maxima."
                });
            }


            string? idSesion = request.IdSesion?.Trim();
            long? secuencia = request.Secuencia;

            if (string.IsNullOrWhiteSpace(idSesion) != !secuencia.HasValue)
            {
                return (400, new
                {
                    mensaje = "IdSesion y Secuencia deben enviarse juntos."
                });
            }

            if (idSesion is not null && !Guid.TryParse(idSesion, out _))
            {
                return (400, new
                {
                    mensaje = "IdSesion debe ser un GUID valido."
                });
            }

            if (idSesion is not null)
            {
                idSesion = Guid.Parse(idSesion).ToString("D");

                bool duplicado = await _context.EventosOcupacion
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.IdAula == aula.IdAula &&
                        x.IdSesion == idSesion &&
                        x.Secuencia == secuencia,
                        cancellationToken);

                if (duplicado)
                {
                    return (200, new
                    {
                        mensaje = "Evento recibido anteriormente.",
                        duplicado = true
                    });
                }
            }

            var evento = new EventoOcupacion
            {
                IdAula = aula.IdAula,
                TipoEvento = tipoEvento,
                Ocupacion = ocupacion,
                TiempoArduinoMs = tiempoMs,
                FechaHora = DateTime.UtcNow,
                IdSesion = idSesion,
                Secuencia = secuencia
            };

            _context.EventosOcupacion.Add(evento);

            await _context.SaveChangesAsync(cancellationToken);


            if (tipoEvento is "ENTRADA" or "SALIDA")
            {
                try
                {
                    var datos = new
                    {
                        aula = aula.Codigo,
                        ocupacionActual = evento.Ocupacion,
                        capacidad = aula.Capacidad,
                        porcentajeOcupacion = Math.Round((decimal)evento.Ocupacion / aula.Capacidad * 100, 2),
                        estado = evento.Ocupacion == 0 ? "VACIA" : evento.Ocupacion >= aula.Capacidad ? "LLENA" : "OCUPADA",
                        idUltimoEvento = evento.IdEvento,
                        fechaHora = evento.FechaHora
                    };

                    await _hubContext.Clients
                        .Group(OcupacionHub.ObtenerGrupo(aula.Codigo))
                        .SendAsync("OcupacionActualizada", datos, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "El evento {IdEvento} se guardó, pero falló su notificación SignalR.",
                        evento.IdEvento);
                }
            }


            _logger.LogInformation(
                "Evento registrado. Aula: {Aula}, Evento: {Evento}, Ocupacion: {Ocupacion}",
                codigoAula,
                tipoEvento,
                ocupacion);

            return (201, new
            {
                mensaje = "Evento registrado correctamente.",
                idEvento = evento.IdEvento,
                aula = codigoAula,
                tipoEvento,
                ocupacion,
                fechaHora = evento.FechaHora
            });
        }
    }
}
