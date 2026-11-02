using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Models.Entities;
using Concreta.Seguridad;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    [Autorizar(RolNombre.AdminIngeniero)]
    public class ProyectosController : BaseController
    {
        public async Task<ActionResult> Index(string q)
        {
            var consulta = Db.Proyectos.Include(p => p.Usuario).Include(p => p.Disenos).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                consulta = consulta.Where(p => p.Nombre.Contains(q) || p.Cliente.Contains(q) || p.Ubicacion.Contains(q));
            ViewBag.Q = q;
            return View(await consulta.OrderByDescending(p => p.FechaCreacion).ToListAsync());
        }

        public ActionResult Create()
        {
            return View(new ProyectoFormVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProyectoFormVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var p = new Proyecto { IdUsuario = IdUsuarioActual };
            Copiar(vm, p);
            Db.Proyectos.Add(p);
            await Db.SaveChangesAsync();
            Exito("Proyecto \"" + p.Nombre + "\" creado");
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int id)
        {
            var p = await Db.Proyectos.FindAsync(id);
            if (p == null) return HttpNotFound();
            return View(new ProyectoFormVM
            {
                IdProyecto = p.IdProyecto,
                Nombre = p.Nombre,
                Cliente = p.Cliente,
                Ubicacion = p.Ubicacion,
                Descripcion = p.Descripcion
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, ProyectoFormVM vm)
        {
            var p = await Db.Proyectos.FindAsync(id);
            if (p == null) return HttpNotFound();
            if (!ModelState.IsValid) return View(vm);

            Copiar(vm, p);
            await Db.SaveChangesAsync();
            Exito("Proyecto \"" + p.Nombre + "\" actualizado");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var p = await Db.Proyectos.FindAsync(id);
            if (p == null) return HttpNotFound();

            if (await Db.Disenos.AnyAsync(d => d.IdProyecto == id))
            {
                Error("No se puede eliminar \"" + p.Nombre + "\" porque tiene diseños registrados.");
                return RedirectToAction("Index");
            }

            Db.Proyectos.Remove(p);
            await Db.SaveChangesAsync();
            Exito("Proyecto \"" + p.Nombre + "\" eliminado");
            return RedirectToAction("Index");
        }

        private static void Copiar(ProyectoFormVM vm, Proyecto p)
        {
            p.Nombre = vm.Nombre.Trim();
            p.Cliente = Limpiar(vm.Cliente);
            p.Ubicacion = Limpiar(vm.Ubicacion);
            p.Descripcion = Limpiar(vm.Descripcion);
        }

        private static string Limpiar(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
    }
}
