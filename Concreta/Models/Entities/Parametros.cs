namespace Concreta.Models.Entities
{
    public class ParametroAguaAire
    {
        public int IdParametro { get; set; }
        public string TamanoMaxNominal { get; set; }
        public decimal AsentamientoMin { get; set; }
        public decimal AsentamientoMax { get; set; }
        public decimal AguaLitrosM3 { get; set; }
        public decimal AireAtrapado { get; set; }
    }

    public class ParametroRelacionAC
    {
        public int IdParametro { get; set; }
        public int FcrKgCm2 { get; set; }
        public decimal RelacionAC { get; set; }
    }

    public class ParametroAgregadoGrueso
    {
        public int IdParametro { get; set; }
        public string TamanoMaxNominal { get; set; }
        public decimal ModuloFineza { get; set; }
        public decimal VolumenM3 { get; set; }
    }
}
