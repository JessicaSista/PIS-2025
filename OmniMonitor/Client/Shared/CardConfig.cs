namespace OmniMonitor.Client.Shared;

public enum CardType
{
    KPI,
    Grafica
}

public class CardConfig
{
    public CardType Tipo { get; set; } = CardType.KPI;

    // KPI
    public string? KpiSeleccionado { get; set; }
    public bool Resaltado { get; set; }

    // KPI display values (llenados en el modal al guardar)
    public decimal? KpiValue { get; set; }
    public decimal? KpiDeltaPercent { get; set; }

    // Gráfica
    public List<ChartSerie> Series { get; set; } = new();
    public string TipoGrafica { get; set; } = "Linea";
    public DateTime FechaDesde { get; set; } = DateTime.Now.AddDays(-7);
    public DateTime FechaHasta { get; set; } = DateTime.Now;

    // Datos para previsualizar/mostrar la gráfica una vez guardada
    public List<DataPoint>? PreviewData { get; set; }
}

public class ChartSerie
{
    public string Source { get; set; } = "";
    public string Sensor { get; set; } = "";
    public string Dispositivo { get; set; } = "";
    public string Color { get; set; } = "#00FF00";
    public double Multiplier { get; set; } = 1.0;
}

public class DataPoint
{
    public DateTime Fecha { get; set; }
    public decimal Valor { get; set; }
}
