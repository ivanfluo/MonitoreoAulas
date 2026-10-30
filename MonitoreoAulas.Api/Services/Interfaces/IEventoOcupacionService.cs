using MonitoreoAulas.Api.DTOs.Requests;

namespace MonitoreoAulas.Api.Services.Interfaces
{
    public interface IEventoOcupacionService
    {
        Task<(int Status, object Resultado)> RegistrarAsync(RegistrarEventoRequest request, CancellationToken cancellationToken);
    }
}
