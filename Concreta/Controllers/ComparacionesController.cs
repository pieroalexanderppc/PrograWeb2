using System;
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
    public class ComparacionesController : BaseController
    {
        private readonly IRecomendacionService _recomendacion = new RecomendacionService();

        public async Task<ActionResult> Index()
        {
            var lista = await Db.Comparaciones.Include(c => c.DisenoRecomendado).Include(c => c.Detalles)
                .OrderByDescending(c => c.FechaComparacion).ToListAsync();
            return View(lista);
        }

        public async Task<ActionResult> Create(int[] ids)
        {
            var vm = new ComparacionFormVM();
            if (ids != null) vm.IdsDisenos = ids.Take(4).ToList();
            await LlenarDisenos(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ComparacionFormVM vm)
        {
            vm.IdsDisenos = (vm.IdsDisenos ?? new System.Collections.Generic.List<int>()).Distinct().ToList();
            if (vm.IdsDisenos.Count < 2 || vm.IdsDisenos.Count > 4)
                ModelState.AddModelError("IdsDisenos", "Selecciona de 2 a 4 diseños para comparar.");
            if (!Criterio.Todos.Contains(vm.Criterio))
                ModelState.AddModelError("Criterio", "Selecciona un criterio válido.");
            if (!ModelState.IsValid)
            {
                await LlenarDisenos(vm);
                return View(vm);
            }

            var disenos = await Db.Disenos.ConDetalle().Where(d => vm.IdsDisenos.Contains(d.IdDiseno)).ToListAsync();
            var candidatos = disenos.Select(d => new CandidatoRecomendacion
            {
                IdDiseno = d.IdDiseno,
                Codigo = d.Codigo,
                FcKgCm2 = d.FcKgCm2,
                Costo = d.CostoTotalM3 ?? 0m,
                CO2 = d.HuellaCO2M3 ?? 0m,
                Resistencia28d = DisenoDatosService.Resistencia28d(d)
            }).ToList();

            var resultado = _recomendacion.Recomendar(candidatos, vm.Criterio);

            string nombre = string.IsNullOrWhiteSpace(vm.Nombre)
                ? string.Join(" vs ", disenos.OrderBy(d => d.Codigo).Select(d => d.Codigo))
                : vm.Nombre.Trim();

            var comparacion = new Comparacion
            {
                IdUsuario = IdUsuarioActual,
                Nombre = nombre.Length > 120 ? nombre.Substring(0, 120) : nombre,
                Criterio = vm.Criterio,
                IdDisenoRecomendado = resultado.Recomendado?.Candidato.IdDiseno,
                Justificacion = Recortar(resultado.Justificacion, 500),
                FechaComparacion = DateTime.Now
            };
            foreach (var r in resultado.Ranking)
            {
                comparacion.Detalles.Add(new ComparacionDetalle
                {
                    IdDiseno = r.Candidato.IdDiseno,
                    Puntaje = r.Puntaje,
                    Posicion = r.Posicion,
                    CumpleResistencia = r.Estado == EstadoResistencia.Cumple
                });
            }

            Db.Comparaciones.Add(comparacion);
            await Db.SaveChangesAsync();
            Exito("Comparación guardada");
            return RedirectToAction("Details", new { id = comparacion.IdComparacion });
        }

        public async Task<ActionResult> Details(int id)
        {
            var c = await Db.Comparaciones.Include(x => x.Detalles).Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.IdComparacion == id);
            if (c == null) return HttpNotFound();

            var ids = c.Detalles.Select(x => x.IdDiseno).ToList();
            var disenos = await Db.Disenos.ConDetalle().Where(d => ids.Contains(d.IdDiseno)).ToListAsync();

            var vm = new ComparacionDetalleVM { Comparacion = c };
            foreach (var det in c.Detalles.OrderBy(x => x.Posicion ?? 99))
            {
                var d = disenos.First(x => x.IdDiseno == det.IdDiseno);
                var dosificacion = DisenoConsultas.Dosificacion(d);
                var cemento = dosificacion.Lineas.FirstOrDefault(l => l.Tipo == TipoLinea.Cemento);
                var r28 = DisenoDatosService.Resistencia28d(d);
                vm.Columnas.Add(new ColumnaComparacion
                {
                    Diseno = d,
                    Cemento = cemento?.Material.Nombre,
                    CementoKg = cemento?.CantidadKg ?? 0m,
                    Resistencia28d = r28,
                    Estado = RecomendacionService.EvaluarResistencia(d.FcKgCm2, r28),
                    Puntaje = det.Puntaje ?? 0m,
                    Posicion = det.Posicion ?? 0,
                    Dosificacion = dosificacion
                });
            }
            vm.Recomendada = vm.Columnas.FirstOrDefault(x => x.Diseno.IdDiseno == c.IdDisenoRecomendado);
            return View(vm);
        }

        private async Task LlenarDisenos(ComparacionFormVM vm)
        {
            var filas = await DisenoConsultas.FilasAsync(
                Db.Disenos.Where(d => d.Estado != DisenoMezcla.Borrador && d.Estado != DisenoMezcla.Descartado));
            vm.Disenos = filas.OrderByDescending(d => d.FechaCreacion).ThenByDescending(d => d.IdDiseno).ToList();
        }

        private static string Recortar(string texto, int max)
        {
            return texto == null || texto.Length <= max ? texto : texto.Substring(0, max - 1) + "…";
        }
    }
}
