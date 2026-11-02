using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Concreta.Services
{
    public interface IDisenoAciService
    {
        DisenoAciResultado Calcular(DisenoAciEntrada entrada, ParametrosAci parametros);
        int CalcularFcr(int fc);
    }

    /// <summary>
    /// Dosificación por m³ con el método ACI 211.1 (concreto sin aire incorporado).
    /// No accede a la base: recibe las tablas ACI ya cargadas.
    /// </summary>
    public class DisenoAciService : IDisenoAciService
    {
        private static readonly CultureInfo Pe = CultureInfo.GetCultureInfo("es-PE");

        public int CalcularFcr(int fc)
        {
            if (fc < 210) return fc + 70;
            if (fc <= 350) return fc + 84;
            return (int)Redondear(1.1m * fc + 50m, 0);
        }

        public DisenoAciResultado Calcular(DisenoAciEntrada e, ParametrosAci p)
        {
            Validar(e);

            var r = new DisenoAciResultado
            {
                FcKgCm2 = e.FcKgCm2,
                AsentamientoPulg = e.AsentamientoPulg,
                TamanoMaxNominal = e.TamanoMaxNominal
            };

            // 1. f'cr
            r.FcrKgCm2 = CalcularFcr(e.FcKgCm2);

            // 2. Agua y aire atrapado
            var filaAgua = BuscarAguaAire(p, e.TamanoMaxNominal, e.AsentamientoPulg, r.Advertencias);
            decimal agua = filaAgua.AguaLitrosM3 * (1m - e.ReduccionAguaPct / 100m);
            r.AguaLitros = Redondear(agua, 0);
            r.AireAtrapadoPct = filaAgua.AireAtrapado;

            // 3. Relación a/c
            r.RelacionAC = Redondear(InterpolarRelacionAC(p, r.FcrKgCm2, r.Advertencias), 3);

            // 4. Cemento
            decimal cementoKg = Redondear(r.AguaLitros / r.RelacionAC, 0);

            // 5. Agregado grueso
            r.VolumenAgregadoGrueso = Redondear(
                InterpolarAgregadoGrueso(p, e.TamanoMaxNominal, e.Arena.ModuloFineza.Value, r.Advertencias), 3);
            decimal piedraKg = Redondear(r.VolumenAgregadoGrueso * e.Piedra.PesoUnitarioCompactado.Value, 0);

            // Aditivo (opcional)
            decimal aditivoKg = 0m;
            if (e.Aditivo != null)
            {
                aditivoKg = e.AditivoDosisKg
                    ?? (e.AditivoDosisPctCemento ?? 0m) / 100m * cementoKg;
                aditivoKg = Redondear(aditivoKg, 2);
            }

            // 6. Volúmenes absolutos
            decimal vCemento = Volumen(cementoKg, e.Cemento.PesoEspecifico);
            decimal vAgua = Volumen(r.AguaLitros, e.Agua.PesoEspecifico);
            decimal vAire = r.AireAtrapadoPct / 100m;
            decimal vPiedra = Volumen(piedraKg, e.Piedra.PesoEspecifico);
            decimal vAditivo = aditivoKg > 0 ? Volumen(aditivoKg, e.Aditivo.PesoEspecifico) : 0m;

            // 7. Arena por diferencia
            decimal vArena = 1m - (vCemento + vAgua + vAire + vPiedra + vAditivo);
            if (vArena <= 0)
                throw new DisenoAciException("Los materiales elegidos no dejan volumen para la arena. Revisa el TMN y el asentamiento.");
            decimal arenaKg = Redondear(vArena * e.Arena.PesoEspecifico * 1000m, 0);

            // 8. Líneas redondeadas
            r.VolumenAireM3 = Redondear(vAire, 4);
            r.Lineas.Add(Linea(TipoLinea.Cemento, e.Cemento, cementoKg, vCemento));
            r.Lineas.Add(Linea(TipoLinea.Agua, e.Agua, r.AguaLitros, vAgua));
            r.Lineas.Add(Linea(TipoLinea.Arena, e.Arena, arenaKg, vArena));
            r.Lineas.Add(Linea(TipoLinea.Piedra, e.Piedra, piedraKg, vPiedra));
            if (aditivoKg > 0)
                r.Lineas.Add(Linea(TipoLinea.Aditivo, e.Aditivo, aditivoKg, vAditivo));

            return r;
        }

        private static void Validar(DisenoAciEntrada e)
        {
            if (e.FcKgCm2 < 100 || e.FcKgCm2 > 700)
                throw new DisenoAciException("Ingresa un f'c entre 100 y 700 kg/cm².");
            if (e.AsentamientoPulg <= 0)
                throw new DisenoAciException("El asentamiento debe ser mayor que 0 pulgadas.");
            if (string.IsNullOrWhiteSpace(e.TamanoMaxNominal))
                throw new DisenoAciException("Selecciona el tamaño máximo nominal (TMN).");
            if (e.Cemento == null || e.Arena == null || e.Piedra == null || e.Agua == null)
                throw new DisenoAciException("Selecciona el cemento, la arena, la piedra y el agua.");
            ValidarTmnPiedra(e.Piedra, e.TamanoMaxNominal);
            if (e.Arena.ModuloFineza == null)
                throw new DisenoAciException("La arena seleccionada no tiene módulo de fineza registrado.");
            if (e.Piedra.PesoUnitarioCompactado == null)
                throw new DisenoAciException("La piedra seleccionada no tiene peso unitario compactado registrado.");
        }

        public static void ValidarTmnPiedra(Models.Entities.Material piedra, string tmn)
        {
            if (!string.IsNullOrEmpty(piedra?.TamanoMaxNominal) && piedra.TamanoMaxNominal != tmn)
                throw new DisenoAciException(
                    $"La piedra seleccionada tiene TMN {piedra.TamanoMaxNominal} y el diseño pide TMN {tmn}. Usa el mismo TMN o elige otra piedra.");
        }

        private static Models.Entities.ParametroAguaAire BuscarAguaAire(
            ParametrosAci p, string tmn, decimal slump, List<string> advertencias)
        {
            var filas = p.AguaAire.Where(x => x.TamanoMaxNominal == tmn).OrderBy(x => x.AsentamientoMin).ToList();
            if (filas.Count == 0)
                throw new DisenoAciException($"No hay datos de agua de mezcla para TMN {tmn}.");

            // La primera fila cuyo máximo alcanza el asentamiento: si cae entre dos rangos, toma el superior.
            var fila = filas.FirstOrDefault(x => slump <= x.AsentamientoMax);
            if (fila == null)
            {
                fila = filas.Last();
                advertencias.Add($"El asentamiento de {slump.ToString("0.#", Pe)}\" supera la tabla ACI; se usó el rango {Rango(fila)}.");
            }
            else if (slump < fila.AsentamientoMin && fila != filas.First())
            {
                advertencias.Add($"El asentamiento de {slump.ToString("0.#", Pe)}\" cae entre dos rangos; se usó el rango superior {Rango(fila)}.");
            }
            return fila;
        }

        private static string Rango(Models.Entities.ParametroAguaAire f)
        {
            return $"{f.AsentamientoMin.ToString("0.#", Pe)}\"–{f.AsentamientoMax.ToString("0.#", Pe)}\"";
        }

        private static decimal InterpolarRelacionAC(ParametrosAci p, int fcr, List<string> advertencias)
        {
            var puntos = p.RelacionAC.OrderBy(x => x.FcrKgCm2)
                .Select(x => new KeyValuePair<decimal, decimal>(x.FcrKgCm2, x.RelacionAC)).ToList();
            if (puntos.Count == 0)
                throw new DisenoAciException("La tabla de relación a/c está vacía.");

            bool fuera;
            decimal ac = Interpolar(puntos, fcr, out fuera);
            if (fuera)
                advertencias.Add($"f'cr = {fcr} kg/cm² está fuera de la tabla de relación a/c; se usó el valor extremo ({ac.ToString("0.000", Pe)}).");
            return ac;
        }

        private static decimal InterpolarAgregadoGrueso(ParametrosAci p, string tmn, decimal mf, List<string> advertencias)
        {
            var puntos = p.AgregadoGrueso.Where(x => x.TamanoMaxNominal == tmn).OrderBy(x => x.ModuloFineza)
                .Select(x => new KeyValuePair<decimal, decimal>(x.ModuloFineza, x.VolumenM3)).ToList();
            if (puntos.Count == 0)
                throw new DisenoAciException($"No hay datos de volumen de agregado grueso para TMN {tmn}.");

            bool fuera;
            decimal vol = Interpolar(puntos, mf, out fuera);
            if (fuera)
                advertencias.Add($"El módulo de fineza {mf.ToString("0.00", Pe)} está fuera de la tabla ACI; se usó el valor extremo.");
            return vol;
        }

        /// <summary>Interpolación lineal sobre puntos ordenados por X; fuera del rango usa el extremo.</summary>
        public static decimal Interpolar(IList<KeyValuePair<decimal, decimal>> puntos, decimal x, out bool fueraDeRango)
        {
            fueraDeRango = false;
            if (x <= puntos[0].Key)
            {
                fueraDeRango = x < puntos[0].Key;
                return puntos[0].Value;
            }
            var ultimo = puntos[puntos.Count - 1];
            if (x >= ultimo.Key)
            {
                fueraDeRango = x > ultimo.Key;
                return ultimo.Value;
            }
            for (int i = 1; i < puntos.Count; i++)
            {
                var a = puntos[i - 1];
                var b = puntos[i];
                if (x <= b.Key)
                    return a.Value + (b.Value - a.Value) * (x - a.Key) / (b.Key - a.Key);
            }
            return ultimo.Value;
        }

        private static LineaDosificacion Linea(string tipo, Models.Entities.Material m, decimal kg, decimal volumen)
        {
            return new LineaDosificacion
            {
                Tipo = tipo,
                Material = m,
                CantidadKg = kg,
                VolumenAbsolutoM3 = Redondear(volumen, 4)
            };
        }

        private static decimal Volumen(decimal kg, decimal pesoEspecifico)
        {
            return kg / (pesoEspecifico * 1000m);
        }

        private static decimal Redondear(decimal valor, int decimales)
        {
            return Math.Round(valor, decimales, MidpointRounding.AwayFromZero);
        }
    }
}
