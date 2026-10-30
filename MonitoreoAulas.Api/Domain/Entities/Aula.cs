namespace MonitoreoAulas.Api.Domain.Entities
{
    public class Aula
    {
        public int IdAula { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? Edificio { get; set; } 

        public int Capacidad { get; set; }

        public bool Activa { get; set; } = true;

        public ICollection<EventoOcupacion> EventosOcupacion { get; set; } = new List<EventoOcupacion>();
    }
}
