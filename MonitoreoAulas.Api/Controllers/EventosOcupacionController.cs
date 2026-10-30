using Microsoft.AspNetCore.Mvc;
using MonitoreoAulas.Api.DTOs.Requests;
using MonitoreoAulas.Api.Services.Interfaces;

namespace MonitoreoAulas.Api.Controllers
{
    [ApiController]
    [Route("api/ocupacion/eventos")]
    public class EventosOcupacionController : ControllerBase
    {
        private readonly IEventoOcupacionService _servicio;

        public EventosOcupacionController(IEventoOcupacionService servicio)
        {
            _servicio = servicio;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Registrar([FromBody] RegistrarEventoRequest request, CancellationToken cancellationToken)
        {
            var (status, resultado) = await _servicio.RegistrarAsync(request, cancellationToken);

            return StatusCode(status, resultado);
        }
    }
}
