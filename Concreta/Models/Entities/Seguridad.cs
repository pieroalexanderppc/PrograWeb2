using System;
using System.Collections.Generic;

namespace Concreta.Models.Entities
{
    public class Rol
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        public virtual ICollection<Usuario> Usuarios { get; set; } = new HashSet<Usuario>();
    }

    public class Usuario
    {
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Correo { get; set; }
        public string ContrasenaHash { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public virtual Rol Rol { get; set; }

        public string NombreCompleto => Nombres + " " + Apellidos;
    }
}
