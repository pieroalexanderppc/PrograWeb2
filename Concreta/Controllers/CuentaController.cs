using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using Concreta.Models.Entities;
using Concreta.Seguridad;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    public class CuentaController : BaseController
    {
        [AllowAnonymous]
        public ActionResult Landing()
        {
            return View();
        }

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

        [AllowAnonymous]
        public async Task<ActionResult> Registro()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Inicio");

            var vm = new RegistroVM();
            await CargarRolesPermitidosAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Registro(RegistroVM vm)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Inicio");

            if (!ModelState.IsValid)
            {
                await CargarRolesPermitidosAsync(vm);
                return View(vm);
            }

            string correo = vm.Correo.Trim().ToLowerInvariant();

            // Validar que el correo no esté registrado
            if (await Db.Usuarios.AnyAsync(u => u.Correo == correo))
            {
                ModelState.AddModelError("Correo", "Ya existe una cuenta registrada con este correo electrónico.");
                await CargarRolesPermitidosAsync(vm);
                return View(vm);
            }

            // Validar que el rol sea válido y NO sea Administrador
            var rol = await Db.Roles.FirstOrDefaultAsync(r => r.IdRol == vm.IdRol);
            if (rol == null || rol.Nombre == RolNombre.Administrador)
            {
                ModelState.AddModelError("IdRol", "El perfil seleccionado no es válido. Solo se permite registrar Ingeniero o Laboratorista.");
                await CargarRolesPermitidosAsync(vm);
                return View(vm);
            }

            var nuevoUsuario = new Usuario
            {
                IdRol = rol.IdRol,
                Nombres = vm.Nombres.Trim(),
                Apellidos = vm.Apellidos.Trim(),
                Correo = correo,
                ContrasenaHash = Crypto.HashPassword(vm.Contrasena),
                Activo = true,
                FechaRegistro = DateTime.Now
            };

            Db.Usuarios.Add(nuevoUsuario);
            await Db.SaveChangesAsync();

            // Iniciar sesión inmediatamente con el nuevo usuario
            nuevoUsuario.Rol = rol;
            ConcretaPrincipal.IniciarSesion(nuevoUsuario, false);

            Exito("¡Cuenta creada exitosamente! Bienvenido(a) a Concreta, " + nuevoUsuario.Nombres + ".");
            return RedirectToAction("Index", "Inicio");
        }

        private async Task CargarRolesPermitidosAsync(RegistroVM vm)
        {
            var roles = await Db.Roles
                .Where(r => r.Nombre != RolNombre.Administrador)
                .OrderBy(r => r.IdRol)
                .ToListAsync();

            vm.RolesDisponibles = roles.Select(r => new SelectListItem
            {
                Value = r.IdRol.ToString(),
                Text = r.Nombre + " — " + r.Descripcion,
                Selected = r.IdRol == vm.IdRol
            }).ToList();
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
