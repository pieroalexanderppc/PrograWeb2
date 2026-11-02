using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Concreta.Models.Entities;

namespace Concreta.Services
{
    public class GeneradorEntrada
    {
        public int FcKgCm2 { get; set; }
        public decimal AsentamientoPulg { get; set; }
        public string TamanoMaxNominal { get; set; }
        public Material Arena { get; set; }
        public Material Piedra { get; set; }
        public Material Agua { get; set; }
        public IList<Material> Cementos { get; set; } = new List<Material>();
        /// <summary>Aditivo plastificante activo; null si no hay ninguno en el catálogo.</summary>
        public Material Aditivo { get; set; }
        public string Criterio { get; set; }
    }

    public class Alternativa
    {
        public Material Cemento { get; set; }
        public bool ConAditivo { get; set; }
        public DisenoAciResultado Resultado { get; set; }
        public decimal Puntaje { get; set; }
        public int Posicion { get; set; }

        public string Etiqueta => Cemento.Nombre + (ConAditivo ? " + aditivo" : "");
    }

    public class GeneradorResultado
    {
        public List<Alternativa> Alternativas { get; set; } = new List<Alternativa>();
        public Alternativa Mejor => Alternativas.FirstOrDefault();
        public string Justificacion { get; set; }
        public List<string> Advertencias { get; set; } = new List<string>();
    }

    public interface IGeneradorAlternativasService
    {
        GeneradorResultado Generar(GeneradorEntrada entrada, ParametrosAci parametros);
        DisenoAciEntrada EntradaVariante(GeneradorEntrada entrada, Material cemento, bool conAditivo);
    }

    /// <summary>
    /// Optimizador: calcula, sin guardar, una variante por cemento activo (sin aditivo y con aditivo)
    /// y las ordena con el mismo puntaje de la recomendación.
    /// </summary>
    public class GeneradorAlternativasService : IGeneradorAlternativasService
    {
        public const decimal ReduccionAguaAditivoPct = 10m;
        public const decimal DosisAditivoPctCemento = 1m;
        private static readonly CultureInfo Pe = CultureInfo.GetCultureInfo("es-PE");

        private readonly IDisenoAciService _aci;
        private readonly ICostoCO2Service _costo;
        private readonly IRecomendacionService _recomendacion;

        public GeneradorAlternativasService()
            : this(new DisenoAciService(), new CostoCO2Service(), new RecomendacionService())
        {
        }

        public GeneradorAlternativasService(IDisenoAciService aci, ICostoCO2Service costo, IRecomendacionService recomendacion)
        {
            _aci = aci;
            _costo = costo;
            _recomendacion = recomendacion;
        }

        public DisenoAciEntrada EntradaVariante(GeneradorEntrada e, Material cemento, bool conAditivo)
        {
            var entrada = new DisenoAciEntrada
            {
                FcKgCm2 = e.FcKgCm2,
                AsentamientoPulg = e.AsentamientoPulg,
                TamanoMaxNominal = e.TamanoMaxNominal,
                Cemento = cemento,
                Arena = e.Arena,
                Piedra = e.Piedra,
                Agua = e.Agua
            };
            if (conAditivo && e.Aditivo != null)
            {
                entrada.Aditivo = e.Aditivo;
                entrada.AditivoDosisPctCemento = DosisAditivoPctCemento;
                entrada.ReduccionAguaPct = ReduccionAguaAditivoPct;
            }
            return entrada;
        }

        public GeneradorResultado Generar(GeneradorEntrada e, ParametrosAci parametros)
        {
            var salida = new GeneradorResultado();
            var variantes = new List<Alternativa>();

            foreach (var cemento in e.Cementos)
            {
                var opciones = e.Aditivo != null ? new[] { false, true } : new[] { false };
                foreach (var conAditivo in opciones)
                {
                    try
                    {
                        var resultado = _aci.Calcular(EntradaVariante(e, cemento, conAditivo), parametros);
                        _costo.Calcular(resultado);
                        variantes.Add(new Alternativa { Cemento = cemento, ConAditivo = conAditivo, Resultado = resultado });
                        foreach (var adv in resultado.Advertencias)
                            if (!salida.Advertencias.Contains(adv)) salida.Advertencias.Add(adv);
                    }
                    catch (DisenoAciException ex)
                    {
                        salida.Advertencias.Add(cemento.Nombre + ": " + ex.Message);
                    }
                }
            }

            if (variantes.Count == 0)
            {
                salida.Justificacion = "No se pudo calcular ninguna alternativa con los datos ingresados.";
                return salida;
            }

            // Sin ensayos: la penalización se aplica a todas por igual y no altera el orden.
            var candidatos = variantes.Select((v, i) => new CandidatoRecomendacion
            {
                IdDiseno = i,
                Codigo = v.Etiqueta,
                FcKgCm2 = e.FcKgCm2,
                Costo = v.Resultado.CostoTotalM3,
                CO2 = v.Resultado.HuellaCO2M3
            }).ToList();
            var ranking = _recomendacion.Recomendar(candidatos, e.Criterio).Ranking;

            foreach (var r in ranking)
            {
                var v = variantes[r.Candidato.IdDiseno];
                v.Puntaje = r.Puntaje;
                v.Posicion = r.Posicion;
                salida.Alternativas.Add(v);
            }

            salida.Justificacion = Justificar(salida.Mejor, variantes, e.Criterio);
            return salida;
        }

        private static string Justificar(Alternativa mejor, List<Alternativa> variantes, string criterio)
        {
            // Referencia: el cemento con menos adición (normalmente Tipo I), sin aditivo.
            var referencia = variantes.Where(v => !v.ConAditivo)
                .OrderBy(v => v.Cemento.PorcentajeAdicion ?? 0m).FirstOrDefault();

            string descripcion = "La alternativa con " + mejor.Cemento.Nombre + (mejor.ConAditivo ? " y aditivo plastificante" : "");

            if (referencia == null || referencia == mejor)
                return descripcion + " es la mejor según el criterio " + Criterio.Etiqueta(criterio) + ".";

            return descripcion + " " + Cambio(referencia.Resultado.HuellaCO2M3, mejor.Resultado.HuellaCO2M3, "el CO₂")
                   + " y " + Cambio(referencia.Resultado.CostoTotalM3, mejor.Resultado.CostoTotalM3, "el costo")
                   + " frente a " + referencia.Cemento.Nombre + " sin aditivo.";
        }

        private static string Cambio(decimal referencia, decimal valor, string nombre)
        {
            if (referencia == 0 || referencia == valor) return "mantiene " + nombre;
            decimal pct = Math.Abs((referencia - valor) / referencia * 100m);
            return (valor < referencia ? "reduce " : "aumenta ") + nombre + " un " + pct.ToString("N1", Pe) + " %";
        }
    }
}
