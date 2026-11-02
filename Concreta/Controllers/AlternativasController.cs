using System.Collections.Generic;
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
    [Autorizar(RolNombre.AdminIngeniero)]
    public class AlternativasController : BaseController
    {
        private readonly IGeneradorAlternativasService _generador = new GeneradorAlternativasService();
        private readonly ICostoCO2Service _costo = new CostoCO2Service();
        private readonly IDisenoAciService _aci = new DisenoAciService();

        public async Task<ActionResult> Index()
        {
            var vm = new AlternativasFormVM();
            var materiales = await MaterialesActivos();
            LlenarListas(vm, materiales, await Db.Proyectos.OrderBy(p => p.Nombre).ToListAsync());
            vm.IdArena = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.AgregadoFino).Select(m => (int?)m.IdMaterial).FirstOrDefault();
            vm.IdPiedra = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.AgregadoGrueso).Select(m => (int?)m.IdMaterial).FirstOrDefault();
            return View(vm);
        }

        /// <summary>GET para que los resultados se puedan recargar o compartir con la URL.</summary>
        public async Task<ActionResult> Generar(AlternativasFormVM vm)
        {
            var materiales = await MaterialesActivos();
            LlenarListas(vm, materiales, await Db.Proyectos.OrderBy(p => p.Nombre).ToListAsync());
            if (!Criterio.Todos.Contains(vm.Criterio))
                ModelState.AddModelError("Criterio", "Selecciona un criterio válido.");
            if (!ModelState.IsValid) return View("Index", vm);

            var entrada = Entrada(vm.FcKgCm2.Value, vm.AsentamientoPulg.Value, vm.TamanoMaxNominal,
                vm.IdArena.Value, vm.IdPiedra.Value, vm.Criterio, materiales);
            if (entrada.Arena == null || entrada.Piedra == null || entrada.Agua == null || entrada.Cementos.Count == 0)
            {
                vm.ErrorCalculo = "Faltan materiales activos en el catálogo (cemento, arena, piedra o agua).";
                return View("Index", vm);
            }

            vm.Resultado = _generador.Generar(entrada, await new DisenoDatosService(Db).CargarParametrosAsync());
            return View("Index", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Guardar(GuardarAlternativaVM vm)
        {
            if (!ModelState.IsValid || vm.IdCemento == null)
            {
                Error("Selecciona el proyecto donde guardar el diseño.");
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index"));
            }

            var materiales = await MaterialesActivos();
            var entrada = Entrada(vm.FcKgCm2.Value, vm.AsentamientoPulg.Value, vm.TamanoMaxNominal,
                vm.IdArena.Value, vm.IdPiedra.Value, Criterio.Balanceado, materiales);
            var cemento = entrada.Cementos.FirstOrDefault(m => m.IdMaterial == vm.IdCemento);
            if (cemento == null) return HttpNotFound();

            try
            {
                var resultado = _aci.Calcular(_generador.EntradaVariante(entrada, cemento, vm.ConAditivo), await new DisenoDatosService(Db).CargarParametrosAsync());
                _costo.Calcular(resultado);

                string descripcion = "f'c " + vm.FcKgCm2 + " con " + cemento.Nombre + (vm.ConAditivo ? " + aditivo" : "") + " (generador de alternativas)";
                var diseno = await new DisenoDatosService(Db).GuardarAsync(resultado, vm.IdProyecto.Value, IdUsuarioActual,
                    descripcion.Length > 200 ? descripcion.Substring(0, 200) : descripcion);
                Exito("Diseño " + diseno.Codigo + " guardado");
                return RedirectToAction("Details", "Disenos", new { id = diseno.IdDiseno });
            }
            catch (DisenoAciException ex)
            {
                Error(ex.Message);
                return RedirectToAction("Index");
            }
        }

        private Task<List<Material>> MaterialesActivos()
        {
            return Db.Materiales.Include(m => m.TipoMaterial).Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync();
        }

        private static GeneradorEntrada Entrada(int fc, decimal slump, string tmn, int idArena, int idPiedra,
            string criterio, List<Material> materiales)
        {
            return new GeneradorEntrada
            {
                FcKgCm2 = fc,
                AsentamientoPulg = slump,
                TamanoMaxNominal = tmn,
                Arena = materiales.FirstOrDefault(m => m.IdMaterial == idArena && m.TipoMaterial.Nombre == TipoMaterial.AgregadoFino),
                Piedra = materiales.FirstOrDefault(m => m.IdMaterial == idPiedra && m.TipoMaterial.Nombre == TipoMaterial.AgregadoGrueso),
                Agua = materiales.FirstOrDefault(m => m.TipoMaterial.Nombre == TipoMaterial.Agua),
                Cementos = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.Cemento)
                    .OrderBy(m => m.PorcentajeAdicion).ToList(),
                Aditivo = materiales.FirstOrDefault(m => m.TipoMaterial.Nombre == TipoMaterial.Aditivo),
                Criterio = criterio
            };
        }

        private static void LlenarListas(AlternativasFormVM vm, List<Material> materiales, List<Proyecto> proyectos)
        {
            vm.Arenas = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.AgregadoFino)
                .Select(m => new SelectListItem { Value = m.IdMaterial.ToString(), Text = m.Nombre });
            vm.Piedras = materiales.Where(m => m.TipoMaterial.Nombre == TipoMaterial.AgregadoGrueso)
                .Select(m => new SelectListItem { Value = m.IdMaterial.ToString(), Text = m.Nombre });
            vm.Proyectos = proyectos.Select(p => new SelectListItem { Value = p.IdProyecto.ToString(), Text = p.Nombre });
        }
    }
}
