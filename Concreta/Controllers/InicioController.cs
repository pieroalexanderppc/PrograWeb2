using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Helpers;
using Concreta.Models.Entities;
using Concreta.Services;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    public class InicioController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            int proyectos = await Db.Proyectos.CountAsync();
            int calculados = await Db.Disenos.CountAsync(d => d.Estado != DisenoMezcla.Borrador);
            int ensayos = await Db.Ensayos.CountAsync();
            decimal? co2Promedio = await Db.Disenos.Where(d => d.HuellaCO2M3 != null).AverageAsync(d => d.HuellaCO2M3);

            var vm = new InicioVM();
            vm.Kpis.Add(new KpiVM("Proyectos activos", proyectos.ToString()));
            vm.Kpis.Add(new KpiVM("Diseños calculados", calculados.ToString()));
            vm.Kpis.Add(new KpiVM("Ensayos registrados", ensayos.ToString()));
            vm.Kpis.Add(new KpiVM("CO₂ promedio", Formato.CO2(co2Promedio), "kg CO₂e/m³"));

            var todos = await DisenoConsultas.FilasAsync(Db.Disenos.Where(d => d.Estado != DisenoMezcla.Descartado));
            vm.UltimosDisenos = todos.OrderByDescending(d => d.FechaCreacion).ThenByDescending(d => d.IdDiseno).Take(8).ToList();
            vm.PendientesEnsayo = todos.Where(d => d.Resistencia28d == null)
                .OrderBy(d => d.FechaCreacion).Take(6).ToList();

            return View(vm);
        }
    }
}
