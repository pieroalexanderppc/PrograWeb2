using System.Collections.Generic;
using Concreta.Models.Entities;
using Concreta.Services;

namespace Concreta.Tests
{
    /// <summary>Mismos datos que database/ConcretaDB.sql.</summary>
    internal static class DatosSemilla
    {
        public static Material CementoTipoI => new Material { IdMaterial = 1, Nombre = "Cemento Portland Tipo I", PesoEspecifico = 3.150m, PorcentajeAdicion = 0m, CostoKg = 0.68m, FactorCO2Kg = 0.9m };
        public static Material CementoTipoIP => new Material { IdMaterial = 2, Nombre = "Cemento Portland Puzolánico Tipo IP", PesoEspecifico = 2.850m, PorcentajeAdicion = 30m, CostoKg = 0.64m, FactorCO2Kg = 0.63m };
        public static Material CementoAltaAdicion => new Material { IdMaterial = 3, Nombre = "Cemento de alta adición (escoria/puzolana)", PesoEspecifico = 2.800m, PorcentajeAdicion = 45m, CostoKg = 0.62m, FactorCO2Kg = 0.5m };
        public static Material Arena => new Material { IdMaterial = 4, Nombre = "Arena gruesa de río", PesoEspecifico = 2.650m, ModuloFineza = 2.80m, CostoKg = 0.045m, FactorCO2Kg = 0.005m };
        public static Material Piedra => new Material { IdMaterial = 5, Nombre = "Piedra chancada 3/4\"", PesoEspecifico = 2.700m, TamanoMaxNominal = "3/4\"", PesoUnitarioCompactado = 1600m, CostoKg = 0.055m, FactorCO2Kg = 0.0075m };
        public static Material Agua => new Material { IdMaterial = 6, Nombre = "Agua potable", PesoEspecifico = 1.000m, CostoKg = 0.005m, FactorCO2Kg = 0.0003m };
        public static Material Aditivo => new Material { IdMaterial = 7, Nombre = "Aditivo plastificante", PesoEspecifico = 1.200m, CostoKg = 9.5m, FactorCO2Kg = 1.5m };

        public static ParametrosAci Parametros()
        {
            var p = new ParametrosAci();
            string[] tmn = { "3/8\"", "1/2\"", "3/4\"", "1\"", "1 1/2\"", "2\"" };
            decimal[] aire = { 3.0m, 2.5m, 2.0m, 1.5m, 1.0m, 0.5m };
            decimal[,] agua = { { 207, 199, 190, 179, 166, 154 }, { 228, 216, 205, 193, 181, 169 }, { 243, 228, 216, 202, 190, 178 } };
            decimal[,] rangos = { { 1, 2 }, { 3, 4 }, { 6, 7 } };
            for (int r = 0; r < 3; r++)
                for (int t = 0; t < tmn.Length; t++)
                    p.AguaAire.Add(new ParametroAguaAire { TamanoMaxNominal = tmn[t], AsentamientoMin = rangos[r, 0], AsentamientoMax = rangos[r, 1], AguaLitrosM3 = agua[r, t], AireAtrapado = aire[t] });

            var ac = new Dictionary<int, decimal> { { 150, 0.800m }, { 200, 0.700m }, { 250, 0.620m }, { 300, 0.550m }, { 350, 0.480m }, { 400, 0.430m }, { 450, 0.380m } };
            foreach (var kv in ac) p.RelacionAC.Add(new ParametroRelacionAC { FcrKgCm2 = kv.Key, RelacionAC = kv.Value });

            decimal[] mf = { 2.40m, 2.60m, 2.80m, 3.00m };
            decimal[,] vol = { { .50m, .48m, .46m, .44m }, { .59m, .57m, .55m, .53m }, { .66m, .64m, .62m, .60m }, { .71m, .69m, .67m, .65m }, { .76m, .74m, .72m, .70m }, { .78m, .76m, .74m, .72m } };
            for (int t = 0; t < tmn.Length; t++)
                for (int m = 0; m < mf.Length; m++)
                    p.AgregadoGrueso.Add(new ParametroAgregadoGrueso { TamanoMaxNominal = tmn[t], ModuloFineza = mf[m], VolumenM3 = vol[t, m] });
            return p;
        }

        public static DisenoAciEntrada Entrada(Material cemento, int fc = 210, decimal slump = 4m)
        {
            return new DisenoAciEntrada
            {
                FcKgCm2 = fc,
                AsentamientoPulg = slump,
                TamanoMaxNominal = "3/4\"",
                Cemento = cemento,
                Arena = Arena,
                Piedra = Piedra,
                Agua = Agua
            };
        }
    }
}
