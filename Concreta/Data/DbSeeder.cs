using System.Linq;
using System.Web.Helpers;

namespace Concreta.Data
{
    /// <summary>
    /// Reemplaza las contraseñas semilla 'HASH_PENDIENTE' del script SQL por un hash real.
    /// </summary>
    public static class DbSeeder
    {
        public const string HashPendiente = "HASH_PENDIENTE";
        public const string ContrasenaSemilla = "Concreta123*";

        public static void Ejecutar()
        {
            using (var db = new ConcretaDbContext())
            {
                var pendientes = db.Usuarios.Where(u => u.ContrasenaHash == HashPendiente).ToList();
                if (pendientes.Count == 0) return;

                foreach (var u in pendientes)
                    u.ContrasenaHash = Crypto.HashPassword(ContrasenaSemilla);

                db.SaveChanges();
            }
        }
    }
}
