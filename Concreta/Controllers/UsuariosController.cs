using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Mvc;
using Concreta.Models.Entities;
using Concreta.Seguridad;
using Concreta.ViewModels;

namespace Concreta.Controllers
{
    [Autorizar(RolNombre.Administrador)]
    public class UsuariosController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            return View(await Db.Usuarios.Include(u => u.Rol).OrderBy(u => u.IdRol).ThenBy(u => u.Apellidos).ToListAsync());
        }

        public async Task<ActionResult> Create()
        {
            var vm = new UsuarioFormVM();
            await LlenarRoles(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UsuarioFormVM vm)
        {
            await LlenarRoles(vm);
            if (string.IsNullOrEmpty(vm.Contrasena))
                ModelState.AddModelError("Contrasena", "Ingresa una contraseña de al menos 8 caracteres.");
            await ValidarCorreo(vm);
            if (!ModelState.IsValid) return View(vm);

            var u = new Usuario { ContrasenaHash = Crypto.HashPassword(vm.Contrasena) };
            Copiar(vm, u);
            Db.Usuarios.Add(u);
            await Db.SaveChangesAsync();
            Exito("Usuario " + u.Correo + " creado");
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(int id)
        {
            var u = await Db.Usuarios.FindAsync(id);
            if (u == null) return HttpNotFound();
            var vm = new UsuarioFormVM
            {
                IdUsuario = u.IdUsuario,
                Nombres = u.Nombres,
                Apellidos = u.Apellidos,
                Correo = u.Correo,
                IdRol = u.IdRol,
                Activo = u.Activo
            };
            await LlenarRoles(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, UsuarioFormVM vm)
        {
            var u = await Db.Usuarios.FindAsync(id);
            if (u == null) return HttpNotFound();

            vm.IdUsuario = id;
            await LlenarRoles(vm);
            await ValidarCorreo(vm);
            if (id == IdUsuarioActual && !vm.Activo)
                ModelState.AddModelError("Activo", "No puedes desactivar tu propio usuario.");
            if (!ModelState.IsValid) return View(vm);

            Copiar(vm, u);
            if (!string.IsNullOrEmpty(vm.Contrasena))
                u.ContrasenaHash = Crypto.HashPassword(vm.Contrasena);
            await Db.SaveChangesAsync();
            Exito("Usuario " + u.Correo + " actualizado");
            return RedirectToAction("Index");
        }

        private async Task ValidarCorreo(UsuarioFormVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Correo)) return;
            string correo = vm.Correo.Trim().ToLowerInvariant();
            if (await Db.Usuarios.AnyAsync(x => x.Correo == correo && x.IdUsuario != vm.IdUsuario))
                ModelState.AddModelError("Correo", "Ya existe un usuario con ese correo.");
        }

        private async Task LlenarRoles(UsuarioFormVM vm)
        {
            var roles = await Db.Roles.OrderBy(r => r.IdRol).ToListAsync();
            vm.Roles = roles.Select(r => new SelectListItem { Value = r.IdRol.ToString(), Text = r.Nombre });
        }

        private static void Copiar(UsuarioFormVM vm, Usuario u)
        {
            u.Nombres = vm.Nombres.Trim();
            u.Apellidos = vm.Apellidos.Trim();
            u.Correo = vm.Correo.Trim().ToLowerInvariant();
            u.IdRol = vm.IdRol.Value;
            u.Activo = vm.Activo;
        }
    }
}
