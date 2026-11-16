using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Concreta.Services;

namespace Concreta.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Ingresa tu correo.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        [Display(Name = "Correo")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "Ingresa tu contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; }

        [Display(Name = "Mantener la sesión iniciada")]
        public bool Recordar { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class RegistroVM
    {
        [Required(ErrorMessage = "Ingresa tus nombres.")]
        [StringLength(60, ErrorMessage = "Los nombres no pueden exceder 60 caracteres.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingresa tus apellidos.")]
        [StringLength(60, ErrorMessage = "Los apellidos no pueden exceder 60 caracteres.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "Selecciona tu rol profesional.")]
        [Display(Name = "Rol profesional")]
        public int IdRol { get; set; }

        [Required(ErrorMessage = "Ingresa una contraseña.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "Confirma tu contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [System.ComponentModel.DataAnnotations.Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; }

        public List<SelectListItem> RolesDisponibles { get; set; } = new List<SelectListItem>();
    }

    /// <summary>Una métrica de la tira de KPIs.</summary>
    public class KpiVM
    {
        public string Etiqueta { get; set; }
        public string Valor { get; set; }
        public string Unidad { get; set; }
        public string Prefijo { get; set; }
        /// <summary>Texto del delta, por ejemplo "29.0 % vs Tipo I".</summary>
        public string Delta { get; set; }
        /// <summary>true = mejora (verde, ↓), false = empeora (rojo, ↑).</summary>
        public bool DeltaMejora { get; set; }

        public KpiVM(string etiqueta, string valor, string unidad = null, string prefijo = null)
        {
            Etiqueta = etiqueta;
            Valor = valor;
            Unidad = unidad;
            Prefijo = prefijo;
        }
    }

    /// <summary>Tabla de dosificación + barra de composición de un diseño (calculado o guardado).</summary>
    public class DosificacionVM
    {
        public List<LineaDosificacion> Lineas { get; set; } = new List<LineaDosificacion>();
        public List<SegmentoMezcla> Segmentos { get; set; } = new List<SegmentoMezcla>();
        public decimal VolumenAireM3 { get; set; }
        public decimal AireAtrapadoPct { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal CO2Total { get; set; }
        /// <summary>Pesos de obra; null si faltan la humedad o la absorción de algún agregado.</summary>
        public PesosObra PesosObra { get; set; }
        /// <summary>Alto de la barra: "" normal (28px) o "mix-bar-sm" (8px).</summary>
        public string ClaseBarra { get; set; } = "";
        public bool MostrarLeyenda { get; set; } = true;

        public static DosificacionVM Desde(IEnumerable<LineaDosificacion> lineas, decimal airePct)
        {
            var vm = new DosificacionVM { AireAtrapadoPct = airePct, VolumenAireM3 = airePct / 100m };
            vm.Lineas.AddRange(lineas);
            vm.Segmentos = Composicion.Segmentos(vm.Lineas, vm.VolumenAireM3);
            vm.PesosObra = CorreccionHumedad.Calcular(vm.Lineas);
            foreach (var l in vm.Lineas)
            {
                vm.CostoTotal += l.CostoParcial;
                vm.CO2Total += l.CO2Parcial;
            }
            return vm;
        }

        public DosificacionVM SoloBarra()
        {
            return new DosificacionVM
            {
                Segmentos = Segmentos,
                ClaseBarra = "mix-bar-sm",
                MostrarLeyenda = false
            };
        }
    }

    /// <summary>Opciones fijas de TMN, igual que en las tablas ACI.</summary>
    public static class Opciones
    {
        public static readonly string[] Tmn = { "3/8\"", "1/2\"", "3/4\"", "1\"", "1 1/2\"", "2\"" };
        public static readonly int[] Edades = { 3, 7, 14, 28, 56 };
        public static readonly string[] TiposEnsayo = { "Compresion", "Asentamiento", "Temperatura", "PesoUnitario" };

        public static string EtiquetaTipoEnsayo(string tipo)
        {
            switch (tipo)
            {
                case "Compresion": return "Compresión";
                case "PesoUnitario": return "Peso unitario";
                default: return tipo;
            }
        }

        public static string UnidadEnsayo(string tipo)
        {
            switch (tipo)
            {
                case "Asentamiento": return "pulg";
                case "Temperatura": return "°C";
                case "PesoUnitario": return "kg/m3";
                default: return "kg/cm2";
            }
        }
    }
}
