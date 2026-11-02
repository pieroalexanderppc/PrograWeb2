using System;
using System.Collections.Generic;

namespace Concreta.Models.Entities
{
    public class Proyecto
    {
        public int IdProyecto { get; set; }
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Cliente { get; set; }
        public string Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual Usuario Usuario { get; set; }
        public virtual ICollection<DisenoMezcla> Disenos { get; set; } = new HashSet<DisenoMezcla>();
    }

    public class DisenoMezcla
    {
        public const string Borrador = "Borrador";
        public const string Calculado = "Calculado";
        public const string Ensayado = "Ensayado";
        public const string Aprobado = "Aprobado";
        public const string Descartado = "Descartado";

        public int IdDiseno { get; set; }
        public int IdProyecto { get; set; }
        public int IdUsuario { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public int FcKgCm2 { get; set; }
        public int? FcrKgCm2 { get; set; }
        public decimal AsentamientoPulg { get; set; }
        public string TamanoMaxNominal { get; set; }
        public decimal? RelacionAC { get; set; }
        public decimal? AireAtrapado { get; set; }
        public string MetodoDiseno { get; set; } = "ACI 211.1";
        public decimal? CostoTotalM3 { get; set; }
        public decimal? HuellaCO2M3 { get; set; }
        public string Estado { get; set; } = Borrador;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual Proyecto Proyecto { get; set; }
        public virtual Usuario Usuario { get; set; }
        public virtual ICollection<DetalleMezcla> Detalles { get; set; } = new HashSet<DetalleMezcla>();
        public virtual ICollection<EnsayoLaboratorio> Ensayos { get; set; } = new HashSet<EnsayoLaboratorio>();
    }

    public class DetalleMezcla
    {
        public int IdDetalle { get; set; }
        public int IdDiseno { get; set; }
        public int IdMaterial { get; set; }
        public decimal CantidadKgM3 { get; set; }
        public decimal? VolumenAbsolutoM3 { get; set; }
        public decimal? CostoParcial { get; set; }
        public decimal? CO2Parcial { get; set; }

        public virtual DisenoMezcla Diseno { get; set; }
        public virtual Material Material { get; set; }
    }

    public class EnsayoLaboratorio
    {
        public const string Compresion = "Compresion";

        public int IdEnsayo { get; set; }
        public int IdDiseno { get; set; }
        public int IdUsuario { get; set; }
        public string TipoEnsayo { get; set; }
        public int? EdadDias { get; set; }
        public decimal ValorObtenido { get; set; }
        public string Unidad { get; set; }
        public DateTime FechaEnsayo { get; set; }
        public string Observaciones { get; set; }

        public virtual DisenoMezcla Diseno { get; set; }
        public virtual Usuario Usuario { get; set; }
    }

    public class Comparacion
    {
        public int IdComparacion { get; set; }
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Criterio { get; set; }
        public int? IdDisenoRecomendado { get; set; }
        public string Justificacion { get; set; }
        public DateTime FechaComparacion { get; set; } = DateTime.Now;

        public virtual Usuario Usuario { get; set; }
        public virtual DisenoMezcla DisenoRecomendado { get; set; }
        public virtual ICollection<ComparacionDetalle> Detalles { get; set; } = new HashSet<ComparacionDetalle>();
    }

    public class ComparacionDetalle
    {
        public int IdComparacion { get; set; }
        public int IdDiseno { get; set; }
        public decimal? Puntaje { get; set; }
        public int? Posicion { get; set; }
        public bool CumpleResistencia { get; set; }

        public virtual Comparacion Comparacion { get; set; }
        public virtual DisenoMezcla Diseno { get; set; }
    }
}
