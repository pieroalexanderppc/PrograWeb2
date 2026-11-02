using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Seguridad;
using Concreta.Services;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    [Autorizar(RolNombre.Administrador)]
    public class ParametrosController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            return View(await new DisenoDatosService(Db).CargarParametrosAsync());
        }

        public async Task<ActionResult> Edit()
        {
            var p = await new DisenoDatosService(Db).CargarParametrosAsync();
            return View(new ParametrosFormVM
            {
                AguaAire = p.AguaAire.OrderBy(x => x.IdParametro).Select(x => new FilaAguaAireVM
                {
                    IdParametro = x.IdParametro,
                    TamanoMaxNominal = x.TamanoMaxNominal,
                    AsentamientoMin = x.AsentamientoMin,
                    AsentamientoMax = x.AsentamientoMax,
                    AguaLitrosM3 = x.AguaLitrosM3,
                    AireAtrapado = x.AireAtrapado
                }).ToList(),
                RelacionAC = p.RelacionAC.OrderBy(x => x.FcrKgCm2).Select(x => new FilaRelacionAcVM
                {
                    IdParametro = x.IdParametro,
                    FcrKgCm2 = x.FcrKgCm2,
                    RelacionAC = x.RelacionAC
                }).ToList(),
                AgregadoGrueso = p.AgregadoGrueso.OrderBy(x => x.IdParametro).Select(x => new FilaAgregadoVM
                {
                    IdParametro = x.IdParametro,
                    TamanoMaxNominal = x.TamanoMaxNominal,
                    ModuloFineza = x.ModuloFineza,
                    VolumenM3 = x.VolumenM3
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ParametrosFormVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Solo cambian los valores: las claves de cada fila (TMN, rangos, f'cr, MF) son de la tabla ACI.
            var agua = await Db.ParametrosAguaAire.ToDictionaryAsync(x => x.IdParametro);
            foreach (var f in vm.AguaAire)
            {
                if (!agua.ContainsKey(f.IdParametro)) continue;
                agua[f.IdParametro].AguaLitrosM3 = f.AguaLitrosM3.Value;
                agua[f.IdParametro].AireAtrapado = f.AireAtrapado.Value;
            }

            var ac = await Db.ParametrosRelacionAC.ToDictionaryAsync(x => x.IdParametro);
            foreach (var f in vm.RelacionAC)
                if (ac.ContainsKey(f.IdParametro)) ac[f.IdParametro].RelacionAC = f.RelacionAC.Value;

            var grueso = await Db.ParametrosAgregadoGrueso.ToDictionaryAsync(x => x.IdParametro);
            foreach (var f in vm.AgregadoGrueso)
                if (grueso.ContainsKey(f.IdParametro)) grueso[f.IdParametro].VolumenM3 = f.VolumenM3.Value;

            await Db.SaveChangesAsync();
            Exito("Parámetros ACI actualizados");
            return RedirectToAction("Index");
        }
    }
}
