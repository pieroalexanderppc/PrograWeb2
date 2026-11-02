using System;
using System.Collections.Generic;
using System.Linq;
using Concreta.Models.Entities;

namespace Concreta.Services
{
    /// <summary>Tablas ACI 211.1 cargadas desde la base.</summary>
    public class ParametrosAci
    {
        public IList<ParametroAguaAire> AguaAire { get; set; } = new List<ParametroAguaAire>();
        public IList<ParametroRelacionAC> RelacionAC { get; set; } = new List<ParametroRelacionAC>();
        public IList<ParametroAgregadoGrueso> AgregadoGrueso { get; set; } = new List<ParametroAgregadoGrueso>();
    }

    public class DisenoAciEntrada
    {
        public int FcKgCm2 { get; set; }
        public decimal AsentamientoPulg { get; set; }
        public string TamanoMaxNominal { get; set; }

        public Material Cemento { get; set; }
        public Material Arena { get; set; }
        public Material Piedra { get; set; }
        public Material Agua { get; set; }

        /// <summary>Aditivo opcional.</summary>
        public Material Aditivo { get; set; }
        /// <summary>Dosis fija del aditivo en kg/m³.</summary>
        public decimal? AditivoDosisKg { get; set; }
        /// <summary>Dosis como % del peso del cemento (se usa si no hay dosis en kg).</summary>
        public decimal? AditivoDosisPctCemento { get; set; }
        /// <summary>Reducción del agua de mezcla por el aditivo, en %.</summary>
        public decimal ReduccionAguaPct { get; set; }
    }

    public static class TipoLinea
    {
        public const string Cemento = "Cemento";
        public const string Agua = "Agua";
        public const string Arena = "Arena";
        public const string Piedra = "Piedra";
        public const string Aditivo = "Aditivo";

        public static string DesdeTipoMaterial(string tipoMaterial)
        {
            switch (tipoMaterial)
            {
                case TipoMaterial.Cemento: return Cemento;
                case TipoMaterial.Agua: return Agua;
                case TipoMaterial.AgregadoFino: return Arena;
                case TipoMaterial.AgregadoGrueso: return Piedra;
                default: return Aditivo;
            }
        }
    }

    /// <summary>Una fila de la dosificación por m³.</summary>
    public class LineaDosificacion
    {
        public string Tipo { get; set; }
        public Material Material { get; set; }
        public decimal CantidadKg { get; set; }
        public decimal VolumenAbsolutoM3 { get; set; }
        public decimal CostoParcial { get; set; }
        public decimal CO2Parcial { get; set; }

        public static LineaDosificacion DesdeDetalle(DetalleMezcla d)
        {
            return new LineaDosificacion
            {
                Tipo = TipoLinea.DesdeTipoMaterial(d.Material.TipoMaterial.Nombre),
                Material = d.Material,
                CantidadKg = d.CantidadKgM3,
                VolumenAbsolutoM3 = d.VolumenAbsolutoM3
                    ?? Math.Round(d.CantidadKgM3 / (d.Material.PesoEspecifico * 1000m), 4, MidpointRounding.AwayFromZero),
                CostoParcial = d.CostoParcial ?? 0m,
                CO2Parcial = d.CO2Parcial ?? 0m
            };
        }
    }

    public class DisenoAciResultado
    {
        public int FcKgCm2 { get; set; }
        public int FcrKgCm2 { get; set; }
        public decimal AsentamientoPulg { get; set; }
        public string TamanoMaxNominal { get; set; }
        public decimal AguaLitros { get; set; }
        public decimal AireAtrapadoPct { get; set; }
        public decimal RelacionAC { get; set; }
        public decimal VolumenAgregadoGrueso { get; set; }
        public decimal VolumenAireM3 { get; set; }

        public List<LineaDosificacion> Lineas { get; set; } = new List<LineaDosificacion>();
        public List<string> Advertencias { get; set; } = new List<string>();

        public decimal CostoTotalM3 { get; set; }
        public decimal HuellaCO2M3 { get; set; }

        public LineaDosificacion Linea(string tipo) => Lineas.FirstOrDefault(l => l.Tipo == tipo);
        public decimal CementoKg => Linea(TipoLinea.Cemento)?.CantidadKg ?? 0m;
    }

    /// <summary>Error de cálculo con un mensaje claro para el usuario.</summary>
    public class DisenoAciException : Exception
    {
        public DisenoAciException(string mensaje) : base(mensaje) { }
    }
}
