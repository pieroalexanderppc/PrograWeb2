using System.Collections.Generic;
using System.Linq;
using Concreta.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Concreta.Tests
{
    [TestClass]
    public class RecomendacionServiceTests
    {
        private readonly RecomendacionService _servicio = new RecomendacionService();

        private static CandidatoRecomendacion TipoI(decimal? r28 = 248m) =>
            new CandidatoRecomendacion { IdDiseno = 1, Codigo = "DM-2026-0001", FcKgCm2 = 210, Costo = 339.85m, CO2 = 341.66m, Resistencia28d = r28 };

        private static CandidatoRecomendacion TipoIP(decimal? r28 = 236m) =>
            new CandidatoRecomendacion { IdDiseno = 2, Codigo = "DM-2026-0002", FcKgCm2 = 210, Costo = 323.73m, CO2 = 242.41m, Resistencia28d = r28 };

        [TestMethod]
        public void Balanceado_RecomiendaTipoIP_ConJustificacion()
        {
            var r = _servicio.Recomendar(new List<CandidatoRecomendacion> { TipoI(), TipoIP() }, Criterio.Balanceado);

            Assert.AreEqual(2, r.Recomendado.Candidato.IdDiseno);
            Assert.AreEqual(100m, r.Recomendado.Puntaje);
            Assert.AreEqual(0m, r.Ranking.Single(x => x.Candidato.IdDiseno == 1).Puntaje);
            Assert.AreEqual(
                "Se recomienda DM-2026-0002: cumple f'c = 210 kg/cm² a 28 días (236 kg/cm²) con 29.0 % menos CO₂ y 4.7 % menos costo que DM-2026-0001.",
                r.Justificacion);
        }

        [TestMethod]
        public void SinEnsayo_Penaliza15Puntos()
        {
            var r = _servicio.Recomendar(new List<CandidatoRecomendacion> { TipoI(), TipoIP(null) }, Criterio.CO2);

            var ip = r.Ranking.Single(x => x.Candidato.IdDiseno == 2);
            Assert.AreEqual(EstadoResistencia.SinEnsayo, ip.Estado);
            Assert.AreEqual(85m, ip.Puntaje);
            Assert.AreEqual(2, r.Recomendado.Candidato.IdDiseno);
        }

        [TestMethod]
        public void NoCumple_SeExcluyeConPuntajeCero()
        {
            var r = _servicio.Recomendar(new List<CandidatoRecomendacion> { TipoI(), TipoIP(190m) }, Criterio.Costo);

            var ip = r.Ranking.Single(x => x.Candidato.IdDiseno == 2);
            Assert.IsTrue(ip.Excluido);
            Assert.AreEqual(0m, ip.Puntaje);
            Assert.AreEqual(2, ip.Posicion);
            Assert.AreEqual(1, r.Recomendado.Candidato.IdDiseno);
            StringAssert.Contains(r.Justificacion, "DM-2026-0002 no alcanzó f'c");
        }

        [TestMethod]
        public void NingunoCumple_SinRecomendacion()
        {
            var r = _servicio.Recomendar(new List<CandidatoRecomendacion> { TipoI(200m), TipoIP(190m) }, Criterio.Balanceado);
            Assert.IsNull(r.Recomendado);
        }

        [TestMethod]
        public void Normalizar_MaxIgualMin_EsCero()
        {
            Assert.AreEqual(0m, RecomendacionService.Normalizar(5m, 5m, 5m));
        }

        [TestMethod]
        public void Criterio_Costo_PrioridadAlMasBarato()
        {
            var caro = new CandidatoRecomendacion { IdDiseno = 3, Codigo = "C", FcKgCm2 = 210, Costo = 400m, CO2 = 100m, Resistencia28d = 250m };
            var barato = new CandidatoRecomendacion { IdDiseno = 4, Codigo = "B", FcKgCm2 = 210, Costo = 300m, CO2 = 300m, Resistencia28d = 250m };

            Assert.AreEqual(4, _servicio.Recomendar(new[] { caro, barato }, Criterio.Costo).Recomendado.Candidato.IdDiseno);
            Assert.AreEqual(3, _servicio.Recomendar(new[] { caro, barato }, Criterio.CO2).Recomendado.Candidato.IdDiseno);
        }
    }
}
