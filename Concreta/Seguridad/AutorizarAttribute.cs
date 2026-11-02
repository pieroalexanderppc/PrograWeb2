using System.Web.Mvc;

namespace Concreta.Seguridad
{
    /// <summary>
    /// Como [Authorize], pero si el usuario ya inició sesión y no tiene el rol, muestra "Sin permiso"
    /// en lugar de devolverlo al login.
    /// </summary>
    public class AutorizarAttribute : AuthorizeAttribute
    {
        public AutorizarAttribute(string roles)
        {
            Roles = roles;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.HttpContext.Response.StatusCode = 403;
                filterContext.Result = new ViewResult { ViewName = "~/Views/Shared/SinPermiso.cshtml" };
                return;
            }
            base.HandleUnauthorizedRequest(filterContext);
        }
    }
}
