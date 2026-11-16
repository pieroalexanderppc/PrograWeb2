using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Concreta.Helpers;
using Concreta.Models.Entities;
using Concreta.ViewModels;

namespace Concreta.Services
{
    public interface IIaService
    {
        bool EstaConfigurada();
        Task<string> GenerarDictamenDisenoAsync(DisenoMezcla diseno, DosificacionVM dosificacion, decimal? resistencia28d);
        Task<string> GenerarJustificacionComparacionAsync(Comparacion comparacion, List<ColumnaComparacion> columnas);
        Task<string> ConsultarAsistenteAsync(string pregunta, string contexto);
    }

    public class GeminiIaService : IIaService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        private string ObtenerApiKey()
        {
            var key = ConfigurationManager.AppSettings["GeminiApiKey"];
            if (string.IsNullOrWhiteSpace(key) || key == "PEGA_AQUI_TU_API_KEY")
            {
                return null;
            }
            return key.Trim();
        }

        private string ObtenerModelo()
        {
            var modelo = ConfigurationManager.AppSettings["GeminiModel"];
            return string.IsNullOrWhiteSpace(modelo) ? "gemini-3.8-flash" : modelo.Trim();
        }

        public bool EstaConfigurada()
        {
            return ObtenerApiKey() != null;
        }

        private async Task<string> LlamarGeminiAsync(string prompt, string systemInstruction = null)
        {
            var apiKey = ObtenerApiKey();
            if (apiKey == null)
            {
                return "⚠️ **API Key no configurada:** Para activar la Inteligencia Artificial, abre el archivo `Web.config` y coloca tu clave gratuita en `<add key=\"GeminiApiKey\" value=\"tu_clave_aqui\" />` (puedes obtenerla gratis en [Google AI Studio](https://aistudio.google.com/app/apikey)).";
            }

            var modeloPreferido = ObtenerModelo();
            var modelosAProbar = new List<string> { modeloPreferido, "gemini-3.1-flash-lite", "gemini-3.5-flash-lite", "gemini-flash-latest", "gemini-3.8-flash" };
            // Quitar duplicados preservando el orden
            var modelosUnicos = new List<string>();
            foreach (var m in modelosAProbar)
            {
                if (!modelosUnicos.Contains(m)) modelosUnicos.Add(m);
            }

            var parts = new List<object> { new { text = prompt } };
            var contents = new List<object>
            {
                new { role = "user", parts }
            };

            var payload = new Dictionary<string, object>
            {
                ["contents"] = contents,
                ["generationConfig"] = new Dictionary<string, object>
                {
                    ["temperature"] = 0.3,
                    ["maxOutputTokens"] = 1200
                }
            };

            if (!string.IsNullOrWhiteSpace(systemInstruction))
            {
                payload["systemInstruction"] = new Dictionary<string, object>
                {
                    ["parts"] = new List<object> { new { text = systemInstruction } }
                };
            }

            string jsonBody = _json.Serialize(payload);
            string ultimoError = null;

            // Asegurar TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            foreach (var modelo in modelosUnicos)
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent?key={apiKey}";
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                try
                {
                    var response = await _httpClient.PostAsync(url, content);
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.BadRequest && responseText.Contains("API_KEY_INVALID"))
                        {
                            return "❌ **Error de autenticación:** La API Key de Gemini configurada en `Web.config` no es válida. Por favor verifica tu clave.";
                        }
                        // Si el modelo está congestionado (503) o saturó la cuota (429) o no existe (404), intentamos con el siguiente de la lista
                        if ((int)response.StatusCode == 503 || (int)response.StatusCode == 429 || response.StatusCode == HttpStatusCode.NotFound)
                        {
                            ultimoError = $"Modelo {modelo} no disponible ({response.StatusCode}). Reintentando con alternativa...";
                            continue;
                        }
                        return $"❌ **Error al consultar la IA ({response.StatusCode}):** {responseText}";
                    }

                    var root = _json.Deserialize<Dictionary<string, object>>(responseText);
                    if (root != null && root.ContainsKey("candidates"))
                    {
                        var candidates = root["candidates"] as System.Collections.ArrayList;
                        if (candidates != null && candidates.Count > 0)
                        {
                            var primerCandidato = candidates[0] as Dictionary<string, object>;
                            if (primerCandidato != null && primerCandidato.ContainsKey("content"))
                            {
                                var contentObj = primerCandidato["content"] as Dictionary<string, object>;
                                if (contentObj != null && contentObj.ContainsKey("parts"))
                                {
                                    var partesRespuesta = contentObj["parts"] as System.Collections.ArrayList;
                                    if (partesRespuesta != null && partesRespuesta.Count > 0)
                                    {
                                        var primeraParte = partesRespuesta[0] as Dictionary<string, object>;
                                        if (primeraParte != null && primeraParte.ContainsKey("text"))
                                        {
                                            return primeraParte["text"].ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return "No se pudo interpretar la respuesta devuelta por la IA.";
                }
                catch (TaskCanceledException)
                {
                    ultimoError = $"Timeout con el modelo {modelo}.";
                    continue;
                }
                catch (Exception ex)
                {
                    ultimoError = ex.Message;
                    continue;
                }
            }

            return "⏳ **Servidores de IA temporalmente ocupados:** Todos los modelos consultados están experimentando alta demanda momentánea en Google. Por favor intenta nuevamente en unos segundos. (" + ultimoError + ")";
        }

        public async Task<string> GenerarDictamenDisenoAsync(DisenoMezcla d, DosificacionVM dos, decimal? r28)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Actúa como un Ingeniero Civil Especialista en Tecnología del Concreto y Durabilidad según normativas ACI 211.1, ACI 318 y NTP (Norma Técnica Peruana).");
            sb.AppendLine("Analiza el siguiente diseño de mezcla de concreto y proporciona un dictamen técnico estructurado en 4 secciones claras en formato Markdown:");
            sb.AppendLine("1. **Evaluación de Dosificación y Trabajabilidad:** Relación a/c, contenido de pasta vs agregado, asentamiento.");
            sb.AppendLine("2. **Durabilidad y Sostenibilidad:** Impacto del tipo de cemento adicionado utilizado y huella de carbono (CO2e).");
            sb.AppendLine("3. **Recomendaciones de Vaciado y Curado en Obra:** Cuidados térmicos, tiempo de curado húmedo y desencofrado recomendado.");
            sb.AppendLine("4. **Dictamen de Resistencia:** Si ya tiene ensayo a 28 días evalúa el cumplimiento; si no, indica la proyección esperada.");
            sb.AppendLine();
            sb.AppendLine("DATOS DEL DISEÑO:");
            sb.AppendLine($"- Código: {d.Codigo}");
            sb.AppendLine($"- Resistencia de diseño f'c: {d.FcKgCm2} kg/cm²");
            sb.AppendLine($"- Resistencia requerida f'cr: {(d.FcrKgCm2.HasValue ? d.FcrKgCm2.Value.ToString() : "N/D")} kg/cm²");
            sb.AppendLine($"- Asentamiento (Slump): {d.AsentamientoPulg}\"");
            sb.AppendLine($"- Tamaño Máximo Nominal (TMN): {d.TamanoMaxNominal}");
            sb.AppendLine($"- Relación a/c: {d.RelacionAC:0.00}");
            sb.AppendLine($"- Costo por m³: S/ {d.CostoTotalM3:0.00}");
            sb.AppendLine($"- Huella CO2 por m³: {d.HuellaCO2M3:0.00} kg CO2e");
            if (r28.HasValue)
            {
                sb.AppendLine($"- Resistencia obtenida a 28 días: {r28.Value:0.0} kg/cm² ({(r28.Value >= d.FcKgCm2 ? "Cumple f'c" : "NO cumple f'c")})");
            }
            else
            {
                sb.AppendLine("- Resistencia obtenida a 28 días: Aún sin ensayo registrado");
            }

            sb.AppendLine("\nMATERIALES Y PROPORCIONES POR m³:");
            if (dos?.Lineas != null)
            {
                foreach (var l in dos.Lineas)
                {
                    sb.AppendLine($"- {l.Tipo}: {l.Material?.Nombre ?? "N/D"} | Cantidad: {l.CantidadKg:0.0} kg/m³");
                }
            }

            var promptInstruccion = "Eres un sistema de dictamen técnico automatizado para tecnología del concreto. REGLAS ESTRICTAS:\n" +
                "1. PROHIBIDO escribir introducciones, saludos o frases como 'Como Ingeniero...', 'A continuación...', 'Presento el análisis...'. Inicia DIRECTAMENTE en el primer encabezado: '### 1. Evaluación de Dosificación y Trabajabilidad'.\n" +
                "2. Cada viñeta DEBE ir en su propia línea con: '- **Concepto:** Detalle técnico'.\n" +
                "3. Separa cada una de las 4 secciones con dos saltos de línea.";

            var resultado = await LlamarGeminiAsync(sb.ToString(), promptInstruccion);
            return LimpiarRespuestaIa(resultado);
        }

        private static string LimpiarRespuestaIa(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto;

            // 1. Eliminar introducciones como "Como Ingeniero Civil...", "Como consultor...", etc.
            int idxHash = texto.IndexOf("###");
            var matchNum = System.Text.RegularExpressions.Regex.Match(texto, @"(\b1\.\s+[A-ZÁÉÍÓÚ])");
            int idxNum = matchNum.Success ? matchNum.Index : -1;
            int idx = -1;

            if (idxHash >= 0 && idxNum > 0) idx = Math.Min(idxHash, idxNum);
            else if (idxHash >= 0) idx = idxHash;
            else if (idxNum > 0) idx = idxNum;

            if (idx > 0 && idx < 450)
            {
                texto = texto.Substring(idx);
            }

            // 2. Si vino texto pegado en una sola línea, forzar saltos de línea antes de títulos y viñetas
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"([^\n])\s*(#{1,4}\s*)", "$1\n\n$2");
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"([^\n])\s*(\b[1-4]\.\s+[A-ZÁÉÍÓÚ])", "$1\n\n$2");
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"([^\n])\s*([-\*•]\s*\*\*)", "$1\n$2");
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"([^\n])\s*([-\*•]\s+[A-ZÁÉÍÓÚ])", "$1\n$2");

            return texto.Trim();
        }

        public async Task<string> GenerarJustificacionComparacionAsync(Comparacion c, List<ColumnaComparacion> columnas)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Actúa como un Ingeniero Consultor Senior en Estructuras y Sostenibilidad.");
            sb.AppendLine("Compara técnica y económicamente los siguientes diseños de mezcla para recomendar la mejor opción al cliente.");
            sb.AppendLine($"El criterio de decisión solicitado es: {c.Criterio}.");
            sb.AppendLine("Estructura tu respuesta en Markdown:");
            sb.AppendLine("- **Opción Ganadora y Argumento Principal**");
            sb.AppendLine("- **Balance Costo vs. Reducción de Huella de Carbono**");
            sb.AppendLine("- **Riesgos Técnicos o Recomendaciones para la Supervisión de Obra**");
            sb.AppendLine();
            sb.AppendLine("DISEÑOS COMPARADOS:");

            foreach (var col in columnas)
            {
                sb.AppendLine($"• Diseño {col.Diseno.Codigo}:");
                sb.AppendLine($"  - Cemento: {col.Cemento}");
                sb.AppendLine($"  - f'c meta: {col.Diseno.FcKgCm2} kg/cm² | a/c: {col.Diseno.RelacionAC:0.00}");
                sb.AppendLine($"  - Cemento: {col.CementoKg:0.0} kg/m³");
                sb.AppendLine($"  - Costo: S/ {col.Diseno.CostoTotalM3:0.00} por m³");
                sb.AppendLine($"  - Huella CO2: {col.Diseno.HuellaCO2M3:0.00} kg CO2e/m³");
                sb.AppendLine($"  - Ensayo 28 días: {(col.Resistencia28d.HasValue ? col.Resistencia28d.Value.ToString("0.0") + " kg/cm² (" + col.Estado + ")" : "Sin ensayo")}");
                sb.AppendLine($"  - Puntaje del algoritmo: {col.Puntaje:0.0} / 100 (Posición #{col.Posicion})");
            }

            var res = await LlamarGeminiAsync(sb.ToString(), "Eres un consultor ejecutivo de ingeniería civil. Redacta una justificación ejecutiva formal, fundamentada y profesional. Prohibido incluir saludos o frases como 'Como especialista...'. Comienza directamente con los puntos comparativos. Usa guiones con saltos de línea para cada viñeta ('- **Tema:** Explicación'). No uses asteriscos sueltos.");
            return LimpiarRespuestaIa(res);
        }

        public async Task<string> ConsultarAsistenteAsync(string pregunta, string contexto)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(contexto))
            {
                sb.AppendLine("CONTEXTO DEL PROYECTO O DISEÑO ACTUAL:");
                sb.AppendLine(contexto);
                sb.AppendLine();
            }
            sb.AppendLine("PREGUNTA DEL USUARIO:");
            sb.AppendLine(pregunta);

            return await LlamarGeminiAsync(sb.ToString(), "Eres 'Concreta IA', un asistente de ingeniería civil especializado en tecnología de concreto, dosificación ACI 211.1, cementos adicionados y control de calidad en obra. Responde de forma clara, técnica y muy estructurada. Usa guiones con saltos de línea para cada viñeta ('- **Tema:** Explicación'). No uses asteriscos sueltos.");
        }
    }
}
