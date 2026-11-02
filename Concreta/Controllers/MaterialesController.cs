using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Models.Entities;
using Concreta.Seguridad;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    [Autorizar(RolNombre.Administrador)]
    public class MaterialesController : BaseController
    {
        public async Task<ActionResult> Index(string q, int? tipo)
        {
            var consulta = Db.Materiales.Include(m => m.TipoMaterial).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                consulta = consulta.Where(m => m.Nombre.Contains(q) || m.Proveedor.Contains(q));
            if (tipo != null)
                consulta = consulta.Where(m => m.IdTipoMaterial == tipo);

            ViewBag.Q = q;
            ViewBag.Tipo = tipo;
            ViewBag.Tipos = await Db.TiposMaterial.OrderBy(t => t.IdTipoMaterial).ToListAsync();
            var lista = await consulta.OrderBy(m => m.IdTipoMaterial).ThenBy(m => m.Nombre).ToListAsync();
            return View(lista);
        }

        public async Task<ActionResult> Create()
        {
            var vm = new MaterialFormVM();
            await LlenarListas(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MaterialFormVM vm)
        {
            await LlenarListas(vm);
            ValidarSegunTipo(vm);
            if (!ModelState.IsValid) return View(vm);

            var m = new Material();
            Copiar(vm, m);
            Db.Materiales.Add(m);
            await Db.SaveChangesAsync();
            Exito("Material \"" + m.Nombre + "\" creado");
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int id)
        {
            var m = await Db.Materiales.FindAsync(id);
            if (m == null) return HttpNotFound();

            var vm = new MaterialFormVM
            {
                IdMaterial = m.IdMaterial,
                IdTipoMaterial = m.IdTipoMaterial,
                Nombre = m.Nombre,
                Proveedor = m.Proveedor,
                PesoEspecifico = m.PesoEspecifico,
                PorcentajeAdicion = m.PorcentajeAdicion,
                ModuloFineza = m.ModuloFineza,
                TamanoMaxNominal = m.TamanoMaxNominal,
                PesoUnitarioCompactado = m.PesoUnitarioCompactado,
                Absorcion = m.Absorcion,
                ContenidoHumedad = m.ContenidoHumedad,
                CostoKg = m.CostoKg,
                FactorCO2Kg = m.FactorCO2Kg,
                Activo = m.Activo
            };
            await LlenarListas(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, MaterialFormVM vm)
        {
            var m = await Db.Materiales.FindAsync(id);
            if (m == null) return HttpNotFound();

            await LlenarListas(vm);
            ValidarSegunTipo(vm);
            if (!ModelState.IsValid) return View(vm);

            Copiar(vm, m);
            await Db.SaveChangesAsync();
            Exito("Material \"" + m.Nombre + "\" actualizado");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var m = await Db.Materiales.FindAsync(id);
            if (m == null) return HttpNotFound();

            bool enUso = await Db.DetallesMezcla.AnyAsync(d => d.IdMaterial == id);
            if (enUso)
            {
                m.Activo = false;
                await Db.SaveChangesAsync();
                Exito("\"" + m.Nombre + "\" se usa en diseños guardados, así que se desactivó en lugar de eliminarse");
            }
            else
            {
                Db.Materiales.Remove(m);
                await Db.SaveChangesAsync();
                Exito("Material \"" + m.Nombre + "\" eliminado");
            }
            return RedirectToAction("Index");
        }

        private async Task LlenarListas(MaterialFormVM vm)
        {
            var tipos = await Db.TiposMaterial.OrderBy(t => t.IdTipoMaterial).ToListAsync();
            vm.Tipos = tipos.Select(t => new SelectListItem { Value = t.IdTipoMaterial.ToString(), Text = t.Nombre });
            vm.NombresTipo = tipos.ToDictionary(t => t.IdTipoMaterial, t => t.Nombre);
        }

        private string NombreTipo(MaterialFormVM vm)
        {
            string nombre;
            return vm.IdTipoMaterial != null && vm.NombresTipo.TryGetValue(vm.IdTipoMaterial.Value, out nombre) ? nombre : null;
        }

        private void ValidarSegunTipo(MaterialFormVM vm)
        {
            switch (NombreTipo(vm))
            {
                case TipoMaterial.Cemento:
                    if (vm.PorcentajeAdicion == null)
                        ModelState.AddModelError("PorcentajeAdicion", "Ingresa el % de adición del cemento (0 para Tipo I).");
                    break;
                case TipoMaterial.AgregadoFino:
                    if (vm.ModuloFineza == null)
                        ModelState.AddModelError("ModuloFineza", "Ingresa el módulo de fineza de la arena.");
                    break;
                case TipoMaterial.AgregadoGrueso:
                    if (string.IsNullOrEmpty(vm.TamanoMaxNominal))
                        ModelState.AddModelError("TamanoMaxNominal", "Selecciona el tamaño máximo nominal de la piedra.");
                    if (vm.PesoUnitarioCompactado == null)
                        ModelState.AddModelError("PesoUnitarioCompactado", "Ingresa el peso unitario compactado de la piedra.");
                    break;
            }
        }

        /// <summary>Copia el formulario a la entidad y limpia los campos que no aplican al tipo.</summary>
        private void Copiar(MaterialFormVM vm, Material m)
        {
            string tipo = NombreTipo(vm);
            bool esAgregado = tipo == TipoMaterial.AgregadoFino || tipo == TipoMaterial.AgregadoGrueso;

            m.IdTipoMaterial = vm.IdTipoMaterial.Value;
            m.Nombre = vm.Nombre.Trim();
            m.Proveedor = string.IsNullOrWhiteSpace(vm.Proveedor) ? null : vm.Proveedor.Trim();
            m.PesoEspecifico = vm.PesoEspecifico.Value;
            m.PorcentajeAdicion = tipo == TipoMaterial.Cemento ? vm.PorcentajeAdicion : null;
            m.ModuloFineza = tipo == TipoMaterial.AgregadoFino ? vm.ModuloFineza : null;
            m.TamanoMaxNominal = tipo == TipoMaterial.AgregadoGrueso ? vm.TamanoMaxNominal : null;
            m.PesoUnitarioCompactado = tipo == TipoMaterial.AgregadoGrueso ? vm.PesoUnitarioCompactado : null;
            m.Absorcion = esAgregado ? vm.Absorcion : null;
            m.ContenidoHumedad = esAgregado ? vm.ContenidoHumedad : null;
            m.CostoKg = vm.CostoKg.Value;
            m.FactorCO2Kg = vm.FactorCO2Kg.Value;
            m.Activo = vm.Activo;
        }
    }
}
