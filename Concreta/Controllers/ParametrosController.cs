using System.Threading.Tasks;
using System.Web.Mvc;
using Concreta.Seguridad;
using Concreta.Services;

namespace Concreta.Controllers
{
    [Autorizar(RolNombre.Administrador)]
    public class ParametrosController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            return View(await new DisenoDatosService(Db).CargarParametrosAsync());
        }
    }
}
