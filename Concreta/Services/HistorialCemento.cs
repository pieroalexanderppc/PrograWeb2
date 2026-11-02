using System.Collections.Generic;
using System.Linq;
using Concreta.Models.Entities;

namespace Concreta.Services
{
    /// <summary>Resistencia real a 28 días que ha logrado un cemento en los diseños ensayados.</summary>
    public class HistorialCemento
    {
        public int Disenos { get; set; }
        /// <summary>Resistencia promedio como % de f'c.</summary>
        public decimal PctFcPromedio { get; set; }
        /// <summary>Peor resultado como % de f'c.</summary>
        public decimal PctFcMinimo { get; set; }

        /// <summary>IdMaterial del cemento → historial. Los disenos deben traer Detalles con Material.TipoMaterial y Ensayos.</summary>
        public static Dictionary<int, HistorialCemento> PorCemento(IEnumerable<DisenoMezcla> disenos)
        {
            var filas = new List<KeyValuePair<int, decimal>>();
            foreach (var d in disenos)
            {
                var r28 = DisenoEstados.Resistencia28d(d);
                var cemento = d.Detalles.FirstOrDefault(x => x.Material.TipoMaterial.Nombre == TipoMaterial.Cemento);
                if (r28 == null || cemento == null) continue;
                filas.Add(new KeyValuePair<int, decimal>(cemento.IdMaterial, r28.Value / d.FcKgCm2 * 100m));
            }

            return filas.GroupBy(f => f.Key).ToDictionary(g => g.Key, g => new HistorialCemento
            {
                Disenos = g.Count(),
                PctFcPromedio = g.Average(f => f.Value),
                PctFcMinimo = g.Min(f => f.Value)
            });
        }
    }
}
