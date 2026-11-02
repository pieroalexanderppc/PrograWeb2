using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Concreta.ViewModels
{
    public class FilaAguaAireVM
    {
        public int IdParametro { get; set; }
        public string TamanoMaxNominal { get; set; }
        public decimal AsentamientoMin { get; set; }
        public decimal AsentamientoMax { get; set; }

        [Required(ErrorMessage = "Ingresa el agua.")]
        [Range(typeof(decimal), "50", "400", ErrorMessage = "El agua va de 50 a 400 L/m³.")]
        public decimal? AguaLitrosM3 { get; set; }

        [Required(ErrorMessage = "Ingresa el aire.")]
        [Range(typeof(decimal), "0", "10", ErrorMessage = "El aire va de 0 a 10 %.")]
        public decimal? AireAtrapado { get; set; }
    }

    public class FilaRelacionAcVM
    {
        public int IdParametro { get; set; }
        public int FcrKgCm2 { get; set; }

        [Required(ErrorMessage = "Ingresa la relación a/c.")]
        [Range(typeof(decimal), "0.2", "1", ErrorMessage = "La relación a/c va de 0.200 a 1.000.")]
        public decimal? RelacionAC { get; set; }
    }

    public class FilaAgregadoVM
    {
        public int IdParametro { get; set; }
        public string TamanoMaxNominal { get; set; }
        public decimal ModuloFineza { get; set; }

        [Required(ErrorMessage = "Ingresa el volumen.")]
        [Range(typeof(decimal), "0.1", "1", ErrorMessage = "El volumen va de 0.100 a 1.000 m³.")]
        public decimal? VolumenM3 { get; set; }
    }

    public class ParametrosFormVM
    {
        public List<FilaAguaAireVM> AguaAire { get; set; } = new List<FilaAguaAireVM>();
        public List<FilaRelacionAcVM> RelacionAC { get; set; } = new List<FilaRelacionAcVM>();
        public List<FilaAgregadoVM> AgregadoGrueso { get; set; } = new List<FilaAgregadoVM>();
    }
}
