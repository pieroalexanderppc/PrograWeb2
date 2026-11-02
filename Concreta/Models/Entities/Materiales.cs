using System.Collections.Generic;

namespace Concreta.Models.Entities
{
    public class TipoMaterial
    {
        // Valores fijos de la tabla TipoMaterial (datos semilla)
        public const string Cemento = "Cemento";
        public const string AgregadoFino = "Agregado fino";
        public const string AgregadoGrueso = "Agregado grueso";
        public const string Agua = "Agua";
        public const string Aditivo = "Aditivo";

        public int IdTipoMaterial { get; set; }
        public string Nombre { get; set; }

        public virtual ICollection<Material> Materiales { get; set; } = new HashSet<Material>();
    }

    public class Material
    {
        public int IdMaterial { get; set; }
        public int IdTipoMaterial { get; set; }
        public string Nombre { get; set; }
        public string Proveedor { get; set; }
        public decimal PesoEspecifico { get; set; }          // g/cm3
        public decimal? PorcentajeAdicion { get; set; }      // solo cementos
        public decimal? ModuloFineza { get; set; }           // agregado fino
        public string TamanoMaxNominal { get; set; }         // agregado grueso
        public decimal? PesoUnitarioCompactado { get; set; } // kg/m3, agregado grueso
        public decimal? Absorcion { get; set; }              // %
        public decimal? ContenidoHumedad { get; set; }       // %
        public decimal CostoKg { get; set; }                 // S/ por kg
        public decimal FactorCO2Kg { get; set; }             // kg CO2e por kg
        public bool Activo { get; set; } = true;

        public virtual TipoMaterial TipoMaterial { get; set; }
    }
}
