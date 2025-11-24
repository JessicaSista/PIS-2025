using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using OmniMonitor.Shared.Dtos.EM;

using Org.BouncyCastle.Math.EC.Multiplier;

using Xunit;

namespace QA.Tests
{
    public class KPIServiceTests : IDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly ApplicationDbContext _context;

        private readonly Mock<IDatasetService> _mockDatasetService = new();
        private readonly Mock<ISondaEMService> _mockSondaEm = new();
        private readonly Mock<ISondaIMService> _mockSondaIm = new();
        private readonly Mock<ISondaAuthService> _mockAuth = new();
        private readonly Mock<IKpiAMService> _mockKpiAm = new();
        private readonly Mock<IDatasetAmService> _mockDatasetAm = new();
        private readonly Mock<IDatasetUMService> _mockDatasetUm = new();
        private readonly Mock<ISondaUMService> _mockSondaUm = new();
        private readonly Mock<ISondaAMService> _mockSondaAm = new();
        private readonly Mock<IDatasetEMService> _mockDatasetEm = new();

        public KPIServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(_dbOptions);

            // ensure database created
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }

        private KpiService CreateService()
        {
            return new KpiService(
                _context,
                _mockDatasetService.Object,
                _mockSondaEm.Object,
                _mockSondaIm.Object,
                _mockAuth.Object,
                _mockKpiAm.Object,
                _mockDatasetAm.Object,
                _mockDatasetUm.Object,
                _mockSondaUm.Object,
                _mockSondaAm.Object,
                _mockDatasetEm.Object
            );
        }

        /* Tests sobre CreateKpiAsync */
        [Fact]
        public async Task CreateKpiAsync_NullRequest_ThrowsArgumentNullException()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<ArgumentNullException>(() => svc.CreateKpiAsync(null!, "user"));
        }

        [Fact]
        public async Task CreateKpiAsync_MissingName_ThrowsArgumentException()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Name = "   ",
                SourceModule = "AM",
                DatasetId = 1
            };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateKpiAsync(req, "user"));
        }

        [Fact]
        public async Task CreateKpiAsync_ValidNonIMModule_CreatesKpiAndSetsLiveAccordingRules()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Name = "MyKpi",
                SourceModule = "AM",
                DatasetId = 1,
                LiveEnabled = true,
                Metric = "count"
            };

            var result = await svc.CreateKpiAsync(req, "owner1");

            Assert.NotNull(result);
            Assert.Equal("MyKpi", result.Name);
            Assert.Equal("AM", result.SourceModule);
            Assert.False(result.LiveEnabled);
            var fromDb = await _context.Kpi.FindAsync(result.Id);
            Assert.NotNull(fromDb);
            Assert.Equal(result.Name, fromDb!.Name);
        }

        [Fact]
        public async Task CreateKpiAsync_HappyPath_IM_ValidatesAndPersists_Fixed()
        {
            var svc = CreateService();

            var req = new KpiRequest
            {
                Name = "Temperature KPI",
                SourceModule = "IM",
                DatasetId = 1,
                Metric = "lastvalue",
                Unit = "C",
                Multiplier = 1.0,
                DefaultColor = "#FF0000",
                LiveEnabled = false,
                Type = 1,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "dateFrom", "2020-01-01T00:00:00Z" },
                    { "dateTo",   "2020-01-02T00:00:00Z" }
                })
            };

            var datasetIm = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "TempSensor", Username = "owner" };
            _context.Set<DatasetIM>().Add(datasetIm);
            await _context.SaveChangesAsync();

            var source = new Source
            {
                Id = 10,
                Devices = new List<Device>
        {
            new Device { Id = 5 }
        }
            };
            _mockSondaIm.Setup(s => s.GetSourceById(10, It.IsAny<string>()))
                        .ReturnsAsync(source);

            var device = new Device
            {
                Id = 5,
                Sensors = new List<Sensor>
        {
            new() { Name = "TempSensor", Type = "double", LastValue = "24.5" }
        }
            };
            _mockSondaIm.Setup(s => s.GetDeviceById(5, It.IsAny<string>()))
                        .ReturnsAsync(device);

            var created = await svc.CreateKpiAsync(req, "owner");

            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("Temperature KPI", created.Name);
            Assert.Equal("IM", created.SourceModule);

            // Persistido correctamente en la BD
            var fromDb = await _context.Kpi.FindAsync(created.Id);
            Assert.NotNull(fromDb);
            Assert.Equal(created.Name, fromDb!.Name);
        }

        [Fact]
        public async Task CreateKpiAsync_IM_MissingDataset_ThrowsInvalidOperationException()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Name = "Temperature KPI",
                SourceModule = "IM",
                DatasetId = 999, // Non-existent dataset
                Metric = "lastvalue",
                Unit = "C",
                Multiplier = 1.0,
                DefaultColor = "#FF0000",
                LiveEnabled = false,
                Type = 1,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "dateFrom", "2020-01-01T00:00:00Z" },
                    { "dateTo",   "2020-01-02T00:00:00Z" }
                })
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateKpiAsync(req, "owner"));
        }

        [Fact]
        public async Task CreateKpiAsync_AM_HappyPath_ValidatesAndPersists()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Name = "AM KPI",
                SourceModule = "AM",
                DatasetId = 2,
                Metric = "average",
                Unit = "units",
                Multiplier = 1.0,
                DefaultColor = "#00FF00",
                LiveEnabled = true,
                Type = 2,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "aggregation", "hourly" }
                })
            };
            var datasetAm = new DatasetAM { DatasetId = 2, Nombre = "AM Dataset", Username = "owner" };
            _context.Set<DatasetAM>().Add(datasetAm);
            await _context.SaveChangesAsync();
            var created = await svc.CreateKpiAsync(req, "owner");
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("AM KPI", created.Name);
            Assert.Equal("AM", created.SourceModule);
            // Persistido correctamente en la BD
            var fromDb = await _context.Kpi.FindAsync(created.Id);
            Assert.NotNull(fromDb);
            Assert.Equal(created.Name, fromDb!.Name);
        }


        [Fact]
        public async Task CreateKpiAsync_UM_HappyPath_ValidatesAndPersists()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Name = "UM KPI",
                SourceModule = "UM",
                DatasetId = 3,
                Metric = "sum",
                Unit = "units",
                Multiplier = 1.0,
                DefaultColor = "#0000FF",
                LiveEnabled = false,
                Type = 3,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "period", "daily" }
                })
            };
            var datasetUm = new DatasetUM { Id = 3, Name = "UM Dataset", Username = "owner" };
            _context.Set<DatasetUM>().Add(datasetUm);
            await _context.SaveChangesAsync();
            var created = await svc.CreateKpiAsync(req, "owner");
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("UM KPI", created.Name);
            Assert.Equal("UM", created.SourceModule);
            // Persistido correctamente en la BD
            var fromDb = await _context.Kpi.FindAsync(created.Id);
            Assert.NotNull(fromDb);
            Assert.Equal(created.Name, fromDb!.Name);
        }

        [Fact]
        public async Task CreateKpiAsync_EM_HappyPath_ValidatesAndPersists()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Name = "EM KPI",
                SourceModule = "EM",
                DatasetId = 4,
                Metric = "max",
                Unit = "units",
                Multiplier = 1.0,
                DefaultColor = "#FFFF00",
                LiveEnabled = true,
                Type = 4,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "window", "weekly" }
                })
            };
            var datasetEm = new DatasetEM { Id = 4, Name = "EM Dataset", Username = "owner" };
            _context.Set<DatasetEM>().Add(datasetEm);
            await _context.SaveChangesAsync();
            var created = await svc.CreateKpiAsync(req, "owner");
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("EM KPI", created.Name);
            Assert.Equal("EM", created.SourceModule);
            // Persistido correctamente en la BD
            var fromDb = await _context.Kpi.FindAsync(created.Id);
            Assert.NotNull(fromDb);
            Assert.Equal(created.Name, fromDb!.Name);
        }

        /* Tests de GetKpiDefinitionAsync */
        [Fact]
        public async Task GetKpiDefinitionAsync_ExistingKpi_ReturnsKpi()
        {
            var svc = CreateService();
            var kpi = new Kpi
            {
                Atributo = "sensor",
                Name = "Test KPI",
                SourceModule = "AM",
                DatasetId = 1
            };
            _context.Kpi.Add(kpi);
            await _context.SaveChangesAsync();
            var result = await svc.GetKpiDefinitionAsync(kpi.Id);
            Assert.NotNull(result);
            Assert.Equal(kpi.Name, result.Name);
            Assert.Equal(kpi.SourceModule, result.SourceModule);
        }

        [Fact]
        public async Task GetKpiDefinitionAsync_NonExistingKpi_ReturnArgumentException()
        {
            var scv = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(async () => await scv.GetKpiDefinitionAsync(999));
        }

        /* Tests sobre CalculateKpiValueAsync */

        [Fact]
        public async Task CalculateKpiValueAsync_IM_ReturnResponseWithValue_WhenOk()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Atributo = "sensor",
                Name = "Temperature KPI",
                SourceModule = "IM",
                DatasetId = 1,
                Metric = "lastvalue",
                Unit = "C",
                Multiplier = 1.0,
                DefaultColor = "#FF0000",
                LiveEnabled = false,
                Type = 1,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "dateFrom", "2020-01-01T00:00:00Z" },
                    { "dateTo",   "2020-01-02T00:00:00Z" }
                })
            };
            var datasetEm = new DatasetEM { Id = 4, Name = "EM Dataset", Username = "owner" };
            _context.Set<DatasetEM>().Add(datasetEm);
            await _context.SaveChangesAsync();
            var datasetIm = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "TempSensor", Username = "owner" };
            _context.Set<DatasetIM>().Add(datasetIm);
            await _context.SaveChangesAsync();
            _mockDatasetService
            .Setup(s => s.GetDatasetIMByIdAsync(datasetIm.Id, It.IsAny<string>()))
            .ReturnsAsync(datasetIm);
            _mockSondaIm.Setup(s => s.GetSensorDataByDate(
            It.Is<int>(id => id == 5),
            It.Is<string>(name => name == "TempSensor"),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>()))
            .ReturnsAsync(new List<SensorData>
            {
                new SensorData { Time = DateTime.Parse("2020-01-01T12:00:00Z"), Data = "24.5" }
            });
            var source = new Source
            {
                Id = 10,
                Devices = new List<Device>
        {
            new Device { Id = 5 }
        }
            };
            _mockSondaIm.Setup(s => s.GetSourceById(10, It.IsAny<string>()))
                        .ReturnsAsync(source);
            var device = new Device
            {
                Id = 5,
                Sensors = new List<Sensor>
        {
            new() { Name = "TempSensor", Type = "double", LastValue = "24.5" }
        }
            };
            _mockSondaIm.Setup(s => s.GetDeviceById(5, It.IsAny<string>()))
                        .ReturnsAsync(device);
            var created = await svc.CreateKpiAsync(req, "owner");
            var result = await svc.CalculateKpiValueAsync(created.Id, "owner");
            Assert.NotNull(result);
            Assert.Equal(24.5 * created.Multiplier, result.Value);
            Assert.Equal("C", result.Unit);
        }

        [Fact]
        public async Task CalculateKpiValueAsync_ReturnArgumentException_WhenModuleNotSupported()
        {
            var scv = CreateService();
            var kpi = new Kpi
            {
                Name = "Bad KPI",
                SourceModule = "$M",
                DatasetId = 1,
                Atributo = "sensor"
            };
            var Dts = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "TempSensor", Username = "owner" };
            _context.DatasetsIM.Add(Dts);
            _context.Kpi.Add(kpi);
            await _context.SaveChangesAsync();
            await Assert.ThrowsAsync<ArgumentException>(async () => await scv.CalculateKpiValueAsync(kpi.Id, "owner"));
        }

        [Fact]
        public async Task CalculateKpiValueAsync_NonExistingKpi_ReturnArgumentException()
        {
            var scv = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(async () => await scv.CalculateKpiValueAsync(999, "owner"));

        }

        /* Tests sobre CalculateKpiValueAsyncSinToken */

        [Fact]
        public async Task CalculateKpiValueAsyncSinToken_IM_ReturnResponseWithValue_WhenOk()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Atributo = "sensor",
                Name = "Temperature KPI",
                SourceModule = "IM",
                DatasetId = 1,
                Metric = "lastvalue",
                Unit = "C",
                Multiplier = 1.0,
                DefaultColor = "#FF0000",
                LiveEnabled = false,
                Type = 1,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "dateFrom", "2020-01-01T00:00:00Z" },
                    { "dateTo",   "2020-01-02T00:00:00Z" }
                })
            };
            var datasetIm = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "TempSensor", Username = "owner" };
            _context.Set<DatasetIM>().Add(datasetIm);
            await _context.SaveChangesAsync();
            _mockDatasetService
            .Setup(s => s.GetDatasetIMByIdAsync(datasetIm.Id, It.IsAny<string>()))
            .ReturnsAsync(datasetIm);
            var source = new Source
            {
                Id = 10,
                Devices = new List<Device>
        {
            new Device { Id = 5 }
        }
            };
            _mockSondaIm.Setup(s => s.GetSourceById(10, It.IsAny<string>()))
                        .ReturnsAsync(source);
            var device = new Device
            {
                Id = 5,
                Sensors = new List<Sensor>
        {
            new() { Name = "TempSensor", Type = "double", LastValue = "24.5" }
        }
            };
            _mockSondaIm.Setup(s => s.GetDeviceById(5, It.IsAny<string>()))
                        .ReturnsAsync(device);
            _mockSondaIm.Setup(s => s.GetSensorDataByDate(
            It.Is<int>(id => id == device.Id),
            It.Is<string>(name => name == datasetIm.SensorName),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>()))
            .ReturnsAsync(new List<SensorData>
            {
            new SensorData
                {
                    Time = DateTime.Parse("2020-01-01T01:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                    Data = "24.5"
                }
            });
            var created = await svc.CreateKpiAsync(req, "owner");
            var result = await svc.CalculateKpiValueAsyncSinToken(created.Id);
            Assert.NotNull(result);
            Assert.Equal(24.5 * created.Multiplier, result.Value);
            Assert.Equal("C", result.Unit);
        }

        [Fact]
        public async Task CalculateKpiValueAsyncSinToken_NonExistingKpi_ReturnArgumentException()
        {
            var scv = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(async () => await scv.CalculateKpiValueAsyncSinToken(999));
        }

        [Fact]
        public async Task CalculateKpiValueAsyncSinToken_ReturnArgumentException_WhenModuleNotSupported()
        {
            var scv = CreateService();
            var kpi = new Kpi
            {
                Name = "Bad KPI",
                SourceModule = "$M",
                DatasetId = 1,
                Atributo = "sensor"
            };
            var Dts = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "TempSensor", Username = "owner" };
            _context.DatasetsIM.Add(Dts);
            _context.Kpi.Add(kpi);
            await _context.SaveChangesAsync();
            await Assert.ThrowsAsync<ArgumentException>(async () => await scv.CalculateKpiValueAsyncSinToken(kpi.Id));
        }

        /* Tests sobre CalculateKpiDataAsync */

        [Fact]
        public async Task CalculateKpiDataAsync_IM_ReturnResponseWithValue_WhenOk()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Atributo = "sensor",
                Name = "Temperature KPI",
                SourceModule = "IM",
                DatasetId = 1,
                Metric = "lastvalue",
                Unit = "C",
                Multiplier = 1.0,
                DefaultColor = "#FF0000",
                LiveEnabled = false,
                Type = 1,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "dateFrom", "2020-01-01T00:00:00Z" },
                    { "dateTo",   "2020-01-02T00:00:00Z" }
                })
            };
            var datasetIm = new DatasetIM { Id = 1, Id_Source = 10, SensorName = "TempSensor", Username = "owner" };
            _context.Set<DatasetIM>().Add(datasetIm);
            await _context.SaveChangesAsync();
            _mockDatasetService
            .Setup(s => s.GetDatasetIMByIdAsync(datasetIm.Id, It.IsAny<string>()))
            .ReturnsAsync(datasetIm);
            var source = new Source
            {
                Id = 10,
                Devices = new List<Device>
        {
            new Device { Id = 5 }
        }
            };
            _mockSondaIm.Setup(s => s.GetSourceById(10, It.IsAny<string>()))
                        .ReturnsAsync(source);
            var device = new Device
            {
                Id = 5,
                Sensors = new List<Sensor>
                {
                    new() { Name = "TempSensor", Type = "double", LastValue = "24.5" }
                }
            };
            _mockSondaIm.Setup(s => s.GetDeviceById(5, It.IsAny<string>()))
                        .ReturnsAsync(device);
            _mockSondaIm.Setup(s => s.GetSensorDataByDate(
            It.Is<int>(id => id == device.Id),
            It.Is<string>(name => name == datasetIm.SensorName),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>()))
            .ReturnsAsync(new List<SensorData>
            {
            new SensorData
                {
                    Time = DateTime.Parse("2020-01-01T01:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                    Data = "24.5"
                }
            });
            var result = await svc.CalculateKpiDataAsync(req, "owner");
            Assert.NotNull(result);
            Assert.Equal(24.5 * 1.0, result.Value);
            Assert.Equal("C", result.Unit);
        }

        [Fact]
        public async Task CalculateKpiDataAsync_ReturnArgumentException_WhenModuleNotSupported()
        {
            var scv = CreateService();
            var req = new KpiRequest
            {
                Name = "Bad KPI",
                SourceModule = "$M",
                DatasetId = 1,
                Atributo = "sensor"
            };
            await Assert.ThrowsAsync<ArgumentException>(async () => await scv.CalculateKpiDataAsync(req, "owner"));

        }

        [Fact]
        public async Task CalculateKpiDataAsync_IM_MissingDataset_ThrowsInvalidOperationException()
        {
            var svc = CreateService();
            var req = new KpiRequest
            {
                Name = "Temperature KPI",
                SourceModule = "IM",
                DatasetId = 999,
                Metric = "lastvalue",
                Unit = "C",
                Multiplier = 1.0,
                DefaultColor = "#FF0000",
                LiveEnabled = false,
                Type = 1,
                ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "dateFrom", "2020-01-01T00:00:00Z" },
                    { "dateTo",   "2020-01-02T00:00:00Z" }
                })
            };
            await Assert.ThrowsAsync<Exception>(() => svc.CalculateKpiDataAsync(req, "owner"));
        }

        /* Tests sobre GetAllKpisForUserAsync */

        [Fact]
        public async Task GetAllKpisForUserAsync_ReturnAllKpis_WhenExist()
        {
            var svc = CreateService();
            var kpi1 = new Kpi { Atributo = "sensor", Name = "First KPI", SourceModule = "IM", DatasetId = 1, Username = "testuser" };
            var kpi2 = new Kpi { Atributo = "events", Name = "Second KPI", SourceModule = "AM", DatasetId = 2, Username = "testuser" };
            _context.Kpi.AddRange(kpi1, kpi2);
            await _context.SaveChangesAsync();
            var result = await svc.GetAllKpisForUserAsync("testuser");
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("First KPI", result[0].Name);
            Assert.Equal("Second KPI", result[1].Name);
        }

        [Fact]
        public async Task GetAllKpisForUserAsync_ReturnEmpty_WhenNotExist()
        {
            var svc = CreateService();
            var result = await svc.GetAllKpisForUserAsync("testuser");
            Assert.Empty(result);
        }

        /* Tests sobre GetMetricInfoListAsync */

        [Fact]
        public async Task GetMetricInfoListAsync_ReturnMetricsForIM_ForSupportedModule()
        {
            var svc = CreateService();
            var imMetrics = await svc.GetMetricInfoListAsync("IM");
            Assert.NotNull(imMetrics);
            Assert.Contains(imMetrics, m => m.Name == "lastValue");
            Assert.Contains(imMetrics, m => m.Name == "average");
            Assert.Contains(imMetrics, m => m.Name == "minValue");
            Assert.Contains(imMetrics, m => m.Name == "maxValue");
        }

        [Fact]
        public async Task GetMetricInforListAsync_ReturnArgumentException_ForUnsupportedModule()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(async () => await svc.GetMetricInfoListAsync("$M"));
        }

        /* Tests sobre DeleteKpiAsync */

        [Fact]
        public async Task DeleteKpiAsync_DeleteKpi_WhenExist()
        {
            var svc = CreateService();
            var kpi1 = new Kpi { Id = 1, Atributo = "sensor", Name = "KPI to Delete", SourceModule = "IM", DatasetId = 1, Username = "testuser" };
            _context.Kpi.Add(kpi1);
            await _context.SaveChangesAsync();
            await svc.DeleteKpiAsync(kpi1.Id, "testuser");
            var fromDb = await _context.Kpi.FindAsync(kpi1.Id);
            Assert.Null(fromDb);
        }

        [Fact]
        public async Task DeleteKpiAsync_ThrowsKeyNotFoundException_WhenKpiDoesNotExist()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await svc.DeleteKpiAsync(999, "testuser"));
        }

        [Fact]
        public async Task DeleteKpiAsync_ReturnUnauthorizedAccessException_WhenUserNotOwner()
        {
            var svc = CreateService();
            var Kpi = new Kpi { Id = 1, Atributo = "sensor", Name = "KPI Unauthorized", SourceModule = "IM", Username = "testuser" };
            _context.Kpi.Add(Kpi);
            await _context.SaveChangesAsync();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await svc.DeleteKpiAsync(Kpi.Id, "otheruser"));
        }

        /* Tests sobre UpdateKpiAsync */

        [Fact]
        public async Task UpdateKpiAsync_NullRequest_ThrowsArgumentNullException()
        {
            var svc = CreateService();
            // prepare existing KPI so the method reaches the null request check early
            var k = new Kpi { Id = 1, Atributo = "sensor", Name = "Original", SourceModule = "IM", DatasetId = 1, Username = "owner" };
            _context.Kpi.Add(k);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentNullException>(() => svc.UpdateKpiAsync(k.Id, null!, "owner"));
        }

        [Fact]
        public async Task UpdateKpiAsync_KpiNotFound_ThrowsKeyNotFoundException()
        {
            var svc = CreateService();
            var req = new KpiRequest { Name = "NewName" };
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdateKpiAsync(999, req, "owner"));
        }

        [Fact]
        public async Task UpdateKpiAsync_UsernameMismatch_ThrowsUnauthorizedAccessException()
        {
            var svc = CreateService();
            var k = new Kpi { Id = 1, Atributo = "sensor", Name = "Original", SourceModule = "IM", DatasetId = 1, Username = "owner" };
            _context.Kpi.Add(k);
            await _context.SaveChangesAsync();

            var req = new KpiRequest { Name = "Original" }; // keep name unchanged to avoid duplicate-name validation
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.UpdateKpiAsync(k.Id, req, "otherUser"));
        }

        [Fact]
        public async Task UpdateKpiAsync_NameProvidedButEmpty_ThrowsArgumentException()
        {
            var svc = CreateService();
            var k = new Kpi { Id = 1, Atributo = "sensor", Name = "Original", SourceModule = "IM", DatasetId = 1, Username = "owner" };
            _context.Kpi.Add(k);
            await _context.SaveChangesAsync();

            var req = new KpiRequest { Name = "   " };
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateKpiAsync(k.Id, req, "owner"));
            Assert.Contains("Name provisto pero vacío", ex.Message);
        }

        [Fact]
        public async Task UpdateKpiAsync_InvalidDatasetId_ThrowsArgumentOutOfRangeException()
        {
            var svc = CreateService();
            var k = new Kpi { Id = 1, Atributo = "sensor", Name = "Original", SourceModule = "IM", DatasetId = 1, Username = "owner" };
            _context.Kpi.Add(k);
            await _context.SaveChangesAsync();

            var req = new KpiRequest { DatasetId = -10 };
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => svc.UpdateKpiAsync(k.Id, req, "owner"));
        }

        [Fact]
        public async Task UpdateKpiAsync_InvalidMultiplier_ThrowsArgumentOutOfRangeException()
        {
            var svc = CreateService();
            var k = new Kpi { Id = 1, Atributo = "sensor", Name = "Original", SourceModule = "IM", DatasetId = 1, Username = "owner" };
            _context.Kpi.Add(k);
            await _context.SaveChangesAsync();

            var req = new KpiRequest { Multiplier = 0 };
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => svc.UpdateKpiAsync(k.Id, req, "owner"));
        }

        [Fact]
        public async Task UpdateKpiAsync_MetricRequiresExtraInfo_MissingOrInvalid_ThrowsArgumentException()
        {
            var svc = CreateService();
            var k = new Kpi
            {
                Id = 1,
                Atributo = "sensor",
                Name = "Original",
                SourceModule = "AM",
                DatasetId = 1,
                Username = "owner",
                Metric = "average",
                ExtraInfo = null // existing KPI has no extraInfo
            };
            _context.Kpi.Add(k);
            await _context.SaveChangesAsync();

            // Request tries to set metric requiring extra info but does not provide ExtraInfo
            var req = new KpiRequest { Metric = "average", ExtraInfo = null };
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateKpiAsync(k.Id, req, "owner"));
            Assert.Contains("ExtraInfo requerida", ex.Message);

            // Invalid JSON ExtraInfo also should fail
            var reqInvalidJson = new KpiRequest { Metric = "average", ExtraInfo = "not-json" };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateKpiAsync(k.Id, reqInvalidJson, "owner"));

            // Malformed date in extraInfo should also fail
            var badDates = JsonSerializer.Serialize(new Dictionary<string, string>
    {
        { "dateFrom", "invalid-date" },
        { "dateTo", "invalid-date" }
    });
            var reqBadDates = new KpiRequest { Metric = "average", ExtraInfo = badDates };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateKpiAsync(k.Id, reqBadDates, "owner"));
        }

        [Fact]
        public async Task UpdateKpiAsync_ValidRequest_UpdatesFieldsAndReturnsKpi()
        {
            var svc = CreateService();
            var originalExtra = JsonSerializer.Serialize(new Dictionary<string, string>
    {
        { "dateFrom", "2020-01-01T00:00:00Z" },
        { "dateTo", "2020-01-02T00:00:00Z" }
    });

            var k = new Kpi
            {
                Id = 1,
                Atributo = "sensor",
                Name = "OriginalName",
                Description = "orig",
                SourceModule = "IM",
                DatasetId = 1,
                Unit = "C",
                Metric = "lastvalue",
                Multiplier = 1.0,
                DefaultColor = "#FF0000",
                ColorRanges = null,
                Username = "owner",
                ExtraInfo = originalExtra,
                LiveEnabled = false,
                Link = null
            };
            _context.Kpi.Add(k);
            await _context.SaveChangesAsync();

            var newExtra = JsonSerializer.Serialize(new Dictionary<string, string>
    {
        { "dateFrom", "2021-01-01T00:00:00Z" },
        { "dateTo", "2021-01-02T00:00:00Z" }
    });

            var req = new KpiRequest
            {
                Name = " UpdatedName ",
                Description = " updated desc ",
                SourceModule = "AM",            // changes module to AM
                DatasetId = 2,
                Unit = "F",
                Metric = "average",
                Multiplier = 2.5,
                DefaultColor = "#00FF00",
                ColorRanges = null,
                ExtraInfo = newExtra,
                Link = "  http://example  ",
                LiveEnabled = true
            };

            // create datasetAM so any dataset checks pass if service validates existence for AM
            var datasetAm = new DatasetAM { DatasetId = 2, Nombre = "AM Dataset", Username = "owner" };
            _context.Set<DatasetAM>().Add(datasetAm);
            await _context.SaveChangesAsync();

            var updated = await svc.UpdateKpiAsync(k.Id, req, "owner");

            Assert.NotNull(updated);
            Assert.Equal("UpdatedName", updated.Name);
            Assert.Equal("updated desc", updated.Description);
            Assert.Equal("AM", updated.SourceModule);
            Assert.Equal(2, updated.DatasetId);
            Assert.Equal("F", updated.Unit);
            Assert.Equal("average", updated.Metric);
            Assert.Equal(2.5, updated.Multiplier);
            Assert.Equal("#00FF00", updated.DefaultColor);
            Assert.Equal(newExtra, updated.ExtraInfo);
            Assert.Equal("  http://example  ", updated.Link);
            Assert.False(updated.LiveEnabled);
        }

        /* Tests sobre GetFieldValueAsync */

        [Fact]
        public async Task GetFieldValuesAsync_InvalidDatasetId_ThrowsArgumentException()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetFieldValuesAsync(0, "AM", "nombre", 1, "user"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetFieldValuesAsync_InvalidModulo_ThrowsArgumentException(string modulo)
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetFieldValuesAsync(1, modulo!, "nombre", 1, "user"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetFieldValuesAsync_InvalidCampo_ThrowsArgumentException(string campo)
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetFieldValuesAsync(1, "AM", campo!, 1, "user"));
        }

        [Fact]
        public async Task GetFieldValuesAsync_AM_Assets_ReturnsDistinctOrdered()
        {
            var svc = CreateService();

            // Ensure datasetAM exists for the DB check in the service
            _context.Set<DatasetAM>().Add(new DatasetAM { Id_Dataset = 10, Nombre = "ds", Username = "u1" });
            await _context.SaveChangesAsync();

            // Prepare reduced assets returned by datasetAm service
            var reduced = new List<DatasetReducedAMDTO>
    {
        new() { nombre = "Zeta", codigo = "C1" },
        new() { nombre = "Alpha", codigo = "C2" },
        new() { nombre = "Alpha", codigo = "C2" } // duplicate to verify distinct
    };

            _mockDatasetAm
                .Setup(s => s.GetReducedAssetsByDatasetIdAsync(10, It.IsAny<string>()))
                .ReturnsAsync(reduced);

            _mockKpiAm
                .Setup(s => s.GetFieldValuesAsync(It.IsAny<List<DatasetReducedAMDTO>>(), "nombre"))
                .ReturnsAsync(reduced.Select(r => r.nombre!).Distinct().OrderBy(v => v).ToList());

            var result = await svc.GetFieldValuesAsync(10, "AM", "nombre", 1, "u1");

            Assert.Equal(new List<string> { "Alpha", "Zeta" }, result);
        }

        [Fact]
        public async Task GetFieldValuesAsync_AM_Events_UsesDatasetAm_GetReducedEvents()
        {
            var svc = CreateService();

            _context.Set<DatasetAM>().Add(new DatasetAM { Id_Dataset = 11, Nombre = "ds", Username = "u1" });
            await _context.SaveChangesAsync();

            var reducedEvents = new List<DatasetReducedAMEventsDTO>
    {
        new() { subject = "Evt1", eventTask = "T1" },
        new() { subject = "Evt2", eventTask = "T2" }
    };

            _mockDatasetAm
                .Setup(s => s.GetReducedEventsByDatasetIdAsync(11, It.IsAny<string>()))
                .ReturnsAsync(reducedEvents);

            _mockKpiAm
                .Setup(s => s.GetFieldValuesAsync(It.IsAny<List<DatasetReducedAMEventsDTO>>(), "subject"))
                .ReturnsAsync(reducedEvents.Select(r => r.subject!).Distinct().OrderBy(v => v).ToList());

            var result = await svc.GetFieldValuesAsync(11, "AM", "subject", 2, "u1");

            Assert.Equal(new List<string> { "Evt1", "Evt2" }, result);
        }

        [Fact]
        public async Task GetFieldValuesAsync_AM_Stock_ReturnsFieldValuesFromStockAggregation()
        {
            var svc = CreateService();

            // Create datasetAM with Grupo_Event_Task_Instance -> Grupo_Stock
            var datasetAm = new DatasetAM
            {
                Id_Dataset = 20,
                Nombre = "ds",
                Username = "u1",
                Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance>
        {
            new DatasetEventTaskInstance
            {
                Id_Event_Task_Instance = 100,
                Grupo_Stock = new List<DatasetStock>
                {
                    new() { Id_Stock = 501 },
                    new() { Id_Stock = 502 }
                }
            }
        }
            };

            _context.Set<DatasetAM>().Add(datasetAm);
            await _context.SaveChangesAsync();

            _mockDatasetAm
                .Setup(s => s.GetDatasetAMByIdAsync(20, It.IsAny<string>()))
                .ReturnsAsync(datasetAm);

            // Mock sondaAM.GetStockById to return stock DTOs with Provider names
            _mockSondaAm
                .Setup(s => s.GetStockById(501, It.IsAny<string>()))
                .ReturnsAsync(new StockDto { Id = 501, Name = "StockA", Provider = new ProviderDto { Name = "Prov1" }, Quantity = 3, Sku = "SKU1", Minimum = 1 });
            _mockSondaAm
                .Setup(s => s.GetStockById(502, It.IsAny<string>()))
                .ReturnsAsync(new StockDto { Id = 502, Name = "StockB", Provider = new ProviderDto { Name = "Prov2" }, Quantity = 7, Sku = "SKU2", Minimum = 2 });

            _mockKpiAm
                .Setup(s => s.GetFieldValuesAsync(It.IsAny<List<ReducedStockDatasetAM>>(), "Proveedor"))
                .ReturnsAsync(new List<string> { "Prov1", "Prov2" });

            var result = await svc.GetFieldValuesAsync(20, "AM", "Proveedor", 3, "u1");

            Assert.Equal(new List<string> { "Prov1", "Prov2" }, result);
        }

        [Fact]
        public async Task GetFieldValuesAsync_EM_Alerts_ExtractsFields()
        {
            var svc = CreateService();

            var datasetEm = new DatasetEM
            {
                Id = 30,
                Name = "ds",
                Username = "u2",
                DatasetAlerts = new List<DatasetAlert> { new() { Id_alert = 900 } }
            };

            _context.DatasetsEM.Add(datasetEm);
            await _context.SaveChangesAsync();
            _mockDatasetEm
                .Setup(s => s.GetDatasetEMByIdAsync(30, It.IsAny<string>()))
                .ReturnsAsync(datasetEm);

            _mockSondaEm
                .Setup(s => s.GetAlertById(900, It.IsAny<string>()))
                .ReturnsAsync(new OmniMonitor.Shared.Dtos.EM.AlertDto { AlertName = "A1", SourceId = 7, AlertState = "Active", SourceAddress = "Addr1" });

            var result = await svc.GetFieldValuesAsync(30, "EM", "Nombre", 1, "u2");

            Assert.Single(result);
            Assert.Equal("A1", result[0]);
        }

        [Fact]
        public async Task GetFieldValuesAsync_EM_Events_ExtractsFields()
        {
            var svc = CreateService();

            var datasetEm = new DatasetEM
            {
                Id = 31,
                Name = "ds",
                Username = "u2",
                DatasetEvents = new List<DatasetEventEM> { new() { Id_event = 200 } }
            };

            _context.DatasetsEM.Add(datasetEm);
            await _context.SaveChangesAsync();
            _mockDatasetEm
                .Setup(s => s.GetDatasetEMByIdAsync(31, It.IsAny<string>()))
                .ReturnsAsync(datasetEm);

            _mockSondaEm
                .Setup(s => s.GetEvents(null, null, null, null, It.IsAny<string>()))
                .ReturnsAsync(new List<EventDto>
                {
            new() { Id = 200, Name = "EvX", Origin = "Org", State = "S", Address = new AddressDto { DisplayName = "D1" } }
                });

            var result = await svc.GetFieldValuesAsync(31, "EM", "Nombre", 2, "u2");

            Assert.Single(result);
            Assert.Equal("EvX", result[0]);
        }

        [Fact]
        public async Task GetFieldValuesAsync_EM_Fallback_UsesDatasetProperties()
        {
            var svc = CreateService();

            var datasetEm = new DatasetEM
            {
                Id = 32,
                Name = "NameX",
                Username = "u2",
                DatasetAlerts = new List<DatasetAlert>()
            };

            _context.DatasetsEM.Add(datasetEm);
            await _context.SaveChangesAsync();
            _mockDatasetEm
                .Setup(s => s.GetDatasetEMByIdAsync(32, It.IsAny<string>()))
                .ReturnsAsync(datasetEm);

            var result = await svc.GetFieldValuesAsync(32, "EM", "Name", 0, "u2");

            Assert.Single(result);
            Assert.Equal("NameX", result[0]);
        }

        [Fact]
        public async Task GetFieldValuesAsync_UM_Events_And_News_ExtractsFields()
        {
            var svc = CreateService();

            var datasetUm = new DatasetUM
            {
                Id = 40,
                Name = "ds",
                Username = "u3",
                DatasetEvents = new List<DatasetEvent> { new() { Id_event = 300 } },
                DatasetNews = new List<DatasetNews> { new() { Id_news = 400 } }
            };

            _context.DatasetsUM.Add(datasetUm);
            await _context.SaveChangesAsync();
            _mockDatasetUm
                .Setup(s => s.GetDatasetUMByIdAsync(40, It.IsAny<string>()))
                .ReturnsAsync(datasetUm);

            _mockSondaUm
                .Setup(s => s.GetEventById(300, It.IsAny<string>()))
                .ReturnsAsync(new Event { Id = 300, Name = "UEvent", Description = "Desc", Date = DateTime.Parse("2020-01-01") });

            _mockSondaUm
                .Setup(s => s.GetNewsById(400, It.IsAny<string>()))
                .ReturnsAsync(new News { Id = 400, Title = "Title1", Summary = "Sum", Description = "Desc", Categories = new List<Category> { new() { Name = "Cat1" } } });

            var eventsRes = await svc.GetFieldValuesAsync(40, "UM", "Nombre", 1, "u3");
            Assert.Single(eventsRes);
            Assert.Equal("UEvent", eventsRes[0]);

            var newsCats = await svc.GetFieldValuesAsync(40, "UM", "Categoria", 2, "u3");
            Assert.Single(newsCats);
            Assert.Equal("Cat1", newsCats[0]);
        }

        [Fact]
        public async Task GetFieldValuesAsync_IM_ThrowsNotSupportedException()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<NotSupportedException>(() => svc.GetFieldValuesAsync(1, "IM", "whatever", 1, "u1"));
        }

        [Fact]
        public async Task GetFieldValuesAsync_FieldDoesNotExist_ThrowsArgumentException()
        {
            var svc = CreateService();

            var datasetEm = new DatasetEM { Id = 50, Name = "Name50", Username = "u9" };
            _context.DatasetsEM.Add(datasetEm);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetFieldValuesAsync(50, "EM", "NonExistentProperty", 0, "u9"));
        }

        /* Tests sobre GetAllKpisPaginatedAsync */
        [Fact]
        public async Task GetAllKpisPaginatedAsync_ReturnsPaginatedResult_Basic()
        {
            var svc = CreateService();

            // arrange: 5 KPIs for user
            for (int i = 1; i <= 5; i++)
            {
                _context.Kpi.Add(new Kpi
                {
                    Atributo = "sensor",
                    Name = $"KPI {i}",
                    SourceModule = "IM",
                    DatasetId = i,
                    Username = "user1",
                    DefaultColor = "#000000"
                });
            }
            await _context.SaveChangesAsync();

            // mock dataset name resolution (IM)
            for (int i = 1; i <= 5; i++)
            {
                _mockDatasetService
                    .Setup(s => s.GetDatasetIMByIdAsync(i, It.IsAny<string>()))
                    .ReturnsAsync(new DatasetIM { Id = i, Name = $"DatasetIM-{i}", Username = "user1" });
            }

            // act: pageSize 2 -> totalPages = 3
            var resPage1 = await svc.GetAllKpisPaginatedAsync("user1", page: 1, pageSize: 2);

            // assert
            Assert.Equal(5, resPage1.TotalCount);
            Assert.Equal(3, resPage1.TotalPages);
            Assert.Equal(1, resPage1.Page);
            Assert.Equal(2, resPage1.Items.Count);
            Assert.False(resPage1.HasPreviousPage);
            Assert.True(resPage1.HasNextPage);
            Assert.All(resPage1.Items, it => Assert.NotNull(it.DatasetName));
        }

        [Fact]
        public async Task GetAllKpisPaginatedAsync_PageGreaterThanTotal_AdjustsToLastPage()
        {
            var svc = CreateService();

            // arrange: 3 KPIs
            for (int i = 1; i <= 3; i++)
            {
                _context.Kpi.Add(new Kpi
                {
                    Atributo = "asset",
                    Name = $"A{i}",
                    SourceModule = "AM",
                    DatasetId = 10 + i,
                    Username = "u2",
                    DefaultColor = "#111111"
                });
            }
            await _context.SaveChangesAsync();

            // mock AM dataset names
            for (int i = 1; i <= 3; i++)
            {
                _mockDatasetAm
                    .Setup(s => s.GetDatasetAMByIdAsync(10 + i, It.IsAny<string>()))
                    .ReturnsAsync(new DatasetAM { Id_Dataset = 10 + i, Nombre = $"AMDS-{i}", Username = "u2" });
            }

            // request page 5 with pageSize 2 -> totalPages = 2 -> page should be clamped to 2
            var res = await svc.GetAllKpisPaginatedAsync("u2", page: 5, pageSize: 2);

            Assert.Equal(3, res.TotalCount);
            Assert.Equal(2, res.TotalPages);
            Assert.Equal(2, res.Page);
            Assert.True(res.HasPreviousPage);
            Assert.False(res.HasNextPage);
            Assert.Equal(1, res.Items.Count); // last page has single item
            Assert.Equal("AMDS-3", res.Items.Single().DatasetName);
        }

        [Fact]
        public async Task GetAllKpisPaginatedAsync_QueryFiltersByNameOrDescription()
        {
            var svc = CreateService();

            _context.Kpi.Add(new Kpi { Atributo = "event", Name = "Pump Monitor", Description = "Monitors pumps", SourceModule = "UM", DatasetId = 100, Username = "filterUser" });
            _context.Kpi.Add(new Kpi { Atributo = "event", Name = "Light KPI", Description = "Street lights", SourceModule = "UM", DatasetId = 101, Username = "filterUser" });
            _context.Kpi.Add(new Kpi {Atributo = "event", Name = "Pump Extra", Description = "extra", SourceModule = "UM", DatasetId = 102, Username = "filterUser" });
            await _context.SaveChangesAsync();

            // mock UM dataset names
            _mockDatasetUm.Setup(s => s.GetDatasetUMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                          .ReturnsAsync((int id, string user) => new DatasetUM { Id = id, Name = $"UM-{id}", Username = user });

            // search "Pump" should return two items ordered by Name
            var res = await svc.GetAllKpisPaginatedAsync("filterUser", page: 1, pageSize: 10, query: "Pump");

            Assert.Equal(2, res.TotalCount);
            Assert.Equal(1, res.TotalPages);
            Assert.Equal(2, res.Items.Count);
            Assert.All(res.Items, it => Assert.Contains("Pump", it.Name));
        }

        [Fact]
        public async Task GetAllKpisPaginatedAsync_EmptyResult_ReturnsEmptyPage()
        {
            var svc = CreateService();

            // no KPIs for this user
            var res = await svc.GetAllKpisPaginatedAsync("noKpisUser", page: 1, pageSize: 5);

            Assert.Equal(0, res.TotalCount);
            Assert.Equal(0, res.TotalPages);
            Assert.Equal(1, res.Page); // service normalizes page to 1 when totalPages == 0
            Assert.Empty(res.Items);
            Assert.False(res.HasPreviousPage);
            Assert.False(res.HasNextPage);
        }

        [Fact]
        public async Task GetAllKpisPaginatedAsync_UsesGetDatasetNameFromModule_ForAllModules()
        {
            var svc = CreateService();

            // Add KPIs for each module to verify dataset name resolution via corresponding services
            _context.Kpi.Add(new Kpi { Atributo = "sensor", Name = "kIM", SourceModule = "IM", DatasetId = 201, Username = "muser" });
            _context.Kpi.Add(new Kpi { Atributo = "asset", Name = "kAM", SourceModule = "AM", DatasetId = 202, Username = "muser" });
            _context.Kpi.Add(new Kpi { Atributo = "event", Name = "kUM", SourceModule = "UM", DatasetId = 203, Username = "muser" });
            _context.Kpi.Add(new Kpi { Atributo = "alert", Name = "kEM", SourceModule = "EM", DatasetId = 204, Username = "muser" });
            await _context.SaveChangesAsync();

            _mockDatasetService.Setup(s => s.GetDatasetIMByIdAsync(201, It.IsAny<string>()))
                .ReturnsAsync(new DatasetIM { Id = 201, Name = "IM-N" });

            _mockDatasetAm.Setup(s => s.GetDatasetAMByIdAsync(202, It.IsAny<string>()))
                .ReturnsAsync(new DatasetAM { Id_Dataset = 202, Nombre = "AM-N" });

            _mockDatasetUm.Setup(s => s.GetDatasetUMByIdAsync(203, It.IsAny<string>()))
                .ReturnsAsync(new DatasetUM { Id = 203, Name = "UM-N" });

            _mockDatasetEm.Setup(s => s.GetDatasetEMByIdAsync(204, It.IsAny<string>()))
                .ReturnsAsync(new DatasetEM { Id = 204, Name = "EM-N" });

            var res = await svc.GetAllKpisPaginatedAsync("muser", page: 1, pageSize: 10);

            // validate every item has corresponding dataset name
            var dict = res.Items.ToDictionary(i => i.Name, i => i.DatasetName);
            Assert.Equal("IM-N", dict["kIM"]);
            Assert.Equal("AM-N", dict["kAM"]);
            Assert.Equal("UM-N", dict["kUM"]);
            Assert.Equal("EM-N", dict["kEM"]);
        }
    }
}
