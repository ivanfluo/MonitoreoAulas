namespace MonitoreoAulas.Api.DTOs.Responses
{
    public class OcupacionActualResponse
    {
        public string Aula { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public int Capacidad { get; set; }

        public int? OcupacionActual { get; set; }

        public decimal? PorcentajeOcupacion { get; set; }

        public string Estado { get; set; } = "DESCONOCIDA";

        public string Conexion { get; set; } = "DESCONECTADO";

        public bool OcupacionConfirmada { get; set; }

        public DateTime? UltimaActualizacion { get; set; }

        public DateTime? UltimaComunicacion { get; set; }
    }
}