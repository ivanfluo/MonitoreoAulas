using System.ComponentModel.DataAnnotations;

namespace MonitoreoAulas.Api.DTOs.Requests
{
    public class RegistrarEventoRequest
    {
        [Required]
        [MaxLength(20)]
        public string Aula { get; set; } = string.Empty;

        [Required]
        public string Evento { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int? Ocupacion { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? Capacidad { get; set; }

        [Required]
        [Range(0, long.MaxValue)]
        public long? TiempoMs { get; set; }

        [MaxLength(36)]
        public string? IdSesion { get; set; }

        [Range(0, long.MaxValue)]
        public long? Secuencia { get; set; }
    }
}
