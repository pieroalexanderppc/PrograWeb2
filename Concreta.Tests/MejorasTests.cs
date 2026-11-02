using System;
using System.Collections.Generic;
using System.Linq;
using Concreta.Models.Entities;
using Concreta.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Concreta.Tests
{
    [TestClass]
    public class ValidacionTmnTests
    {
        [TestMethod]
        [ExpectedException(typeof(DisenoAciException))]
        public void PiedraConOtroTmn_Lanza()
        {
            var e = DatosSemilla.Entrada(DatosSemilla.CementoTipoI);
            e.TamanoMaxNominal = "1\"";
            new DisenoAciService().Calcular(e, DatosSemilla.Parametros());
        }

        [TestMethod]
        [ExpectedException(typeof(DisenoAciException))]
        public void Generador_PiedraConOtroTmn_Lanza()
        {
            new GeneradorAlternativasService().Generar(new GeneradorEntrada
            {
                FcKgCm2 = 210,
                AsentamientoPulg = 4m,
                TamanoMaxNominal = "1\"",
                Arena = DatosSemilla.Arena,
                Piedra = DatosSemilla.Piedra,
                Agua = DatosSemilla.Agua,
                Cementos = new List<Material> { DatosSemilla.CementoTipoI },
                Criterio = Criterio.Costo
            }, DatosSemilla.Parametros());
        }
    }

    [TestClass]
    public class CorreccionHumedadTests
    {
        private static List<LineaDosificacion> Lineas()
        {
            var arena = DatosSemilla.Arena; arena.ContenidoHumedad = 3m; arena.Absorcion = 1.2m;
            var piedra = DatosSemilla.Piedra; piedra.ContenidoHumedad = 1m; piedra.Absorcion = 0.8m;
            return new List<LineaDosificacion>
            {
                new LineaDosificacion { Tipo = TipoLinea.Cemento, Material = DatosSemilla.CementoTipoI, CantidadKg = 367m },
                new LineaDosificacion { Tipo = TipoLinea.Agua, Material = DatosSemilla.Agua, CantidadKg = 205m },
                new LineaDosificacion { Tipo = TipoLinea.Arena, Material = arena, CantidadKg = 771m },
                new LineaDosificacion { Tipo = TipoLinea.Piedra, Material = piedra, CantidadKg = 992m }
            };
        }

        [TestMethod]
        public void AjustaAgregadosYAgua()
        {
            var r = CorreccionHumedad.Calcular(Lineas());

            Assert.AreEqual(367m, r.Lineas.Single(l => l.Tipo == TipoLinea.Cemento).ObraKg);
            Assert.AreEqual(794m, r.Lineas.Single(l => l.Tipo == TipoLinea.Arena).ObraKg);    // 771 × 1.03
            Assert.AreEqual(1002m, r.Lineas.Single(l => l.Tipo == TipoLinea.Piedra).ObraKg);  // 992 × 1.01
            Assert.AreEqual(189m, r.Lineas.Single(l => l.Tipo == TipoLinea.Agua).ObraKg);     // 205 − 13.9 − 2.0
            Assert.AreEqual(15.9m, r.AporteAguaLitros);
        }

        [TestMethod]
        public void SinHumedadRegistrada_DevuelveNull()
        {
            var lineas = Lineas();
            lineas.Single(l => l.Tipo == TipoLinea.Arena).Material.ContenidoHumedad = null;
            Assert.IsNull(CorreccionHumedad.Calcular(lineas));
        }
    }

    [TestClass]
    public class DisenoEstadosTests
    {
        private static DisenoMezcla Diseno(string estado, params decimal[] r28)
        {
            var d = new DisenoMezcla { FcKgCm2 = 210, Estado = estado };
            foreach (var v in r28)
                d.Ensayos.Add(new EnsayoLaboratorio { TipoEnsayo = EnsayoLaboratorio.Compresion, EdadDias = 28, ValorObtenido = v });
            return d;
        }

        [TestMethod]
        public void Resistencia_EsPromedioDeProbetas_IgnoraOtrasEdades()
        {
            var d = Diseno(DisenoMezcla.Calculado, 200m, 230m);
            d.Ensayos.Add(new EnsayoLaboratorio { TipoEnsayo = EnsayoLaboratorio.Compresion, EdadDias = 7, ValorObtenido = 100m });
            Assert.AreEqual(215m, DisenoEstados.Resistencia28d(d));
        }

        [TestMethod]
        public void NoSeApruebaSinCumplir()
        {
            Assert.IsFalse(DisenoEstados.PuedeAprobar(Diseno(DisenoMezcla.Ensayado, 200m)));
            Assert.IsFalse(DisenoEstados.PuedeAprobar(Diseno(DisenoMezcla.Calculado)));
            Assert.IsTrue(DisenoEstados.PuedeAprobar(Diseno(DisenoMezcla.Ensayado, 215m)));
        }

        [TestMethod]
        public void Sincronizar_RespetaAprobacionSoloSiSigueCumpliendo()
        {
            var cumple = Diseno(DisenoMezcla.Aprobado, 220m);
            DisenoEstados.Sincronizar(cumple);
            Assert.AreEqual(DisenoMezcla.Aprobado, cumple.Estado);

            var dejo = Diseno(DisenoMezcla.Aprobado, 190m);
            DisenoEstados.Sincronizar(dejo);
            Assert.AreEqual(DisenoMezcla.Ensayado, dejo.Estado);

            var sinEnsayos = Diseno(DisenoMezcla.Ensayado);
            DisenoEstados.Sincronizar(sinEnsayos);
            Assert.AreEqual(DisenoMezcla.Calculado, sinEnsayos.Estado);

            var descartado = Diseno(DisenoMezcla.Descartado, 220m);
            DisenoEstados.Sincronizar(descartado);
            Assert.AreEqual(DisenoMezcla.Descartado, descartado.Estado);
        }

        [TestMethod]
        public void Historial_AgrupaPorCemento()
        {
            var tipo = new TipoMaterial { Nombre = TipoMaterial.Cemento };
            var cemento = new Material { IdMaterial = 2, TipoMaterial = tipo };
            var a = Diseno(DisenoMezcla.Ensayado, 231m);
            var b = Diseno(DisenoMezcla.Ensayado, 210m);
            a.Detalles.Add(new DetalleMezcla { IdMaterial = 2, Material = cemento });
            b.Detalles.Add(new DetalleMezcla { IdMaterial = 2, Material = cemento });

            var h = HistorialCemento.PorCemento(new[] { a, b })[2];

            Assert.AreEqual(2, h.Disenos);
            Assert.AreEqual(105m, h.PctFcPromedio);
            Assert.AreEqual(100m, h.PctFcMinimo);
        }
    }
}
