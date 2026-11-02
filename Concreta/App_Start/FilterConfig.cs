using System.Web.Mvc;

namespace Concreta
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Todo requiere sesión salvo las acciones marcadas con [AllowAnonymous].
            filters.Add(new AuthorizeAttribute());
        }
    }
}
