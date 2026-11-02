using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Models.Entities;
using Concreta.Seguridad;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    public class EnsayosController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            var ensayos = await Db.Ensayos.Include(e => e.Diseno).Include(e => e.Usuario)
                .OrderByDescending(e => e.FechaEnsayo).ThenByDescending(e => e.IdEnsayo).ToListAsync();
            return View(ensayos);
        }

        [Autorizar(RolNombre.AdminLaboratorista)]
        public async Task<ActionResult> Create(int idDiseno)
        {
            var d = await Db.Disenos.FindAsync(idDiseno);
            if (d == null) return HttpNotFound();
            return View(new EnsayoFormVM { IdDiseno = d.IdDiseno, CodigoDiseno = d.Codigo });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Autorizar(RolNombre.AdminLaboratorista)]
        public async Task<ActionResult> Create(EnsayoFormVM vm)
        {
            var d = await Db.Disenos.FindAsync(vm.IdDiseno);
            if (d == null) return HttpNotFound();
            vm.CodigoDiseno = d.Codigo;

            if (!Opciones.TiposEnsayo.Contains(vm.TipoEnsayo))
                ModelState.AddModelError("TipoEnsayo", "Selecciona un tipo de ensayo válido.");
            if (vm.TipoEnsayo == EnsayoLaboratorio.Compresion && vm.EdadDias == null)
                ModelState.AddModelError("EdadDias", "Selecciona la edad del ensayo de compresión.");
            if (vm.EdadDias != null && !Opciones.Edades.Contains(vm.EdadDias.Value))
                ModelState.AddModelError("EdadDias", "La edad debe ser 3, 7, 14, 28 o 56 días.");
            if (!ModelState.IsValid) return View(vm);

            Db.Ensayos.Add(new EnsayoLaboratorio
            {
                IdDiseno = d.IdDiseno,
                IdUsuario = IdUsuarioActual,
                TipoEnsayo = vm.TipoEnsayo,
                EdadDias = vm.EdadDias,
                ValorObtenido = vm.ValorObtenido.Value,
                Unidad = Opciones.UnidadEnsayo(vm.TipoEnsayo),
                FechaEnsayo = vm.FechaEnsayo.Value.Date,
                Observaciones = string.IsNullOrWhiteSpace(vm.Observaciones) ? null : vm.Observaciones.Trim()
            });

            bool compresion28 = vm.TipoEnsayo == EnsayoLaboratorio.Compresion && vm.EdadDias == 28;
            if (compresion28 && (d.Estado == DisenoMezcla.Borrador || d.Estado == DisenoMezcla.Calculado))
                d.Estado = DisenoMezcla.Ensayado;

            await Db.SaveChangesAsync();
            Exito("Ensayo registrado en " + d.Codigo);
            return RedirectToAction("Details", "Disenos", new { id = d.IdDiseno });
        }
    }
}
