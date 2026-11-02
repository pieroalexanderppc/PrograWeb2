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
        public async Task<ActionResult> Create(int? idProyecto, int? desde)
        {
            var vm = new DisenoFormVM { IdProyecto = idProyecto };
            await LlenarListas(vm);

            if (desde != null)
            {
                var origen = await Db.Disenos.ConDetalle().FirstOrDefaultAsync(x => x.IdDiseno == desde);
                if (origen == null) return HttpNotFound();
                Copiar(origen, vm);
                return View(vm);
            }

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

            if (accion == "guardar" || accion == "guardar_comparar")
            {
                var datos = new DisenoDatosService(Db);
                var diseno = await datos.GuardarAsync(resultado, vm.IdProyecto.Value, IdUsuarioActual, vm.Descripcion);
                Exito("Diseño " + diseno.Codigo + " guardado");
                if (accion == "guardar_comparar")
                    return RedirectToAction("Create", "Comparaciones", new { ids = diseno.IdDiseno });
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
                Resistencia28d = DisenoEstados.Resistencia28d(d),
                PuedeRegistrarEnsayo = User.IsInRole(RolNombre.Administrador) || User.IsInRole(RolNombre.Laboratorista),
                PuedeEditar = User.IsInRole(RolNombre.Administrador) || User.IsInRole(RolNombre.Ingeniero),
                PuedeAprobar = DisenoEstados.PuedeAprobar(d),
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
        public async Task<ActionResult> CambiarEstado(int id, string accion)
        {
            var d = await Db.Disenos.Include(x => x.Ensayos).FirstOrDefaultAsync(x => x.IdDiseno == id);
            if (d == null) return HttpNotFound();

            switch (accion)
            {
                case "aprobar":
                    if (!DisenoEstados.PuedeAprobar(d))
                    {
                        Error("Solo se aprueba un diseño cuyo ensayo a 28 días alcance f'c.");
                        return RedirectToAction("Details", new { id });
                    }
                    d.Estado = DisenoMezcla.Aprobado;
                    break;
                case "descartar":
                    d.Estado = DisenoMezcla.Descartado;
                    break;
                case "reactivar":
                    d.Estado = DisenoEstados.EstadoBase(d);
                    break;
                default:
                    return new HttpStatusCodeResult(400);
            }

            await Db.SaveChangesAsync();
            Exito("Diseño " + d.Codigo + ": estado " + d.Estado.ToLowerInvariant());
            return RedirectToAction("Details", new { id });
        }

        /// <summary>Hoja imprimible; el navegador la guarda como PDF.</summary>
        public async Task<ActionResult> Reporte(int id)
        {
            var d = await Db.Disenos.ConDetalle()
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.IdDiseno == id);
            if (d == null) return HttpNotFound();

            return View(new DisenoDetalleVM
            {
                Diseno = d,
                Dosificacion = DisenoConsultas.Dosificacion(d),
                Resistencia28d = DisenoEstados.Resistencia28d(d),
                EstadoResistencia = DisenoEstados.Resistencia(d)
            });
        }

        private static void Copiar(DisenoMezcla d, DisenoFormVM vm)
        {
            vm.IdProyecto = d.IdProyecto;
            vm.Descripcion = "Copia de " + d.Codigo;
            vm.FcKgCm2 = d.FcKgCm2;
            vm.AsentamientoPulg = d.AsentamientoPulg;
            vm.TamanoMaxNominal = d.TamanoMaxNominal;

            foreach (var det in d.Detalles)
            {
                switch (det.Material.TipoMaterial.Nombre)
                {
                    case TipoMaterial.Cemento: vm.IdCemento = det.IdMaterial; break;
                    case TipoMaterial.AgregadoFino: vm.IdArena = det.IdMaterial; break;
                    case TipoMaterial.AgregadoGrueso: vm.IdPiedra = det.IdMaterial; break;
                    case TipoMaterial.Agua: vm.IdAgua = det.IdMaterial; break;
                    case TipoMaterial.Aditivo:
                        vm.IdAditivo = det.IdMaterial;
                        vm.AditivoDosisKg = det.CantidadKgM3;
                        break;
                }
            }
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
                AditivoDosisKg = vm.IdAditivo != null ? vm.AditivoDosisKg : null,
                ReduccionAguaPct = vm.IdAditivo != null ? vm.ReduccionAguaPct ?? 0m : 0m
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
