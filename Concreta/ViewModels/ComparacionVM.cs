using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Concreta.Models.Entities;
using Concreta.Services;

namespace Concreta.ViewModels
{
    public class ComparacionFormVM
    {
        [Display(Name = "Diseños a comparar")]
        public List<int> IdsDisenos { get; set; } = new List<int>();

        [Required(ErrorMessage = "Selecciona el criterio.")]
        [Display(Name = "Criterio")]
        public string Criterio { get; set; } = Services.Criterio.Balanceado;

        [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
        [Display(Name = "Nombre de la comparación")]
        public string Nombre { get; set; }

        public List<DisenoFilaVM> Disenos { get; set; } = new List<DisenoFilaVM>();
    }

    /// <summary>Una columna de la tabla comparativa.</summary>
    public class ColumnaComparacion
    {
        public DisenoMezcla Diseno { get; set; }
        public string Cemento { get; set; }
        public decimal CementoKg { get; set; }
        public decimal? Resistencia28d { get; set; }
        public EstadoResistencia Estado { get; set; }
        public decimal Puntaje { get; set; }
        public int Posicion { get; set; }
        public DosificacionVM Dosificacion { get; set; }
    }

    public class ComparacionDetalleVM
    {
        public Comparacion Comparacion { get; set; }
        public List<ColumnaComparacion> Columnas { get; set; } = new List<ColumnaComparacion>();
        public ColumnaComparacion Recomendada { get; set; }
    }

    public class AlternativasFormVM
    {
        [Required(ErrorMessage = "Ingresa el f'c.")]
        [Range(100, 700, ErrorMessage = "Ingresa un f'c entre 100 y 700 kg/cm².")]
        [Display(Name = "f'c")]
        public int? FcKgCm2 { get; set; } = 210;

        [Required(ErrorMessage = "Ingresa el asentamiento.")]
        [Range(typeof(decimal), "1", "7", ErrorMessage = "Ingresa un asentamiento entre 1 y 7 pulgadas.")]
        [Display(Name = "Asentamiento")]
        public decimal? AsentamientoPulg { get; set; } = 4m;

        [Required(ErrorMessage = "Selecciona el TMN.")]
        [Display(Name = "TMN")]
        public string TamanoMaxNominal { get; set; } = "3/4\"";

        [Required(ErrorMessage = "Selecciona la arena.")]
        [Display(Name = "Arena")]
        public int? IdArena { get; set; }

        [Required(ErrorMessage = "Selecciona la piedra.")]
        [Display(Name = "Piedra")]
        public int? IdPiedra { get; set; }

        [Required(ErrorMessage = "Selecciona el criterio.")]
        [Display(Name = "Criterio")]
        public string Criterio { get; set; } = Services.Criterio.Balanceado;

        public IEnumerable<SelectListItem> Arenas { get; set; }
        public IEnumerable<SelectListItem> Piedras { get; set; }
        public IEnumerable<SelectListItem> Proyectos { get; set; }

        public GeneradorResultado Resultado { get; set; }
        public string ErrorCalculo { get; set; }
    }

    /// <summary>Datos para "Guardar como diseño" desde el generador.</summary>
    public class GuardarAlternativaVM
    {
        [Required] public int? FcKgCm2 { get; set; }
        [Required] public decimal? AsentamientoPulg { get; set; }
        [Required] public string TamanoMaxNominal { get; set; }
        [Required] public int? IdArena { get; set; }
        [Required] public int? IdPiedra { get; set; }

        /// <summary>Variante elegida: "{IdCemento}-{0|1}" (1 = con aditivo). Lo envía el botón de cada fila.</summary>
        [Required] public string Seleccion { get; set; }

        public int? IdCemento
        {
            get
            {
                int id;
                var partes = (Seleccion ?? "").Split('-');
                return int.TryParse(partes[0], out id) ? id : (int?)null;
            }
        }

        public bool ConAditivo => (Seleccion ?? "").EndsWith("-1");

        [Required(ErrorMessage = "Selecciona el proyecto donde guardar el diseño.")]
        public int? IdProyecto { get; set; }
    }

    public class InicioVM
    {
        public List<KpiVM> Kpis { get; set; } = new List<KpiVM>();
        public List<DisenoFilaVM> UltimosDisenos { get; set; } = new List<DisenoFilaVM>();
        public List<DisenoFilaVM> PendientesEnsayo { get; set; } = new List<DisenoFilaVM>();
    }
}
