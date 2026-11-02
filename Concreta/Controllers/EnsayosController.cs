using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Models.Entities;
using Concreta.Seguridad;
using Concreta.Services;
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
            var d = await DisenoConEnsayos(vm.IdDiseno);
            if (d == null) return HttpNotFound();
            vm.CodigoDiseno = d.Codigo;

            Validar(vm);
            if (!ModelState.IsValid) return View(vm);

            var ensayo = new EnsayoLaboratorio { IdDiseno = d.IdDiseno, IdUsuario = IdUsuarioActual };
            Copiar(vm, ensayo);
            d.Ensayos.Add(ensayo);
            DisenoEstados.Sincronizar(d);

            await Db.SaveChangesAsync();
            Exito("Ensayo registrado en " + d.Codigo);
            return RedirectToAction("Details", "Disenos", new { id = d.IdDiseno });
        }

        [Autorizar(RolNombre.AdminLaboratorista)]
        public async Task<ActionResult> Edit(int id)
        {
            var e = await Db.Ensayos.Include(x => x.Diseno).FirstOrDefaultAsync(x => x.IdEnsayo == id);
            if (e == null) return HttpNotFound();
            return View(new EnsayoFormVM
            {
                IdEnsayo = e.IdEnsayo,
                IdDiseno = e.IdDiseno,
                CodigoDiseno = e.Diseno.Codigo,
                TipoEnsayo = e.TipoEnsayo,
                EdadDias = e.EdadDias,
                ValorObtenido = e.ValorObtenido,
                FechaEnsayo = e.FechaEnsayo,
                Observaciones = e.Observaciones
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Autorizar(RolNombre.AdminLaboratorista)]
        public async Task<ActionResult> Edit(int id, EnsayoFormVM vm)
        {
            var e = await Db.Ensayos.FindAsync(id);
            if (e == null) return HttpNotFound();
            var d = await DisenoConEnsayos(e.IdDiseno);
            vm.IdEnsayo = id;
            vm.IdDiseno = d.IdDiseno;
            vm.CodigoDiseno = d.Codigo;

            Validar(vm);
            if (!ModelState.IsValid) return View(vm);

            Copiar(vm, e);
            DisenoEstados.Sincronizar(d);

            await Db.SaveChangesAsync();
            Exito("Ensayo actualizado en " + d.Codigo);
            return RedirectToAction("Details", "Disenos", new { id = d.IdDiseno });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Autorizar(RolNombre.AdminLaboratorista)]
        public async Task<ActionResult> Delete(int id)
        {
            var e = await Db.Ensayos.FindAsync(id);
            if (e == null) return HttpNotFound();
            var d = await DisenoConEnsayos(e.IdDiseno);

            d.Ensayos.Remove(e);
            Db.Ensayos.Remove(e);
            DisenoEstados.Sincronizar(d);

            await Db.SaveChangesAsync();
            Exito("Ensayo eliminado de " + d.Codigo);
            return RedirectToAction("Details", "Disenos", new { id = d.IdDiseno });
        }

        private Task<DisenoMezcla> DisenoConEnsayos(int id)
        {
            return Db.Disenos.Include(x => x.Ensayos).FirstOrDefaultAsync(x => x.IdDiseno == id);
        }

        private void Validar(EnsayoFormVM vm)
        {
            if (!Opciones.TiposEnsayo.Contains(vm.TipoEnsayo))
                ModelState.AddModelError("TipoEnsayo", "Selecciona un tipo de ensayo válido.");
            if (vm.TipoEnsayo == EnsayoLaboratorio.Compresion && vm.EdadDias == null)
                ModelState.AddModelError("EdadDias", "Selecciona la edad del ensayo de compresión.");
            if (vm.EdadDias != null && !Opciones.Edades.Contains(vm.EdadDias.Value))
                ModelState.AddModelError("EdadDias", "La edad debe ser 3, 7, 14, 28 o 56 días.");
        }

        private static void Copiar(EnsayoFormVM vm, EnsayoLaboratorio e)
        {
            e.TipoEnsayo = vm.TipoEnsayo;
            e.EdadDias = vm.EdadDias;
            e.ValorObtenido = vm.ValorObtenido.Value;
            e.Unidad = Opciones.UnidadEnsayo(vm.TipoEnsayo);
            e.FechaEnsayo = vm.FechaEnsayo.Value.Date;
            e.Observaciones = string.IsNullOrWhiteSpace(vm.Observaciones) ? null : vm.Observaciones.Trim();
        }
    }
}
