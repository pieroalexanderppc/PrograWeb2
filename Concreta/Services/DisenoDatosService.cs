using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Concreta.Data;
using Concreta.Models.Entities;

namespace Concreta.Services
{
    /// <summary>Lectura de tablas ACI y guardado de diseños calculados.</summary>
    public class DisenoDatosService
    {
        private readonly ConcretaDbContext _db;

        public DisenoDatosService(ConcretaDbContext db)
        {
            _db = db;
        }

        public async Task<ParametrosAci> CargarParametrosAsync()
        {
            return new ParametrosAci
            {
                AguaAire = await _db.ParametrosAguaAire.AsNoTracking().ToListAsync(),
                RelacionAC = await _db.ParametrosRelacionAC.AsNoTracking().ToListAsync(),
                AgregadoGrueso = await _db.ParametrosAgregadoGrueso.AsNoTracking().ToListAsync()
            };
        }

        public Task<Material> MaterialAsync(int? id)
        {
            if (id == null) return Task.FromResult<Material>(null);
            return _db.Materiales.Include(m => m.TipoMaterial).FirstOrDefaultAsync(m => m.IdMaterial == id.Value);
        }

        /// <summary>DM-{año}-{correlativo de 4 dígitos}.</summary>
        public async Task<string> SiguienteCodigoAsync()
        {
            string prefijo = "DM-" + DateTime.Now.Year + "-";
            var codigos = await _db.Disenos.Where(d => d.Codigo.StartsWith(prefijo)).Select(d => d.Codigo).ToListAsync();
            int max = 0;
            foreach (var c in codigos)
            {
                int n;
                if (int.TryParse(c.Substring(prefijo.Length), out n) && n > max) max = n;
            }
            return prefijo + (max + 1).ToString("0000");
        }

        public async Task<DisenoMezcla> GuardarAsync(DisenoAciResultado r, int idProyecto, int idUsuario, string descripcion)
        {
            var diseno = new DisenoMezcla
            {
                IdProyecto = idProyecto,
                IdUsuario = idUsuario,
                Codigo = await SiguienteCodigoAsync(),
                Descripcion = descripcion,
                FcKgCm2 = r.FcKgCm2,
                FcrKgCm2 = r.FcrKgCm2,
                AsentamientoPulg = r.AsentamientoPulg,
                TamanoMaxNominal = r.TamanoMaxNominal,
                RelacionAC = r.RelacionAC,
                AireAtrapado = r.AireAtrapadoPct,
                CostoTotalM3 = r.CostoTotalM3,
                HuellaCO2M3 = r.HuellaCO2M3,
                Estado = DisenoMezcla.Calculado,
                FechaCreacion = DateTime.Now
            };

            foreach (var l in r.Lineas)
            {
                diseno.Detalles.Add(new DetalleMezcla
                {
                    IdMaterial = l.Material.IdMaterial,
                    CantidadKgM3 = l.CantidadKg,
                    VolumenAbsolutoM3 = l.VolumenAbsolutoM3,
                    CostoParcial = l.CostoParcial,
                    CO2Parcial = l.CO2Parcial
                });
            }

            _db.Disenos.Add(diseno);
            await _db.SaveChangesAsync();
            return diseno;
        }

        /// <summary>Último ensayo de compresión a 28 días de cada diseño.</summary>
        public static decimal? Resistencia28d(DisenoMezcla d)
        {
            return d.Ensayos
                .Where(e => e.TipoEnsayo == EnsayoLaboratorio.Compresion && e.EdadDias == 28)
                .OrderByDescending(e => e.FechaEnsayo)
                .Select(e => (decimal?)e.ValorObtenido)
                .FirstOrDefault();
        }
    }
}
