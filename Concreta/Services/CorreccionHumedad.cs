using System;
using System.Collections.Generic;
using System.Linq;

namespace Concreta.Services
{
    public class LineaObra
    {
        public string Tipo { get; set; }
        public string Material { get; set; }
        public decimal SecoKg { get; set; }
        public decimal ObraKg { get; set; }
    }

    /// <summary>Pesos por m³ que se miden en obra, con los agregados húmedos.</summary>
    public class PesosObra
    {
        public List<LineaObra> Lineas { get; set; } = new List<LineaObra>();
        /// <summary>Agua libre que aportan (+) o absorben (−) los agregados, en L/m³.</summary>
        public decimal AporteAguaLitros { get; set; }
    }

    /// <summary>
    /// Ajuste por humedad y absorción de ACI 211.1:
    /// húmedo = seco × (1 + w/100) y agua efectiva = agua − Σ seco × (w − a)/100.
    /// Los kg guardados en DetalleMezcla siguen siendo secos.
    /// </summary>
    public static class CorreccionHumedad
    {
        /// <summary>Devuelve null si a algún agregado le falta la humedad o la absorción.</summary>
        public static PesosObra Calcular(IEnumerable<LineaDosificacion> lineas)
        {
            var lista = lineas.ToList();
            var agregados = lista.Where(EsAgregado).ToList();
            if (agregados.Count == 0 || lista.All(l => l.Tipo != TipoLinea.Agua)
                || agregados.Any(l => l.Material.ContenidoHumedad == null || l.Material.Absorcion == null))
                return null;

            decimal aporte = agregados.Sum(l => l.CantidadKg * (l.Material.ContenidoHumedad.Value - l.Material.Absorcion.Value) / 100m);
            var salida = new PesosObra { AporteAguaLitros = Math.Round(aporte, 1, MidpointRounding.AwayFromZero) };

            foreach (var l in lista)
            {
                decimal obra = l.CantidadKg;
                if (EsAgregado(l)) obra = l.CantidadKg * (1m + l.Material.ContenidoHumedad.Value / 100m);
                else if (l.Tipo == TipoLinea.Agua) obra = l.CantidadKg - aporte;

                salida.Lineas.Add(new LineaObra
                {
                    Tipo = l.Tipo,
                    Material = l.Material.Nombre,
                    SecoKg = l.CantidadKg,
                    ObraKg = l.Tipo == TipoLinea.Aditivo ? obra : Math.Round(obra, 0, MidpointRounding.AwayFromZero)
                });
            }
            return salida;
        }

        private static bool EsAgregado(LineaDosificacion l)
        {
            return l.Tipo == TipoLinea.Arena || l.Tipo == TipoLinea.Piedra;
        }
    }
}
