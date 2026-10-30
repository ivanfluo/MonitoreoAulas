using MonitoreoAulas.Api.DTOs.Responses;

namespace MonitoreoAulas.Api.Services.Interfaces
{
    public interface IOcupacionService
    {
        Task<OcupacionActualResponse?> ObtenerActualAsync(string codigoAula, CancellationToken cancellationToken);
    }
}
