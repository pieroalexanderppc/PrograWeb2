using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Concreta.Models.Entities;
using Concreta.Services;

namespace Concreta.ViewModels
{
    /// <summary>Formulario de "Nuevo diseño de mezcla".</summary>
    public class DisenoFormVM
    {
        [Required(ErrorMessage = "Selecciona el proyecto.")]
        [Display(Name = "Proyecto")]
        public int? IdProyecto { get; set; }

        [StringLength(200, ErrorMessage = "La descripción admite hasta 200 caracteres.")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "Ingresa el f'c.")]
        [Range(100, 700, ErrorMessage = "Ingresa un f'c entre 100 y 700 kg/cm².")]
        [Display(Name = "f'c")]
        public int? FcKgCm2 { get; set; } = 210;

        [Required(ErrorMessage = "Ingresa el asentamiento.")]
        [Range(typeof(decimal), "1", "7", ErrorMessage = "Ingresa un asentamiento entre 1 y 7 pulgadas.")]
        [Display(Name = "Asentamiento")]
        public decimal? AsentamientoPulg { get; set; } = 4m;

        [Required(ErrorMessage = "Selecciona el TMN.")]
        [Display(Name = "Tamaño máximo nominal")]
        public string TamanoMaxNominal { get; set; } = "3/4\"";

        [Required(ErrorMessage = "Selecciona el cemento.")]
        [Display(Name = "Cemento")]
        public int? IdCemento { get; set; }

        [Required(ErrorMessage = "Selecciona la arena.")]
        [Display(Name = "Agregado fino (arena)")]
        public int? IdArena { get; set; }

        [Required(ErrorMessage = "Selecciona la piedra.")]
        [Display(Name = "Agregado grueso (piedra)")]
        public int? IdPiedra { get; set; }

        [Required(ErrorMessage = "Selecciona el agua.")]
        [Display(Name = "Agua")]
        public int? IdAgua { get; set; }

        [Display(Name = "Aditivo (opcional)")]
        public int? IdAditivo { get; set; }

        [Range(typeof(decimal), "0.01", "50", ErrorMessage = "Ingresa una dosis de aditivo entre 0.01 y 50 kg/m³.")]
        [Display(Name = "Dosis de aditivo")]
        public decimal? AditivoDosisKg { get; set; }

        // ----- Listas para los selects -----
        public IEnumerable<SelectListItem> Proyectos { get; set; }
        public List<Material> Cementos { get; set; } = new List<Material>();
        public List<Material> Arenas { get; set; } = new List<Material>();
        public List<Material> Piedras { get; set; } = new List<Material>();
        public List<Material> Aguas { get; set; } = new List<Material>();
        public List<Material> Aditivos { get; set; } = new List<Material>();

        // ----- Resultado del cálculo -----
        public DisenoAciResultado Resultado { get; set; }
        public DosificacionVM Dosificacion { get; set; }
        public string ErrorCalculo { get; set; }
    }

    public class EnsayoFormVM
    {
        [Required]
        public int IdDiseno { get; set; }

        [Required(ErrorMessage = "Selecciona el tipo de ensayo.")]
        [Display(Name = "Tipo de ensayo")]
        public string TipoEnsayo { get; set; } = EnsayoLaboratorio.Compresion;

        [Display(Name = "Edad")]
        public int? EdadDias { get; set; } = 28;

        [Required(ErrorMessage = "Ingresa el valor obtenido.")]
        [Range(typeof(decimal), "0", "99999", ErrorMessage = "El valor no puede ser negativo.")]
        [Display(Name = "Valor obtenido")]
        public decimal? ValorObtenido { get; set; }

        [Required(ErrorMessage = "Ingresa la fecha del ensayo.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha del ensayo")]
        public DateTime? FechaEnsayo { get; set; } = DateTime.Today;

        [StringLength(250, ErrorMessage = "Las observaciones admiten hasta 250 caracteres.")]
        [Display(Name = "Observaciones")]
        public string Observaciones { get; set; }

        public string CodigoDiseno { get; set; }
    }

    public class DisenoDetalleVM
    {
        public DisenoMezcla Diseno { get; set; }
        public DosificacionVM Dosificacion { get; set; }
        public List<KpiVM> Kpis { get; set; } = new List<KpiVM>();
        public decimal? Resistencia28d { get; set; }
        public EstadoResistencia EstadoResistencia { get; set; }
        public EnsayoFormVM NuevoEnsayo { get; set; }
        public bool PuedeRegistrarEnsayo { get; set; }
        public bool PuedeEditar { get; set; }
    }

    public class DisenoFilaVM
    {
        public int IdDiseno { get; set; }
        public string Codigo { get; set; }
        public string Proyecto { get; set; }
        public int FcKgCm2 { get; set; }
        public string Cemento { get; set; }
        public decimal? CementoKg { get; set; }
        public decimal? Costo { get; set; }
        public decimal? CO2 { get; set; }
        public string Estado { get; set; }
        public decimal? Resistencia28d { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
