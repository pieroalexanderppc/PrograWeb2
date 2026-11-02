using System;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using Concreta.Models.Entities;

namespace Concreta.Seguridad
{
    /// <summary>Nombres de la tabla Rol (no se llama "Roles" para no chocar con System.Web.Security.Roles).</summary>
    public static class RolNombre
    {
        public const string Administrador = "Administrador";
        public const string Ingeniero = "Ingeniero";
        public const string Laboratorista = "Laboratorista";

        public const string AdminIngeniero = Administrador + "," + Ingeniero;
        public const string AdminLaboratorista = Administrador + "," + Laboratorista;
    }

    /// <summary>Usuario autenticado: el ticket de Forms Authentication guarda "IdUsuario|Rol|Nombre".</summary>
    public class ConcretaPrincipal : GenericPrincipal
    {
        public int IdUsuario { get; }
        public string Rol { get; }
        public string NombreCompleto { get; }

        public ConcretaPrincipal(IIdentity identity, int idUsuario, string rol, string nombre)
            : base(identity, new[] { rol })
        {
            IdUsuario = idUsuario;
            Rol = rol;
            NombreCompleto = nombre;
        }

        public static void IniciarSesion(Usuario u, bool recordar)
        {
            string datos = u.IdUsuario + "|" + u.Rol.Nombre + "|" + u.NombreCompleto;
            var ticket = new FormsAuthenticationTicket(1, u.Correo, DateTime.Now,
                DateTime.Now.Add(FormsAuthentication.Timeout), recordar, datos);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath
            };
            if (recordar) cookie.Expires = ticket.Expiration;
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        /// <summary>Se llama en Application_PostAuthenticateRequest.</summary>
        public static void Cargar(HttpContext contexto)
        {
            var identidad = contexto.User?.Identity as FormsIdentity;
            if (identidad == null || !identidad.IsAuthenticated) return;

            var partes = (identidad.Ticket.UserData ?? "").Split('|');
            int id;
            if (partes.Length < 3 || !int.TryParse(partes[0], out id))
            {
                FormsAuthentication.SignOut();
                return;
            }
            contexto.User = new ConcretaPrincipal(identidad, id, partes[1], partes[2]);
        }
    }

    public static class PrincipalExtensions
    {
        public static ConcretaPrincipal Concreta(this IPrincipal user)
        {
            return user as ConcretaPrincipal;
        }

        public static int IdUsuario(this IPrincipal user)
        {
            var p = user as ConcretaPrincipal;
            return p != null ? p.IdUsuario : 0;
        }
    }
}
