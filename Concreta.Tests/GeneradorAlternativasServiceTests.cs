using System.Collections.Generic;
using Concreta.Models.Entities;
using Concreta.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Concreta.Tests
{
    [TestClass]
    public class GeneradorAlternativasServiceTests
    {
        private static GeneradorEntrada Entrada(string criterio)
        {
            return new GeneradorEntrada
            {
                FcKgCm2 = 210,
                AsentamientoPulg = 4m,
                TamanoMaxNominal = "3/4\"",
                Arena = DatosSemilla.Arena,
                Piedra = DatosSemilla.Piedra,
                Agua = DatosSemilla.Agua,
                Cementos = new List<Material> { DatosSemilla.CementoTipoI, DatosSemilla.CementoTipoIP, DatosSemilla.CementoAltaAdicion },
                Aditivo = DatosSemilla.Aditivo,
                Criterio = criterio
            };
        }

        [TestMethod]
        public void GeneraDosVariantesPorCemento()
        {
            var r = new GeneradorAlternativasService().Generar(Entrada(Criterio.Balanceado), DatosSemilla.Parametros());
            Assert.AreEqual(6, r.Alternativas.Count);
            for (int i = 0; i < r.Alternativas.Count; i++)
                Assert.AreEqual(i + 1, r.Alternativas[i].Posicion);
        }

        [TestMethod]
        public void CO2_LaMejorEsAltaAdicion_ConJustificacion()
        {
            var r = new GeneradorAlternativasService().Generar(Entrada(Criterio.CO2), DatosSemilla.Parametros());

            Assert.AreEqual(3, r.Mejor.Cemento.IdMaterial);
            StringAssert.Contains(r.Justificacion, "frente a Cemento Portland Tipo I sin aditivo");
            StringAssert.Contains(r.Justificacion, "reduce el CO₂");
        }

        [TestMethod]
        public void Balanceado_AltaAdicionSinAditivo_43PorCientoMenosCO2()
        {
            var r = new GeneradorAlternativasService().Generar(Entrada(Criterio.Balanceado), DatosSemilla.Parametros());

            Assert.AreEqual(3, r.Mejor.Cemento.IdMaterial);
            Assert.IsFalse(r.Mejor.ConAditivo);
            StringAssert.Contains(r.Justificacion, "reduce el CO₂ un 43.0 % y reduce el costo un 7.0 %");
        }

        [TestMethod]
        public void SinAditivoEnCatalogo_SoloUnaVariantePorCemento()
        {
            var e = Entrada(Criterio.Costo);
            e.Aditivo = null;
            var r = new GeneradorAlternativasService().Generar(e, DatosSemilla.Parametros());
            Assert.AreEqual(3, r.Alternativas.Count);
        }
    }
}
