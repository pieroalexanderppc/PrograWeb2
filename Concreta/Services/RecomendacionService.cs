using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Concreta.Services
{
    public static class Criterio
    {
        public const string Costo = "Costo";
        public const string CO2 = "CO2";
        public const string Balanceado = "Balanceado";

        public static readonly string[] Todos = { Costo, CO2, Balanceado };

        public static string Etiqueta(string criterio)
        {
            return criterio == CO2 ? "CO₂" : criterio;
        }
    }

    public enum EstadoResistencia
    {
        Cumple,
        NoCumple,
        SinEnsayo
    }

    public class CandidatoRecomendacion
    {
        public int IdDiseno { get; set; }
        public string Codigo { get; set; }
        public int FcKgCm2 { get; set; }
        public decimal Costo { get; set; }
        public decimal CO2 { get; set; }
        /// <summary>Resistencia a compresión a 28 días; null si no hay ensayo.</summary>
        public decimal? Resistencia28d { get; set; }
    }

    public class ResultadoCandidato
    {
        public CandidatoRecomendacion Candidato { get; set; }
        public EstadoResistencia Estado { get; set; }
        public decimal Puntaje { get; set; }
        public int Posicion { get; set; }
        public bool Excluido => Estado == EstadoResistencia.NoCumple;
    }

    public class ResultadoRecomendacion
    {
        public string Criterio { get; set; }
        public List<ResultadoCandidato> Ranking { get; set; } = new List<ResultadoCandidato>();
        public ResultadoCandidato Recomendado { get; set; }
        public string Justificacion { get; set; }
    }

    public interface IRecomendacionService
    {
        ResultadoRecomendacion Recomendar(IEnumerable<CandidatoRecomendacion> candidatos, string criterio);
    }

    /// <summary>
    /// Puntaje = 100 × (1 − (wCosto·nCosto + wCO2·nCO2)) − penalización, con normalización min-max
    /// entre los candidatos que no fueron excluidos.
    /// </summary>
    public class RecomendacionService : IRecomendacionService
    {
        public const decimal PenalizacionSinEnsayo = 15m;
        private static readonly CultureInfo Pe = CultureInfo.GetCultureInfo("es-PE");

        public static EstadoResistencia EvaluarResistencia(int fc, decimal? resistencia28d)
        {
            if (resistencia28d == null) return EstadoResistencia.SinEnsayo;
            return resistencia28d.Value >= fc ? EstadoResistencia.Cumple : EstadoResistencia.NoCumple;
        }

        public static void Pesos(string criterio, out decimal wCosto, out decimal wCO2)
        {
            switch (criterio)
            {
                case Criterio.Costo: wCosto = 1m; wCO2 = 0m; break;
                case Criterio.CO2: wCosto = 0m; wCO2 = 1m; break;
                case Criterio.Balanceado: wCosto = 0.5m; wCO2 = 0.5m; break;
                default: throw new ArgumentException("Criterio no válido: " + criterio);
            }
        }

        public ResultadoRecomendacion Recomendar(IEnumerable<CandidatoRecomendacion> candidatos, string criterio)
        {
            decimal wCosto, wCO2;
            Pesos(criterio, out wCosto, out wCO2);

            var resultados = candidatos.Select(c => new ResultadoCandidato
            {
                Candidato = c,
                Estado = EvaluarResistencia(c.FcKgCm2, c.Resistencia28d)
            }).ToList();

            var elegibles = resultados.Where(r => !r.Excluido).ToList();
            if (elegibles.Count > 0)
            {
                decimal minCosto = elegibles.Min(r => r.Candidato.Costo), maxCosto = elegibles.Max(r => r.Candidato.Costo);
                decimal minCO2 = elegibles.Min(r => r.Candidato.CO2), maxCO2 = elegibles.Max(r => r.Candidato.CO2);

                foreach (var r in elegibles)
                {
                    decimal nCosto = Normalizar(r.Candidato.Costo, minCosto, maxCosto);
                    decimal nCO2 = Normalizar(r.Candidato.CO2, minCO2, maxCO2);
                    decimal puntaje = 100m * (1m - (wCosto * nCosto + wCO2 * nCO2));
                    if (r.Estado == EstadoResistencia.SinEnsayo) puntaje -= PenalizacionSinEnsayo;
                    r.Puntaje = Math.Round(Math.Max(0m, puntaje), 2, MidpointRounding.AwayFromZero);
                }
            }

            var ranking = resultados
                .OrderBy(r => r.Excluido)
                .ThenByDescending(r => r.Puntaje)
                .ThenBy(r => r.Candidato.CO2)
                .ThenBy(r => r.Candidato.Costo)
                .ToList();
            for (int i = 0; i < ranking.Count; i++) ranking[i].Posicion = i + 1;

            var resultado = new ResultadoRecomendacion { Criterio = criterio, Ranking = ranking };
            resultado.Recomendado = ranking.FirstOrDefault(r => !r.Excluido);
            resultado.Justificacion = Justificar(resultado);
            return resultado;
        }

        public static decimal Normalizar(decimal x, decimal min, decimal max)
        {
            return max == min ? 0m : (x - min) / (max - min);
        }

        private static string Justificar(ResultadoRecomendacion r)
        {
            if (r.Recomendado == null)
                return "Ningún diseño alcanzó f'c a 28 días, así que no hay una recomendación. Revisa las mezclas o registra nuevos ensayos.";

            var mejor = r.Recomendado.Candidato;
            string texto = "Se recomienda " + mejor.Codigo + ": ";

            if (r.Recomendado.Estado == EstadoResistencia.Cumple)
                texto += $"cumple f'c = {mejor.FcKgCm2} kg/cm² a 28 días ({mejor.Resistencia28d.Value.ToString("N0", Pe)} kg/cm²)";
            else
                texto += $"aún no tiene ensayo a 28 días (se aplicó una penalización de {PenalizacionSinEnsayo:0} puntos)";

            // Referencia: el otro diseño con más CO₂ (y más costo si empatan), para mostrar la mejora frente al caso base.
            var referencia = r.Ranking.Where(x => x != r.Recomendado)
                .OrderByDescending(x => x.Candidato.CO2).ThenByDescending(x => x.Candidato.Costo)
                .FirstOrDefault();
            if (referencia == null)
                return texto + ".";

            var otro = referencia.Candidato;
            texto += " con " + Diferencia(otro.CO2, mejor.CO2, "CO₂") + " y " + Diferencia(otro.Costo, mejor.Costo, "costo")
                     + " que " + otro.Codigo + ".";

            var excluidos = r.Ranking.Where(x => x.Excluido).Select(x => x.Candidato.Codigo).ToList();
            if (excluidos.Count > 0)
                texto += " " + string.Join(", ", excluidos)
                         + (excluidos.Count == 1
                             ? " no alcanzó f'c a 28 días y quedó fuera de la recomendación."
                             : " no alcanzaron f'c a 28 días y quedaron fuera de la recomendación.");

            return texto;
        }

        /// <summary>"29.0 % menos CO₂", "2.1 % más costo" o "el mismo costo".</summary>
        public static string Diferencia(decimal referencia, decimal valor, string nombre)
        {
            if (referencia == 0 || referencia == valor) return "el mismo " + nombre;
            decimal pct = Math.Abs((referencia - valor) / referencia * 100m);
            return pct.ToString("N1", Pe) + " % " + (valor < referencia ? "menos " : "más ") + nombre;
        }
    }
}
