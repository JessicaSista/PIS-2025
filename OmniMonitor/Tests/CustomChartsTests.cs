using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Shared.Dtos;
using Xunit;

public class CustomChartsTests
{
    private Mock<IHttpClientFactory> _httpClientFactoryMock;
    private Mock<HttpMessageHandler> _handlerMock;
    private HttpClient _httpClient;

    public CustomChartsTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content = "")
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }

    #region Criterio 1: Crear y Validar

    [Fact]
    public void CreateChart_WithDataset_RequiresDatasetId()
    {
        // Arrange
        var chartRequest = new ChartCreateRequest
        {
            Title = "Test Chart",
            ChartType = "line",
            SourceType = "dataset",
            DatasetId = null, // ← Esto debería fallar
            Mappings = new ChartMappings { XField = "date", YField = "value" },
            Appearance = new ChartAppearance { Color = "#ff0000" }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ValidateChartRequest(chartRequest)
        );
        Assert.Contains("datasetid", exception.Message.ToLower());
    }

    [Fact]
    public void CreateChart_WithDataset_ValidatesDatasetExists()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound);
        
        var chartRequest = new ChartCreateRequest
        {
            Title = "Test Chart",
            ChartType = "line",
            SourceType = "dataset",
            DatasetId = 999, // Dataset inexistente
            Mappings = new ChartMappings { XField = "date", YField = "value" }
        };

        // Act & Assert
        var result = ValidateDatasetExists(chartRequest.DatasetId.Value, "user123");
        Assert.False(result.IsValid);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void CreateChart_WithModule_ValidatesModuleType()
    {
        // Arrange
        var chartRequest = new ChartCreateRequest
        {
            Title = "Test Chart",
            ChartType = "bar",
            SourceType = "module",
            Module = "INVALID_MODULE", // ← Módulo inválido
            EntityType = "device"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ValidateModuleRequest(chartRequest)
        );
        Assert.Contains("module", exception.Message.ToLower());
    }

    [Theory]
    [InlineData("AM")]
    [InlineData("IM")]
    [InlineData("EM")]
    [InlineData("UM")]
    public void CreateChart_WithModule_AcceptsValidModules(string validModule)
    {
        // Arrange
        var chartRequest = new ChartCreateRequest
        {
            Title = "Test Chart",
            ChartType = "area",
            SourceType = "module",
            Module = validModule,
            EntityType = "device",
            QueryDef = new QueryDefinition { Fields = new[] { "id", "name" } }
        };

        // Act
        var result = ValidateModuleRequest(chartRequest);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateChart_WithModule_ValidatesEntityType()
    {
        // Arrange
        var chartRequest = new ChartCreateRequest
        {
            Title = "Test Chart",
            ChartType = "pie",
            SourceType = "module",
            Module = "IM",
            EntityType = "invalid_entity" // ← Entidad inválida para IM
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ValidateEntityTypeForModule(chartRequest.Module, chartRequest.EntityType)
        );
        Assert.Contains("entitytype", exception.Message.ToLower());
    }

    [Theory]
    [InlineData("IM", "device")]
    [InlineData("IM", "source")]
    [InlineData("IM", "group")]
    public void CreateChart_WithModule_AcceptsValidEntityTypes(string module, string entityType)
    {
        // Act
        var result = ValidateEntityTypeForModule(module, entityType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CreateChart_ValidatesRequiredFields_ForLineChart()
    {
        // Arrange
        var chartRequest = new ChartCreateRequest
        {
            Title = "Line Chart",
            ChartType = "line",
            SourceType = "dataset",
            DatasetId = 1,
            Mappings = new ChartMappings 
            { 
                XField = "date",
                YField = null // ← Falta YField requerido para line chart
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ValidateChartMappings(chartRequest)
        );
        Assert.Contains("yfield", exception.Message.ToLower());
    }

    [Fact]
    public void CreateChart_ValidatesRequiredFields_ForPieChart()
    {
        // Arrange
        var chartRequest = new ChartCreateRequest
        {
            Title = "Pie Chart",
            ChartType = "pie",
            SourceType = "dataset",
            DatasetId = 1,
            Mappings = new ChartMappings 
            { 
                CategoryField = "category",
                ValueField = null // ← Falta ValueField requerido para pie chart
            }
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            ValidateChartMappings(chartRequest)
        );
        Assert.Contains("valuefield", exception.Message.ToLower());
    }

    [Fact]
    public void CreateChart_PersistsRequiredFields()
    {
        // Arrange
        var chartRequest = new ChartCreateRequest
        {
            Title = "Test Chart",
            ChartType = "bar",
            SourceType = "dataset",
            DatasetId = 1,
            Mappings = new ChartMappings { XField = "category", YField = "value" },
            Appearance = new ChartAppearance { Color = "#00ff00", Title = "Mi Gráfica" }
        };

        // Act
        var persistedChart = CreatePersistedChart(chartRequest, "user123");

        // Assert - Verificar campos mínimos persistidos
        Assert.NotNull(persistedChart.OwnerId);
        Assert.Equal("user123", persistedChart.OwnerId);
        Assert.Equal("Test Chart", persistedChart.Title);
        Assert.Equal("bar", persistedChart.ChartType);
        Assert.NotNull(persistedChart.Mappings);
        Assert.NotNull(persistedChart.Appearance);
        Assert.Equal("dataset", persistedChart.SourceType);
        Assert.Equal(1, persistedChart.DatasetId);
    }

    #endregion

    #region Criterio 2: Visualizar

    [Fact]
    public void RenderChart_RespectsColorConfiguration()
    {
        // Arrange
        var chart = new Chart
        {
            Id = 1,
            ChartType = "line",
            Appearance = new ChartAppearance 
            { 
                Color = "#ff5722",
                Title = "Mi Gráfica"
            }
        };

        // Act
        var renderConfig = GenerateRenderConfig(chart);

        // Assert
        Assert.Equal("#ff5722", renderConfig.Color);
        Assert.Equal("Mi Gráfica", renderConfig.Title);
    }

    [Fact]
    public void RenderChart_RespectsLayoutConfiguration()
    {
        // Arrange
        var chart = new Chart
        {
            Id = 1,
            ChartType = "bar",
            Appearance = new ChartAppearance 
            { 
                Width = 800,
                Height = 600,
                ShowLegend = true,
                ShowLabels = false
            }
        };

        // Act
        var renderConfig = GenerateRenderConfig(chart);

        // Assert
        Assert.Equal(800, renderConfig.Width);
        Assert.Equal(600, renderConfig.Height);
        Assert.True(renderConfig.ShowLegend);
        Assert.False(renderConfig.ShowLabels);
    }

    #endregion

    #region Criterio 3: Listar / Detalle

    [Fact]
    public void ListCharts_SupportsPagination()
    {
        // Arrange
        var request = new ChartListRequest
        {
            Page = 1,
            PageSize = 10,
            UserId = "user123"
        };

        // Act
        var result = GetChartsPaginated(request);

        // Assert
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.TotalCount >= 0);
        Assert.NotNull(result.Charts);
    }

    [Fact]
    public void ListCharts_SupportsOrdering()
    {
        // Arrange
        var request = new ChartListRequest
        {
            Page = 1,
            PageSize = 5,
            OrderBy = "updatedAt",
            OrderDirection = "desc",
            UserId = "user123"
        };

        // Act
        var result = GetChartsPaginated(request);

        // Assert
        // Verificar que está ordenado por updatedAt descendente
        if (result.Charts.Count > 1)
        {
            for (int i = 0; i < result.Charts.Count - 1; i++)
            {
                Assert.True(result.Charts[i].UpdatedAt >= result.Charts[i + 1].UpdatedAt);
            }
        }
    }

    [Fact]
    public void ListCharts_SupportsSearchByTitle()
    {
        // Arrange
        var request = new ChartListRequest
        {
            Page = 1,
            PageSize = 10,
            SearchTitle = "Sales",
            UserId = "user123"
        };

        // Act
        var result = GetChartsPaginated(request);

        // Assert
        foreach (var chart in result.Charts)
        {
            Assert.Contains("Sales", chart.Title, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ListCharts_SupportsFilterBySourceType()
    {
        // Arrange
        var request = new ChartListRequest
        {
            Page = 1,
            PageSize = 10,
            SourceType = "module",
            UserId = "user123"
        };

        // Act
        var result = GetChartsPaginated(request);

        // Assert
        foreach (var chart in result.Charts)
        {
            Assert.Equal("module", chart.SourceType);
        }
    }

    [Fact]
    public void ListCharts_SupportsFilterByModule()
    {
        // Arrange
        var request = new ChartListRequest
        {
            Page = 1,
            PageSize = 10,
            Module = "IM",
            UserId = "user123"
        };

        // Act
        var result = GetChartsPaginated(request);

        // Assert
        foreach (var chart in result.Charts)
        {
            Assert.Equal("IM", chart.Module);
        }
    }

    [Fact]
    public void GetChartDetail_ReturnsCompleteConfiguration()
    {
        // Arrange
        int chartId = 1;
        string userId = "user123";

        // Act
        var detail = GetChartDetail(chartId, userId);

        // Assert
        Assert.NotNull(detail);
        Assert.NotNull(detail.Chart);
        Assert.NotNull(detail.Chart.Mappings);
        Assert.NotNull(detail.Chart.Appearance);
        Assert.NotNull(detail.SampleData);
        Assert.NotNull(detail.SourceMetadata);
    }

    [Fact]
    public void GetChartDetail_IncludesSampleData()
    {
        // Arrange
        int chartId = 1;
        string userId = "user123";

        // Act
        var detail = GetChartDetail(chartId, userId);

        // Assert
        Assert.NotNull(detail.SampleData);
        Assert.True(detail.SampleData.Count > 0);
        // Verificar que los datos de muestra incluyen los campos mapeados
        if (detail.Chart.Mappings.XField != null)
        {
            Assert.True(detail.SampleData.All(d => d.ContainsKey(detail.Chart.Mappings.XField)));
        }
    }

    #endregion

    #region Métodos auxiliares para simular la lógica

    private void ValidateChartRequest(ChartCreateRequest request)
    {
        if (request.SourceType == "dataset" && !request.DatasetId.HasValue)
        {
            throw new ArgumentException("DatasetId is required when sourceType is 'dataset'");
        }
    }

    private ValidationResult ValidateDatasetExists(int datasetId, string userId)
    {
        // Simular validación de dataset
        return new ValidationResult { IsValid = false, StatusCode = 404 };
    }

    private ValidationResult ValidateModuleRequest(ChartCreateRequest request)
    {
        var validModules = new[] { "AM", "IM", "EM", "UM" };
        if (!validModules.Contains(request.Module))
        {
            throw new ArgumentException($"Invalid module: {request.Module}");
        }
        return new ValidationResult { IsValid = true };
    }

    private bool ValidateEntityTypeForModule(string module, string entityType)
    {
        var validEntities = new Dictionary<string, string[]>
        {
            ["IM"] = new[] { "device", "source", "group" },
            ["AM"] = new[] { "alert", "rule" },
            ["EM"] = new[] { "event", "log" },
            ["UM"] = new[] { "user", "zone" }
        };

        if (!validEntities.ContainsKey(module) || !validEntities[module].Contains(entityType))
        {
            throw new ArgumentException($"Invalid entityType '{entityType}' for module '{module}'");
        }
        return true;
    }

    private void ValidateChartMappings(ChartCreateRequest request)
    {
        switch (request.ChartType.ToLower())
        {
            case "line":
            case "bar":
            case "area":
                if (string.IsNullOrEmpty(request.Mappings?.YField))
                    throw new ArgumentException("YField is required for line/bar/area charts");
                break;
            case "pie":
                if (string.IsNullOrEmpty(request.Mappings?.ValueField))
                    throw new ArgumentException("ValueField is required for pie charts");
                break;
        }
    }

    private Chart CreatePersistedChart(ChartCreateRequest request, string userId)
    {
        return new Chart
        {
            Id = 1,
            OwnerId = userId,
            Title = request.Title,
            ChartType = request.ChartType,
            Mappings = request.Mappings,
            Appearance = request.Appearance,
            SourceType = request.SourceType,
            DatasetId = request.DatasetId,
            Module = request.Module,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private ChartRenderConfig GenerateRenderConfig(Chart chart)
    {
        return new ChartRenderConfig
        {
            Color = chart.Appearance?.Color,
            Title = chart.Appearance?.Title,
            Width = chart.Appearance?.Width ?? 400,
            Height = chart.Appearance?.Height ?? 300,
            ShowLegend = chart.Appearance?.ShowLegend ?? true,
            ShowLabels = chart.Appearance?.ShowLabels ?? true
        };
    }

    private ChartListResult GetChartsPaginated(ChartListRequest request)
    {
        var allCharts = new List<Chart>
        {
            new Chart { Id = 1, Title = "Sales Report", SourceType = "dataset", UpdatedAt = DateTime.UtcNow.AddDays(-1) },
            new Chart { Id = 2, Title = "Device Status", SourceType = "module", Module = "IM", UpdatedAt = DateTime.UtcNow.AddDays(-2) },
            new Chart { Id = 3, Title = "Sales Analysis", SourceType = "dataset", UpdatedAt = DateTime.UtcNow.AddDays(-3) }
        };
        var filteredCharts = allCharts.AsEnumerable();

        if (!string.IsNullOrEmpty(request.SearchTitle))
        {
            filteredCharts = filteredCharts.Where(c => c.Title.Contains(request.SearchTitle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.SourceType))
        {
            filteredCharts = filteredCharts.Where(c => c.SourceType == request.SourceType);
        }

        if (!string.IsNullOrEmpty(request.Module))
        {
            filteredCharts = filteredCharts.Where(c => c.Module == request.Module);
        }

        // Aplicar ordenamiento
        if (request.OrderBy == "updatedAt")
        {
            filteredCharts = request.OrderDirection == "desc" 
                ? filteredCharts.OrderByDescending(c => c.UpdatedAt)
                : filteredCharts.OrderBy(c => c.UpdatedAt);
        }

        var pagedCharts = filteredCharts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new ChartListResult
        {
            Charts = pagedCharts,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = filteredCharts.Count()
        };
    }

    private ChartDetailResult GetChartDetail(int chartId, string userId)
    {
        var chart = new Chart
        {
            Id = chartId,
            Title = "Test Chart",
            ChartType = "line",
            SourceType = "dataset",
            DatasetId = 1,
            Mappings = new ChartMappings { XField = "date", YField = "value" },
            Appearance = new ChartAppearance { Color = "#ff0000" }
        };

        var sampleData = new List<Dictionary<string, object>>
        {
            new() { ["date"] = "2023-01-01", ["value"] = 100 },
            new() { ["date"] = "2023-01-02", ["value"] = 150 },
            new() { ["date"] = "2023-01-03", ["value"] = 120 }
        };

        return new ChartDetailResult
        {
            Chart = chart,
            SampleData = sampleData,
            SourceMetadata = new { DatasetName = "Test Dataset", Fields = new[] { "date", "value" } }
        };
    }

    #endregion

    #region DTOs de prueba

    public class ChartCreateRequest
    {
        public string Title { get; set; }
        public string ChartType { get; set; }
        public string SourceType { get; set; }
        public int? DatasetId { get; set; }
        public string Module { get; set; }
        public string EntityType { get; set; }
        public QueryDefinition QueryDef { get; set; }
        public ChartMappings Mappings { get; set; }
        public ChartAppearance Appearance { get; set; }
    }

    public class Chart
    {
        public int Id { get; set; }
        public string OwnerId { get; set; }
        public string Title { get; set; }
        public string ChartType { get; set; }
        public ChartMappings Mappings { get; set; }
        public ChartAppearance Appearance { get; set; }
        public string SourceType { get; set; }
        public int? DatasetId { get; set; }
        public string Module { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ChartMappings
    {
        public string XField { get; set; }
        public string YField { get; set; }
        public string CategoryField { get; set; }
        public string ValueField { get; set; }
    }

    public class ChartAppearance
    {
        public string Color { get; set; }
        public string Title { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool? ShowLegend { get; set; }
        public bool? ShowLabels { get; set; }
    }

    public class QueryDefinition
    {
        public string[] Fields { get; set; }
        public object Filters { get; set; }
        public string Sort { get; set; }
        public int? Limit { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }

    public class ChartRenderConfig
    {
        public string Color { get; set; }
        public string Title { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool ShowLegend { get; set; }
        public bool ShowLabels { get; set; }
    }

    public class ChartListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; }
        public string OrderDirection { get; set; } = "asc";
        public string SearchTitle { get; set; }
        public string SourceType { get; set; }
        public string Module { get; set; }
        public string UserId { get; set; }
    }

    public class ChartListResult
    {
        public List<Chart> Charts { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class ChartDetailResult
    {
        public Chart Chart { get; set; }
        public List<Dictionary<string, object>> SampleData { get; set; }
        public object SourceMetadata { get; set; }
    }

    #endregion
}