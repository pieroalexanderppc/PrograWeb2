using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Concreta.ViewModels
{
    public class MaterialFormVM
    {
        public int IdMaterial { get; set; }

        [Required(ErrorMessage = "Selecciona el tipo de material.")]
        [Display(Name = "Tipo")]
        public int? IdTipoMaterial { get; set; }

        [Required(ErrorMessage = "Ingresa el nombre del material.")]
        [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [StringLength(100, ErrorMessage = "El proveedor admite hasta 100 caracteres.")]
        [Display(Name = "Proveedor")]
        public string Proveedor { get; set; }

        [Required(ErrorMessage = "Ingresa el peso específico.")]
        [Range(typeof(decimal), "0.5", "9.999", ErrorMessage = "Ingresa un peso específico entre 0.5 y 9.999 g/cm³.")]
        [Display(Name = "Peso específico")]
        public decimal? PesoEspecifico { get; set; }

        [Range(typeof(decimal), "0", "100", ErrorMessage = "El % de adición va de 0 a 100.")]
        [Display(Name = "Adición")]
        public decimal? PorcentajeAdicion { get; set; }

        [Range(typeof(decimal), "1", "5", ErrorMessage = "Ingresa un módulo de fineza entre 1.00 y 5.00.")]
        [Display(Name = "Módulo de fineza")]
        public decimal? ModuloFineza { get; set; }

        [Display(Name = "Tamaño máximo nominal")]
        public string TamanoMaxNominal { get; set; }

        [Range(typeof(decimal), "500", "3000", ErrorMessage = "Ingresa un peso unitario compactado entre 500 y 3000 kg/m³.")]
        [Display(Name = "Peso unitario compactado")]
        public decimal? PesoUnitarioCompactado { get; set; }

        [Range(typeof(decimal), "0", "30", ErrorMessage = "La absorción va de 0 a 30 %.")]
        [Display(Name = "Absorción")]
        public decimal? Absorcion { get; set; }

        [Range(typeof(decimal), "0", "30", ErrorMessage = "El contenido de humedad va de 0 a 30 %.")]
        [Display(Name = "Contenido de humedad")]
        public decimal? ContenidoHumedad { get; set; }

        [Required(ErrorMessage = "Ingresa el costo por kg.")]
        [Range(typeof(decimal), "0", "99999", ErrorMessage = "El costo no puede ser negativo.")]
        [Display(Name = "Costo")]
        public decimal? CostoKg { get; set; }

        [Required(ErrorMessage = "Ingresa el factor de CO₂.")]
        [Range(typeof(decimal), "0", "99999", ErrorMessage = "El factor de CO₂ no puede ser negativo.")]
        [Display(Name = "Factor de CO₂")]
        public decimal? FactorCO2Kg { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public IEnumerable<SelectListItem> Tipos { get; set; }
        /// <summary>IdTipoMaterial → nombre, para mostrar solo los campos del tipo elegido.</summary>
        public Dictionary<int, string> NombresTipo { get; set; } = new Dictionary<int, string>();
    }

    public class ProyectoFormVM
    {
        public int IdProyecto { get; set; }

        [Required(ErrorMessage = "Ingresa el nombre del proyecto.")]
        [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [StringLength(120, ErrorMessage = "El cliente admite hasta 120 caracteres.")]
        [Display(Name = "Cliente")]
        public string Cliente { get; set; }

        [StringLength(150, ErrorMessage = "La ubicación admite hasta 150 caracteres.")]
        [Display(Name = "Ubicación")]
        public string Ubicacion { get; set; }

        [StringLength(300, ErrorMessage = "La descripción admite hasta 300 caracteres.")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }
    }

    public class UsuarioFormVM
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "Ingresa los nombres.")]
        [StringLength(60)]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingresa los apellidos.")]
        [StringLength(60)]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Ingresa el correo.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        [StringLength(100)]
        [Display(Name = "Correo")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "Selecciona el rol.")]
        [Display(Name = "Rol")]
        public int? IdRol { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public IEnumerable<SelectListItem> Roles { get; set; }
    }
}
