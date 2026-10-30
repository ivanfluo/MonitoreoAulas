namespace MonitoreoAulas.Api.Domain.Entities
{
    public class EventoOcupacion
    {
        public long IdEvento { get; set; }

        public int IdAula { get; set; }

        public string TipoEvento { get; set; } = string.Empty;

        public int Ocupacion { get; set; }

        public DateTime FechaHora { get; set; }

        public long? TiempoArduinoMs { get; set; }

        public string? IdSesion { get; set; }

        public long? Secuencia { get; set; }

        public Aula Aula { get; set; } = null!;
    }
}
