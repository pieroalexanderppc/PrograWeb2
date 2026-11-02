using System.Globalization;
using System.Web;
using System.Web.Mvc;

namespace Concreta.Helpers
{
    /// <summary>Formato numérico es-PE: kg con 0 decimales, S/ con 2, CO₂ con 1, a/c con 3 y % con 1.</summary>
    public static class Formato
    {
        public static readonly CultureInfo Pe = CultureInfo.GetCultureInfo("es-PE");

        public static string Kg(decimal? v) => v?.ToString("N0", Pe) ?? "—";
        public static string Soles(decimal? v) => v?.ToString("N2", Pe) ?? "—";
        public static string CO2(decimal? v) => v?.ToString("N1", Pe) ?? "—";
        public static string AC(decimal? v) => v?.ToString("N3", Pe) ?? "—";
        public static string Pct(decimal? v) => v?.ToString("N1", Pe) ?? "—";
        public static string M3(decimal? v) => v?.ToString("N4", Pe) ?? "—";
        public static string Pulg(decimal? v) => v?.ToString("0.#", Pe) ?? "—";

        /// <summary>Número para CSS (siempre con punto decimal).</summary>
        public static string Css(decimal v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        public static string Fecha(System.DateTime? f) => f?.ToString("dd/MM/yyyy", Pe) ?? "—";

        /// <summary>Las columnas VARCHAR guardan "CO₂" como "CO2"; al mostrarlo se restaura el subíndice.</summary>
        public static string Quimica(string texto) => texto?.Replace("CO2", "CO₂");

        /// <summary>Resumen de propiedades de un material, por ejemplo "PE 2.850 · adición 30.0 % · S/ 0.6400/kg · 0.630 kg CO₂e/kg".</summary>
        public static string PropsMaterial(Models.Entities.Material m)
        {
            var partes = new System.Collections.Generic.List<string> { "PE " + m.PesoEspecifico.ToString("0.000", Pe) };
            if (m.PorcentajeAdicion != null) partes.Add("adición " + Pct(m.PorcentajeAdicion) + " %");
            if (m.ModuloFineza != null) partes.Add("MF " + m.ModuloFineza.Value.ToString("0.00", Pe));
            if (m.TamanoMaxNominal != null) partes.Add("TMN " + m.TamanoMaxNominal);
            if (m.PesoUnitarioCompactado != null) partes.Add("PUC " + Kg(m.PesoUnitarioCompactado) + " kg/m³");
            partes.Add("S/ " + m.CostoKg.ToString("0.0000", Pe) + "/kg");
            partes.Add(m.FactorCO2Kg.ToString("0.000", Pe) + " kg CO₂e/kg");
            return string.Join(" · ", partes);
        }

        /// <summary>JSON IdMaterial → propiedades, para el script de la vista.</summary>
        public static IHtmlString PropsJson(System.Collections.Generic.IEnumerable<Models.Entities.Material> materiales)
        {
            var dic = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var m in materiales) dic[m.IdMaterial.ToString()] = PropsMaterial(m);
            return new HtmlString(System.Web.Helpers.Json.Encode(dic));
        }
    }

    public static class HtmlExtensions
    {
        /// <summary>Cifra en Plex Mono seguida de su unidad: &lt;span class="num"&gt;367&lt;/span&gt;&lt;span class="unit"&gt;kg/m³&lt;/span&gt;.</summary>
        public static IHtmlString Num(this HtmlHelper html, string valor, string unidad = null, string prefijo = null)
        {
            string s = "";
            if (!string.IsNullOrEmpty(prefijo))
                s += "<span class=\"unit unit-pre\">" + HttpUtility.HtmlEncode(prefijo) + "</span>";
            s += "<span class=\"num\">" + HttpUtility.HtmlEncode(valor) + "</span>";
            if (!string.IsNullOrEmpty(unidad))
                s += "<span class=\"unit\">" + HttpUtility.HtmlEncode(unidad) + "</span>";
            return new HtmlString(s);
        }

        /// <summary>"active" si el controlador actual es alguno de los indicados.</summary>
        public static string MenuActivo(this HtmlHelper html, params string[] controladores)
        {
            string actual = html.ViewContext.RouteData.Values["controller"] as string;
            foreach (var c in controladores)
                if (string.Equals(c, actual, System.StringComparison.OrdinalIgnoreCase)) return "active";
            return "";
        }
    }
}
