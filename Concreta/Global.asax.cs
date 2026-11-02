using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Concreta.Data;
using Concreta.Seguridad;

namespace Concreta
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            MvcHandler.DisableMvcResponseHeader = true;

            try
            {
                DbSeeder.Ejecutar();
            }
            catch (Exception ex)
            {
                // La app arranca aunque la base no esté disponible; el error aparecerá al primer acceso.
                Trace.TraceError("No se pudo ejecutar DbSeeder: " + ex);
            }
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            ConcretaPrincipal.Cargar(HttpContext.Current);
        }
    }
}
