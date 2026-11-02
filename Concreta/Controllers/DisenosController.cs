using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Helpers;
using Concreta.Models.Entities;
using Concreta.Seguridad;
using Concreta.Services;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    public class DisenosController : BaseController
    {
        private readonly IDisenoAciService _aci = new DisenoAciService();
        private readonly ICostoCO2Service _costo = new CostoCO2Service();

        public async Task<ActionResult> Index(int? proyecto, string estado)
        {
            var q = Db.Disenos.AsQueryable();
            if (proyecto != null) q = q.Where(d => d.IdProyecto == proyecto);
            if (!string.IsNullOrEmpty(estado)) q = q.Where(d => d.Estado == estado);

            ViewBag.Proyecto = proyecto;
            ViewBag.Estado = estado;
            ViewBag.Proyectos = await Db.Proyectos.OrderBy(p => p.Nombre).ToListAsync();
            var filas = await DisenoConsultas.FilasAsync(q);
            return View(filas.OrderByDescending(d => d.FechaCreacion).ThenByDescending(d => d.IdDiseno).ToList());
        }

        [Autorizar(RolNombre.AdminIngeniero)]
        public async Task<ActionResult> Create(int? idProyecto)
        {
            var vm = new DisenoFormVM { IdProyecto = idProyecto };
            await LlenarListas(vm);
            // Valores iniciales: el primer material activo de cada tipo.
            vm.IdCemento = vm.Cementos.Select(m => (int?)m.IdMaterial).FirstOrDefault();
            vm.IdArena = vm.Arenas.Select(m => (int?)m.IdMaterial).FirstOrDefault();
            vm.IdPiedra = vm.Piedras.Select(m => (int?)m.IdMaterial).FirstOrDefault();
            vm.IdAgua = vm.Aguas.Select(m => (int?)m.IdMaterial).FirstOrDefault();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Autorizar(RolNombre.AdminIngeniero)]
        public async Task<ActionResult> Create(DisenoFormVM vm, string accion)
        {
            await LlenarListas(vm);
            if (vm.IdAditivo != null && vm.AditivoDosisKg == null)
                ModelState.AddModelError("AditivoDosisKg", "Ingresa la dosis del aditivo en kg/m³.");
            if (!ModelState.IsValid) return View(vm);

            DisenoAciResultado resultado;
            try
            {
                resultado = await Calcular(vm);
            }
            catch (DisenoAciException ex)
            {
                vm.ErrorCalculo = ex.Message;
                return View(vm);
            }

            if (accion == "guardar")
            {
                var datos = new DisenoDatosService(Db);
                var diseno = await datos.GuardarAsync(resultado, vm.IdProyecto.Value, IdUsuarioActual, vm.Descripcion);
                Exito("Diseño " + diseno.Codigo + " guardado");
                return RedirectToAction("Details", new { id = diseno.IdDiseno });
            }

            ModelState.Clear();
            vm.Resultado = resultado;
            vm.Dosificacion = DosificacionVM.Desde(resultado.Lineas, resultado.AireAtrapadoPct);
            return View(vm);
        }

        public async Task<ActionResult> Details(int id)
        {
            var d = await Db.Disenos.ConDetalle()
                .Include(x => x.Usuario)
                .Include(x => x.Ensayos.Select(e => e.Usuario))
                .FirstOrDefaultAsync(x => x.IdDiseno == id);
            if (d == null) return HttpNotFound();

            var vm = new DisenoDetalleVM
            {
                Diseno = d,
                Dosificacion = DisenoConsultas.Dosificacion(d),
                Resistencia28d = DisenoDatosService.Resistencia28d(d),
                PuedeRegistrarEnsayo = User.IsInRole(RolNombre.Administrador) || User.IsInRole(RolNombre.Laboratorista),
                PuedeEditar = User.IsInRole(RolNombre.Administrador) || User.IsInRole(RolNombre.Ingeniero),
                NuevoEnsayo = new EnsayoFormVM { IdDiseno = d.IdDiseno, CodigoDiseno = d.Codigo }
            };
            vm.EstadoResistencia = RecomendacionService.EvaluarResistencia(d.FcKgCm2, vm.Resistencia28d);

            var cemento = vm.Dosificacion.Lineas.FirstOrDefault(l => l.Tipo == TipoLinea.Cemento);
            vm.Kpis.Add(new KpiVM("Relación a/c", Formato.AC(d.RelacionAC)));
            vm.Kpis.Add(new KpiVM("Cemento", Formato.Kg(cemento?.CantidadKg), "kg/m³"));
            vm.Kpis.Add(new KpiVM("Costo", Formato.Soles(d.CostoTotalM3), "/m³", "S/"));
            vm.Kpis.Add(new KpiVM("Huella CO₂", Formato.CO2(d.HuellaCO2M3), "kg CO₂e/m³"));
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Autorizar(RolNombre.AdminIngeniero)]
        public async Task<ActionResult> CambiarEstado(int id, string estado)
        {
            var permitidos = new[] { DisenoMezcla.Aprobado, DisenoMezcla.Descartado, DisenoMezcla.Calculado };
            var d = await Db.Disenos.FindAsync(id);
            if (d == null) return HttpNotFound();
            if (!permitidos.Contains(estado)) return new HttpStatusCodeResult(400);

            d.Estado = estado;
            await Db.SaveChangesAsync();
            Exito("Diseño " + d.Codigo + " marcado como " + estado.ToLowerInvariant());
            return RedirectToAction("Details", new { id });
        }

        private async Task<DisenoAciResultado> Calcular(DisenoFormVM vm)
        {
            var entrada = new DisenoAciEntrada
            {
                FcKgCm2 = vm.FcKgCm2.Value,
                AsentamientoPulg = vm.AsentamientoPulg.Value,
                TamanoMaxNominal = vm.TamanoMaxNominal,
                Cemento = Buscar(vm.Cementos, vm.IdCemento),
                Arena = Buscar(vm.Arenas, vm.IdArena),
                Piedra = Buscar(vm.Piedras, vm.IdPiedra),
                Agua = Buscar(vm.Aguas, vm.IdAgua),
                Aditivo = Buscar(vm.Aditivos, vm.IdAditivo),
                AditivoDosisKg = vm.IdAditivo != null ? vm.AditivoDosisKg : null
            };

            var parametros = await new DisenoDatosService(Db).CargarParametrosAsync();
            var resultado = _aci.Calcular(entrada, parametros);
            _costo.Calcular(resultado);
            return resultado;
        }

        private static Material Buscar(List<Material> lista, int? id)
        {
            return id == null ? null : lista.FirstOrDefault(m => m.IdMaterial == id.Value);
        }

        private async Task LlenarListas(DisenoFormVM vm)
        {
            var proyectos = await Db.Proyectos.OrderBy(p => p.Nombre).ToListAsync();
            vm.Proyectos = proyectos.Select(p => new SelectListItem { Value = p.IdProyecto.ToString(), Text = p.Nombre });

            var materiales = await Db.Materiales.Include(m => m.TipoMaterial)
                .Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync();
            vm.Cementos = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.Cemento)
                .OrderBy(m => m.PorcentajeAdicion).ToList();
            vm.Arenas = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.AgregadoFino).ToList();
            vm.Piedras = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.AgregadoGrueso).ToList();
            vm.Aguas = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.Agua).ToList();
            vm.Aditivos = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.Aditivo).ToList();
        }
    }
}
