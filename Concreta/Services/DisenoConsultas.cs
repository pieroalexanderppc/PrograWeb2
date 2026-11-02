using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Concreta.Models.Entities;
using Concreta.ViewModels;

namespace Concreta.Services
{
    /// <summary>Filas de diseño para los listados (Inicio, Diseños, Comparar).</summary>
    public static class DisenoConsultas
    {
        public static IQueryable<DisenoMezcla> ConDetalle(this IQueryable<DisenoMezcla> q)
        {
            return q.Include(d => d.Proyecto)
                    .Include(d => d.Detalles.Select(x => x.Material.TipoMaterial))
                    .Include(d => d.Ensayos);
        }

        public static async Task<List<DisenoFilaVM>> FilasAsync(IQueryable<DisenoMezcla> q)
        {
            var disenos = await q.ConDetalle().ToListAsync();
            return disenos.Select(Fila).ToList();
        }

        public static DisenoFilaVM Fila(DisenoMezcla d)
        {
            var cemento = d.Detalles.FirstOrDefault(x => x.Material.TipoMaterial.Nombre == TipoMaterial.Cemento);
            return new DisenoFilaVM
            {
                IdDiseno = d.IdDiseno,
                Codigo = d.Codigo,
                Proyecto = d.Proyecto.Nombre,
                FcKgCm2 = d.FcKgCm2,
                Cemento = cemento?.Material.Nombre,
                CementoKg = cemento?.CantidadKgM3,
                Costo = d.CostoTotalM3,
                CO2 = d.HuellaCO2M3,
                Estado = d.Estado,
                Resistencia28d = DisenoEstados.Resistencia28d(d),
                FechaCreacion = d.FechaCreacion
            };
        }

        public static DosificacionVM Dosificacion(DisenoMezcla d)
        {
            var orden = new[] { TipoLinea.Cemento, TipoLinea.Agua, TipoLinea.Arena, TipoLinea.Piedra, TipoLinea.Aditivo };
            var lineas = d.Detalles.Select(LineaDosificacion.DesdeDetalle)
                .OrderBy(l => System.Array.IndexOf(orden, l.Tipo)).ToList();
            return DosificacionVM.Desde(lineas, d.AireAtrapado ?? 0m);
        }
    }
}
