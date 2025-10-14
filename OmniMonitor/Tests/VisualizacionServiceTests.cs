using Xunit;
using OmniMonitor.Server.Services;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;

public class VisualizacionServiceTests
{
    private (ApplicationDbContext context, int datasetId) CreateDbContext()
    {
        // Usa la misma configuración que tu test principal
        var inMemorySettings = new Dictionary<string, string?> {
            {"ConnectionStrings:DefaultConnection", "Server=localhost;Database=OmniMonitorDev;Trusted_Connection=True;TrustServerCertificate=True;"}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var context = new ApplicationDbContext(configuration);

        // Limpia y prepara datos de prueba
        context.Visualizaciones.RemoveRange(context.Visualizaciones.ToList());
        context.Datasets.RemoveRange(context.Datasets.ToList());
        context.Users.RemoveRange(context.Users.ToList());
        context.SaveChanges();

        // Usuario de prueba
        context.Users.Add(new User { Username = "user1", Password = "pass" });
        context.SaveChanges();

        // Crear y guardar el dataset
        var dataset = new Dataset { Name = "ds1", Username = "user1", Is_Dataset = "S" };
        context.Datasets.Add(dataset);
        context.SaveChanges();
        var datasetId = dataset.Id; // Usa este Id real

        return (context, datasetId);
    }

    [Fact]
    public async Task CreateVisualizacionAsync_CreatesWithValidDataset()
    {
        var (context, datasetId) = CreateDbContext();
        var service = new VisualizacionService(context);

        var request = new CreateVisualizacionRequest
        {
            Nombre = "Mi Gráfica",
            Username = "user1",
            FechaDesde = DateTime.UtcNow.AddDays(-7),
            FechaHasta = DateTime.UtcNow,
            JsonDiseñoGeneral = "{\"chartType\":\"bar\",\"mappings\":{\"xField\":\"fecha\",\"yField\":\"valor\"},\"appearance\":{\"color\":\"#ff0000\"}}",
            Datasets = new List<DatasetConfig>
            {
                new DatasetConfig { DatasetId = datasetId, JsonDiseño = "{}" }
            }
        };

        var result = await service.CreateVisualizacionAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Mi Gráfica", result.Nombre);
        Assert.Single(result.GrupoDatasets);
        Assert.Equal(datasetId, result.GrupoDatasets.First().DatasetId);
    }

    [Fact]
    public async Task CreateVisualizacionAsync_Throws404_IfDatasetNotExists()
    {
        var (context, _) = CreateDbContext();
        var service = new VisualizacionService(context);

        var request = new CreateVisualizacionRequest
        {
            Nombre = "Gráfica",
            Username = "user1",
            FechaDesde = DateTime.UtcNow.AddDays(-7),
            FechaHasta = DateTime.UtcNow,
            JsonDiseñoGeneral = "{}",
            Datasets = new List<DatasetConfig>
            {
                new DatasetConfig { DatasetId = 999999, JsonDiseño = "{}" }
            }
        };

        // Simula la validación manual (el servicio actual no la hace, deberías agregarla)
        var dataset = context.Datasets.Find(999999);
        if (dataset == null)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateVisualizacionAsync(request));
        }
    }

    [Fact]
    public async Task CreateVisualizacionAsync_Throws403_IfDatasetNotOwned()
    {
        var (context, _) = CreateDbContext();
        var otherDataset = new Dataset { Name = "ds2", Username = "otro", Is_Dataset = "S" };
        context.Datasets.Add(otherDataset);
        context.SaveChanges();
        var otherDatasetId = otherDataset.Id;

        var service = new VisualizacionService(context);

        var request = new CreateVisualizacionRequest
        {
            Nombre = "Gráfica",
            Username = "user1",
            FechaDesde = DateTime.UtcNow.AddDays(-7),
            FechaHasta = DateTime.UtcNow,
            JsonDiseñoGeneral = "{}",
            Datasets = new List<DatasetConfig>
            {
                new DatasetConfig { DatasetId = otherDatasetId, JsonDiseño = "{}" }
            }
        };

        // Simula la validación manual (el servicio actual no la hace, deberías agregarla)
        var dataset = context.Datasets.Find(otherDatasetId);
        if (dataset != null && dataset.Username != "user1")
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateVisualizacionAsync(request));
        }
    }

    [Fact]
    public async Task CreateVisualizacionAsync_Throws422_IfMissingFields()
    {
        var (context, _) = CreateDbContext();
        var service = new VisualizacionService(context);

        var request = new CreateVisualizacionRequest
        {
            Nombre = "", // Falta nombre
            Username = "user1",
            FechaDesde = DateTime.UtcNow.AddDays(-7),
            FechaHasta = DateTime.UtcNow,
            JsonDiseñoGeneral = null,
            Datasets = new List<DatasetConfig>()
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateVisualizacionAsync(request));
    }

    [Fact]
    public async Task GetAllVisualizacionesAsync_ReturnsPagedAndFiltered()
    {
        var (context, datasetId) = CreateDbContext();
        var service = new VisualizacionService(context);

        // Crea varias visualizaciones
        for (int i = 0; i < 10; i++)
        {
            context.Visualizaciones.Add(new Visualizacion
            {
                Nombre = $"Grafica {i}",
                Username = "user1",
                FechaDesde = DateTime.UtcNow.AddDays(-i),
                FechaHasta = DateTime.UtcNow,
                JsonDesign = "{}",
                GrupoDatasets = new List<GrupoDataset>
                {
                    new GrupoDataset { DatasetId = datasetId, JsonDesign = "{}" }
                }
            });
        }
        context.SaveChanges();

        var all = await service.GetAllVisualizacionesAsync("user1");
        Assert.Equal(10, all.Count);
        Assert.Equal("Grafica 9", all.First().Nombre); // Orden descendente por IdVisualizacion
    }

    [Fact]
    public async Task GetVisualizacionByIdAsync_ReturnsDetailWithDatasets()
    {
        var (context, datasetId) = CreateDbContext();
        var service = new VisualizacionService(context);

        var vis = new Visualizacion
        {
            Nombre = "Detalle",
            Username = "user1",
            FechaDesde = DateTime.UtcNow.AddDays(-1),
            FechaHasta = DateTime.UtcNow,
            JsonDesign = "{}",
            GrupoDatasets = new List<GrupoDataset>
            {
                new GrupoDataset { DatasetId = datasetId, JsonDesign = "{}" }
            }
        };
        context.Visualizaciones.Add(vis);
        context.SaveChanges();

        var result = await service.GetVisualizacionByIdAsync(vis.IdVisualizacion, "user1");
        Assert.NotNull(result);
        Assert.Equal("Detalle", result.Nombre);
        Assert.Single(result.GrupoDatasets);
    }

    [Fact]
    public async Task GetVisualizacionByIdAsync_ReturnsNull_IfVisualizacionDoesNotExist()
    {
        var (context, datasetId) = CreateDbContext();
        var service = new VisualizacionService(context);

        // No existe ninguna visualización con Id 99999
        var result = await service.GetVisualizacionByIdAsync(99999, "user1");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVisualizacionByIdAsync_ReturnsNull_IfVisualizacionIsNotOwnedByUser()
    {
        var (context, datasetId) = CreateDbContext();
        var service = new VisualizacionService(context);

        // Creamos una visualización para otro usuario
        var vis = new Visualizacion
        {
            Nombre = "No es tuya",
            Username = "otro",
            FechaDesde = DateTime.UtcNow.AddDays(-1),
            FechaHasta = DateTime.UtcNow,
            JsonDesign = "{}",
            GrupoDatasets = new List<GrupoDataset>
            {
                new GrupoDataset { DatasetId = datasetId, JsonDesign = "{}" }
            }
        };
        context.Visualizaciones.Add(vis);
        context.SaveChanges();

        // user1 intenta acceder a la visualización de "otro"
        var result = await service.GetVisualizacionByIdAsync(vis.IdVisualizacion, "user1");
        Assert.Null(result);
    }
}