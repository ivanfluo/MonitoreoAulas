using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoreoAulas.Api.Data;
using MonitoreoAulas.Api.Services.Interfaces;

namespace MonitoreoAulas.Api.Controllers
{
    [ApiController]
    [Route("api/aulas")]
    public class AulasController : ControllerBase
    {
        private readonly MonitoreoDbContext _context;
        private readonly IOcupacionService _ocupacionService;

        public AulasController(MonitoreoDbContext context, IOcupacionService ocupacionService)
        {
            _context = context;
            _ocupacionService = ocupacionService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerAulas(CancellationToken cancellationToken)
        {
            try
            {

                var aulas = await _context.Aulas
                    .AsNoTracking()
                    .OrderBy(a => a.Codigo)
                    .Select(a => new
                    {
                        a.IdAula,
                        a.Codigo,
                        a.Nombre,
                        a.Edificio,
                        a.Capacidad,
                        a.Activa
                    })
                    .ToListAsync(cancellationToken);

                return Ok(aulas);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener las aulas: {ex}");
            }
        }


        [HttpGet("{codigo}/ocupacion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerOcupacionActual(string codigo, CancellationToken cancellationToken)
        {
            var resultado = await _ocupacionService.ObtenerActualAsync(codigo, cancellationToken);

            if (resultado is null)
            {
                return NotFound(new
                {
                    mensaje = $"No existe el aula {codigo}."
                });
            }

            return Ok(resultado);
        }

    }
}
