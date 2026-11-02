using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using Concreta.Seguridad;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    public class CuentaController : BaseController
    {
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Inicio");
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            string correo = vm.Correo.Trim().ToLowerInvariant();
            var usuario = await Db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Correo == correo);

            bool valido = usuario != null && usuario.Activo
                          && usuario.ContrasenaHash != Data.DbSeeder.HashPendiente
                          && Crypto.VerifyHashedPassword(usuario.ContrasenaHash, vm.Contrasena);
            if (!valido)
            {
                ModelState.AddModelError("", "El correo o la contraseña no son correctos.");
                return View(vm);
            }

            ConcretaPrincipal.IniciarSesion(usuario, vm.Recordar);

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);
            return RedirectToAction("Index", "Inicio");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}
