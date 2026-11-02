using System.Web.Mvc;
using Concreta.Data;
using Concreta.Seguridad;

namespace Concreta.Controllers
{
    public abstract class BaseController : Controller
    {
        private ConcretaDbContext _db;

        protected ConcretaDbContext Db => _db ?? (_db = new ConcretaDbContext());

        protected int IdUsuarioActual => User.IdUsuario();

        /// <summary>Mensaje de éxito que el layout muestra como toast.</summary>
        protected void Exito(string mensaje)
        {
            TempData["Exito"] = mensaje;
        }

        protected void Error(string mensaje)
        {
            TempData["Error"] = mensaje;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _db != null) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
