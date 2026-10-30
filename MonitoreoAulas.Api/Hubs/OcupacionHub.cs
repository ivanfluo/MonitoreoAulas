
using Microsoft.AspNetCore.SignalR;

namespace MonitoreoAulas.Api.Hubs;

public class OcupacionHub : Hub
{
    public async Task SuscribirseAula(string codigoAula)
    {
        if (string.IsNullOrWhiteSpace(codigoAula))
        {
            throw new HubException("Debe proporcionar un codigo de aula.");
        }

        string grupo = ObtenerGrupo(codigoAula);

        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);
    }

    public async Task DesuscribirseAula(string codigoAula)
    {
        if (string.IsNullOrWhiteSpace(codigoAula))
        {
            return;
        }

        string grupo = ObtenerGrupo(codigoAula);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);
    }

    public static string ObtenerGrupo(string codigoAula)
    {
        return $"aula:{codigoAula.Trim().ToUpperInvariant()}";
    }
}
