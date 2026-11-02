using System.Collections.Generic;
using System.Linq;

namespace Concreta.Services
{
    /// <summary>Un segmento de la barra de composición (volumen absoluto dentro de 1 m³).</summary>
    public class SegmentoMezcla
    {
        public string Nombre { get; set; }
        /// <summary>Variable CSS del color del material, por ejemplo "--mat-cemento".</summary>
        public string ColorVar { get; set; }
        public decimal VolumenM3 { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public static class Composicion
    {
        public static List<SegmentoMezcla> Segmentos(IEnumerable<LineaDosificacion> lineas, decimal volumenAireM3)
        {
            var lista = lineas.ToList();
            var segmentos = new List<SegmentoMezcla>();

            Agregar(segmentos, "Cemento", "--mat-cemento", Volumen(lista, TipoLinea.Cemento));
            Agregar(segmentos, "Agua", "--mat-agua", Volumen(lista, TipoLinea.Agua));
            Agregar(segmentos, "Aire", "--mat-aire", volumenAireM3);
            Agregar(segmentos, "Aditivo", "--mat-aditivo", Volumen(lista, TipoLinea.Aditivo));
            Agregar(segmentos, "Arena", "--mat-fino", Volumen(lista, TipoLinea.Arena));
            Agregar(segmentos, "Piedra", "--mat-grueso", Volumen(lista, TipoLinea.Piedra));

            decimal total = segmentos.Sum(s => s.VolumenM3);
            if (total > 0)
                foreach (var s in segmentos)
                    s.Porcentaje = System.Math.Round(s.VolumenM3 / total * 100m, 1);

            return segmentos;
        }

        private static decimal Volumen(List<LineaDosificacion> lineas, string tipo)
        {
            return lineas.Where(l => l.Tipo == tipo).Sum(l => l.VolumenAbsolutoM3);
        }

        private static void Agregar(List<SegmentoMezcla> lista, string nombre, string color, decimal volumen)
        {
            if (volumen <= 0) return;
            lista.Add(new SegmentoMezcla { Nombre = nombre, ColorVar = color, VolumenM3 = volumen });
        }
    }
}
