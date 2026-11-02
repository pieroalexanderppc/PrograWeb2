using System.Linq;
using Concreta.Models.Entities;

namespace Concreta.Services
{
    /// <summary>Reglas de resistencia y de estado de un diseño. No accede a la base.</summary>
    public static class DisenoEstados
    {
        /// <summary>Promedio de las probetas de compresión a 28 días; null si no hay ensayos.</summary>
        public static decimal? Resistencia28d(DisenoMezcla d)
        {
            var valores = d.Ensayos
                .Where(e => e.TipoEnsayo == EnsayoLaboratorio.Compresion && e.EdadDias == 28)
                .Select(e => e.ValorObtenido).ToList();
            return valores.Count == 0 ? (decimal?)null : System.Math.Round(valores.Average(), 2);
        }

        public static EstadoResistencia Resistencia(DisenoMezcla d)
        {
            return RecomendacionService.EvaluarResistencia(d.FcKgCm2, Resistencia28d(d));
        }

        public static bool PuedeAprobar(DisenoMezcla d)
        {
            return Resistencia(d) == EstadoResistencia.Cumple;
        }

        /// <summary>Estado que corresponde según sus ensayos, sin aprobar ni descartar.</summary>
        public static string EstadoBase(DisenoMezcla d)
        {
            return Resistencia28d(d) != null ? DisenoMezcla.Ensayado : DisenoMezcla.Calculado;
        }

        /// <summary>Se llama cuando cambian los ensayos. Un diseño aprobado que deja de cumplir pierde la aprobación.</summary>
        public static void Sincronizar(DisenoMezcla d)
        {
            if (d.Estado == DisenoMezcla.Descartado) return;
            if (d.Estado == DisenoMezcla.Aprobado && PuedeAprobar(d)) return;
            d.Estado = EstadoBase(d);
        }
    }
}
