using System;
using System.Collections.Generic;
using System.Linq;

namespace Concreta.Services
{
    public interface ICostoCO2Service
    {
        void Calcular(DisenoAciResultado resultado);
        void CalcularParciales(IEnumerable<LineaDosificacion> lineas);
    }

    /// <summary>
    /// Costo (S/ por m³) y huella de carbono (kg CO₂e por m³). Es el único camino de cálculo de totales
    /// en la aplicación: redondea igual que sp_CalcularTotalesDiseno (costo a 2 y CO₂ a 3 decimales por línea).
    /// </summary>
    public class CostoCO2Service : ICostoCO2Service
    {
        public void CalcularParciales(IEnumerable<LineaDosificacion> lineas)
        {
            foreach (var l in lineas)
            {
                l.CostoParcial = Math.Round(l.CantidadKg * l.Material.CostoKg, 2, MidpointRounding.AwayFromZero);
                l.CO2Parcial = Math.Round(l.CantidadKg * l.Material.FactorCO2Kg, 3, MidpointRounding.AwayFromZero);
            }
        }

        public void Calcular(DisenoAciResultado resultado)
        {
            CalcularParciales(resultado.Lineas);
            resultado.CostoTotalM3 = resultado.Lineas.Sum(l => l.CostoParcial);
            resultado.HuellaCO2M3 = Math.Round(resultado.Lineas.Sum(l => l.CO2Parcial), 2, MidpointRounding.AwayFromZero);
        }
    }
}
