using System.Linq;
using Concreta.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Concreta.Tests
{
    [TestClass]
    public class DisenoAciServiceTests
    {
        private readonly DisenoAciService _aci = new DisenoAciService();
        private readonly CostoCO2Service _costo = new CostoCO2Service();

        [TestMethod]
        public void CasoObligatorio_TipoI()
        {
            var r = _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoI), DatosSemilla.Parametros());

            Assert.AreEqual(294, r.FcrKgCm2);
            Assert.AreEqual(0.558m, r.RelacionAC);
            Assert.AreEqual(205m, r.AguaLitros);
            Assert.AreEqual(2.0m, r.AireAtrapadoPct);
            Assert.AreEqual(367m, r.Linea(TipoLinea.Cemento).CantidadKg);
            Assert.AreEqual(992m, r.Linea(TipoLinea.Piedra).CantidadKg);
            Assert.AreEqual(771m, r.Linea(TipoLinea.Arena).CantidadKg);
            Assert.AreEqual(0, r.Advertencias.Count);
        }

        [TestMethod]
        public void CasoObligatorio_TipoIP()
        {
            var r = _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoIP), DatosSemilla.Parametros());

            Assert.AreEqual(367m, r.Linea(TipoLinea.Cemento).CantidadKg);
            Assert.AreEqual(739m, r.Linea(TipoLinea.Arena).CantidadKg);
        }

        [TestMethod]
        public void VolumenesSumanUnMetroCubico()
        {
            var r = _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoIP), DatosSemilla.Parametros());
            decimal total = r.Lineas.Sum(l => l.VolumenAbsolutoM3) + r.VolumenAireM3;
            Assert.AreEqual(1m, total, 0.0005m);
        }

        [DataTestMethod]
        [DataRow(175, 245)]
        [DataRow(210, 294)]
        [DataRow(350, 434)]
        [DataRow(400, 490)]
        public void Fcr_SegunRango(int fc, int esperado)
        {
            Assert.AreEqual(esperado, _aci.CalcularFcr(fc));
        }

        [TestMethod]
        public void AsentamientoEntreRangos_UsaElSuperior()
        {
            var r = _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoI, slump: 5m), DatosSemilla.Parametros());
            Assert.AreEqual(216m, r.AguaLitros);
            Assert.AreEqual(1, r.Advertencias.Count);
        }

        [TestMethod]
        public void FcrFueraDeTabla_UsaElExtremoYAdvierte()
        {
            var r = _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoI, fc: 500), DatosSemilla.Parametros());
            Assert.AreEqual(600, r.FcrKgCm2);
            Assert.AreEqual(0.380m, r.RelacionAC);
            Assert.IsTrue(r.Advertencias.Any(a => a.Contains("fuera de la tabla")));
        }

        [TestMethod]
        public void Aditivo_ReduceAguaYCemento()
        {
            var e = DatosSemilla.Entrada(DatosSemilla.CementoTipoI);
            e.Aditivo = DatosSemilla.Aditivo;
            e.AditivoDosisPctCemento = 1m;
            e.ReduccionAguaPct = 10m;
            var r = _aci.Calcular(e, DatosSemilla.Parametros());

            Assert.AreEqual(185m, r.AguaLitros);                         // 205 × 0.9 = 184.5
            Assert.AreEqual(332m, r.CementoKg);                          // 185 / 0.558
            Assert.AreEqual(3.32m, r.Linea(TipoLinea.Aditivo).CantidadKg); // 1 % del cemento
        }

        [TestMethod]
        [ExpectedException(typeof(DisenoAciException))]
        public void FcFueraDeRango_Lanza()
        {
            _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoI, fc: 50), DatosSemilla.Parametros());
        }

        [TestMethod]
        public void CostoYCO2_TipoI()
        {
            var r = _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoI), DatosSemilla.Parametros());
            _costo.Calcular(r);
            Assert.AreEqual(339.85m, r.CostoTotalM3);
            Assert.AreEqual(341.66m, r.HuellaCO2M3);
        }

        [TestMethod]
        public void CostoYCO2_TipoIP()
        {
            var r = _aci.Calcular(DatosSemilla.Entrada(DatosSemilla.CementoTipoIP), DatosSemilla.Parametros());
            _costo.Calcular(r);
            Assert.AreEqual(323.73m, r.CostoTotalM3);
            Assert.AreEqual(242.41m, r.HuellaCO2M3);
        }
    }
}
