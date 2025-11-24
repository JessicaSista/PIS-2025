using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using OmniMonitor.Shared.Dtos.EM;
using OmniMonitor.Shared.Dtos.UM;

namespace QA.Tests
{
        public class KpiAMServiceTests : IDisposable
        {
            private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
            private readonly ApplicationDbContext _context;

            private readonly Mock<ISondaAMService> _mockSondaAm = new();
            private readonly Mock<IDatasetAmService> _mockDatasetAm = new();

            public KpiAMServiceTests()
            {
                _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;
                _context = new ApplicationDbContext(_dbOptions);
                _context.Database.EnsureCreated();
            }

            public void Dispose()
            {
                _context?.Database.EnsureDeleted();
                _context?.Dispose();
            }

            private KpiAMService CreateService()
            {
                return new KpiAMService(_context, _mockSondaAm.Object, _mockDatasetAm.Object);
            }

            private Kpi MakeKpi(string metric, string atributo = "", string extraInfo = null, string defaultColor = "#000000", string colorRanges = null, double? multiplier = null, int? type = null)
            {
                return new Kpi
                {
                    Id = 1,
                    Name = "TestKpi",
                    Metric = metric,
                    Atributo = atributo ?? string.Empty,
                    ExtraInfo = extraInfo,
                    DefaultColor = defaultColor,
                    ColorRanges = colorRanges,
                    Multiplier = multiplier,
                    Type = type
                };
            }

            /* Tests sobre GetFieldValuesAsync */

            [Fact]
            public async Task GetFieldValuesAsync_ReturnsDistinctOrderedValues()
            {
                var svc = CreateService();

                var items = new List<DatasetReducedAMDTO>
            {
                new() { nombre = "Zeta", codigo = "C1" },
                new() { nombre = "Alpha", codigo = "C2" },
                new() { nombre = "Alpha", codigo = "C3" } // duplicate nombre
            };

                var result = await svc.GetFieldValuesAsync(items, "nombre");

                Assert.Equal(new List<string> { "Alpha", "Zeta" }, result);
            }

        [Fact]
        public async Task CalculateAmKpiAsync_EM_Alerts_CountMetric_Works()
        {
            var svc = CreateService();

            var kpi = MakeKpi("count", atributo: "Estado", extraInfo: "Activo", multiplier: 1.0);

            // Build list of EM DatasetReducedAlertEMDTO with Estado property
            var alerts = new List<DatasetReducedAlertEMDTO>
    {
        new() { Nombre = "A1", Estado = "Activo", Fuente = "1" },
        new() { Nombre = "A2", Estado = "Inactivo", Fuente = "2" },
        new() { Nombre = "A3", Estado = "Activo", Fuente = "1" }
    };

            var res = await svc.CalculateAmKpiAsync(kpi, "user", alerts);

            Assert.NotNull(res);
            Assert.Equal("count", res.Type);
            // coincidencias = 2
            Assert.Equal(2.0, Convert.ToDouble(res.Value));
            Assert.Equal(kpi.DefaultColor, res.ActualColor);
        }

        [Fact]
        public async Task CalculateAmKpiAsync_EM_Events_PercentageMetric_Works()
        {
            var svc = CreateService();

            var kpi = MakeKpi("percentage", atributo: "Estado", extraInfo: "S", multiplier: 1.0);

            var events = new List<DatasetReducedEventEMDTO>
    {
        new() { Nombre = "E1", Estado = "S", Origen = "X", Direccion = "D1" },
        new() { Nombre = "E2", Estado = "N", Origen = "X", Direccion = "D2" },
        new() { Nombre = "E3", Estado = "S", Origen = "Y", Direccion = "D3" },
        new() { Nombre = "E4", Estado = "N", Origen = "Y", Direccion = "D4" }
    };

            var res = await svc.CalculateAmKpiAsync(kpi, "user", events);

            Assert.NotNull(res);
            Assert.Equal("average", res.Type);
            // 2/4 => 50%
            Assert.Equal(50.0, Convert.ToDouble(res.Value));
            Assert.Equal("%", res.Unit);
        }

        [Fact]
        public async Task CalculateAmKpiAsync_UM_Events_StateMetric_SingleItem_UsesStringAndColorParsing()
        {
            var svc = CreateService();

            // Color ranges: 0..5 -> #111, 6..10 -> #222
            var ranges = JsonSerializer.Serialize(new List<KpiAMService.ColorRange>
    {
        new() { min = 0, max = 5, color = "#111111" },
        new() { min = 6, max = 10, color = "#222222" }
    });

            var kpi = MakeKpi("state", atributo: "Tipo", defaultColor: "#000000", colorRanges: ranges);

            // For UM events reduced DTO the Tipo property is mapped to Tipo (string)
            var ums = new List<DatasetReducedEventsUMDTO>
    {
        new() { Nombre = "U1", Tipo = "7", Descripcion = "d", Fecha = "2020-01-01 00:00:00", Aprobacion = true }
    };

            var res = await svc.CalculateAmKpiAsync(kpi, "user", ums);

            Assert.NotNull(res);
            Assert.Equal("state", res.Type);
            // single item -> returns the string "7" as Value
            Assert.Equal("7", res.Value);
            // parsed numeric 7 falls into 6..10 -> color #222222
            Assert.Equal("#222222", res.ActualColor);
        }

        [Fact]
        public async Task CalculateAmKpiAsync_UM_News_CountOnCategoria_Works()
        {
            var svc = CreateService();

            var kpi = MakeKpi("count", atributo: "Categoria", extraInfo: null, multiplier: 1.0);

            var newsDtos = new List<DatasetReducedNewsUMDTO>
    {
        new() { Titulo = "N1", Categoria = "CatA", Resumen = "r", Descripcion = "d" },
        new() { Titulo = "N2", Categoria = "CatB", Resumen = "r", Descripcion = "d" },
        new() { Titulo = "N3", Categoria = "CatA", Resumen = "r", Descripcion = "d" }
    };

            var res = await svc.CalculateAmKpiAsync(kpi, "user", newsDtos);

            Assert.NotNull(res);
            Assert.Equal("count", res.Type);
            // atributo "Categoria" -> coincidencias where Categoria == ExtraInfo (extraInfo null -> compare to empty string) => none
            // But since ExtraInfo is null, Count compares against empty string -> 0
            Assert.Equal(0.0, Convert.ToDouble(res.Value));
        }

        [Fact]
        public async Task CalculateAmKpiAsync_Generic_NonNumericParsingDoesNotThrow_ReturnsDefaultColor()
        {
            var svc = CreateService();

            var kpi = MakeKpi("state", atributo: "SomeField", defaultColor: "#ABCDEF");

            var list = new List<object> { new { SomeField = "not-a-number" }, new { SomeField = "not-a-number" } };

            var res = await svc.CalculateAmKpiAsync(kpi, "user", list);

            Assert.NotNull(res);
            Assert.Equal("state", res.Type);
            Assert.Equal(0.0, Convert.ToDouble(res.Value));
            Assert.Equal("#ABCDEF", res.ActualColor);
        }

        [Fact]
            public async Task GetFieldValuesAsync_EmptyOrNull_ReturnsEmptyList()
            {
                var svc = CreateService();

                var res1 = await svc.GetFieldValuesAsync<DatasetReducedAMDTO>(new List<DatasetReducedAMDTO>(), "nombre");
                var res2 = await svc.GetFieldValuesAsync<DatasetReducedAMDTO>(null!, "nombre");

                Assert.Empty(res1);
                Assert.Empty(res2);
            }

            /* Tests sobre CalculateAmKpiAsync */

            [Fact]
            public async Task CalculateAmKpiAsync_CountMetric_ReturnsCountValue()
            {
                var svc = CreateService();

                var kpi = MakeKpi("count", atributo: "state", extraInfo: "OK", multiplier: 1.0);

                // create items with property "state"
                var items = new List<object>
            {
                new { state = "OK" },
                new { state = "FAIL" },
                new { state = "OK" }
            };

                var response = await svc.CalculateAmKpiAsync<object>(kpi, "user", items);

                Assert.NotNull(response);
                Assert.Equal("count", response.Type);
                Assert.Equal(2.0, Convert.ToDouble(response.Value));
                Assert.Equal(kpi.DefaultColor, response.ActualColor);
            }

            [Fact]
            public async Task CalculateAmKpiAsync_PercentageMetric_ComputesPercentageAndAppliesMultiplier()
            {
                var svc = CreateService();

                var kpi = MakeKpi("percentage", atributo: "ok", extraInfo: "S", multiplier: 0.5);

                var items = new List<object>
            {
                new { ok = "S" },
                new { ok = "N" },
                new { ok = "S" },
                new { ok = "N" }
            };

                var response = await svc.CalculateAmKpiAsync<object>(kpi, "user", items);

                Assert.NotNull(response);
                Assert.Equal("average", response.Type);
                // coincidencias = 2, items = 4 -> base % = 50.00 -> porcentajeFinal = 50 * 0.5 = 25.00
                Assert.Equal(25.0, Convert.ToDouble(response.Value));
                Assert.Equal("%", response.Unit);
            }

            [Fact]
            public async Task CalculateAmKpiAsync_StateMetric_SingleItem_ReturnsValueStringAndColorFromRanges()
            {
                var svc = CreateService();

                // ColorRanges where 0..10 -> #AAA, 11..100 -> #BBB
                var ranges = JsonSerializer.Serialize(new List<KpiAMService.ColorRange>
            {
                new() { min = 0, max = 10, color = "#AAA" },
                new() { min = 11, max = 100, color = "#BBB" }
            });

                var kpi = MakeKpi("state", atributo: "num", defaultColor: "#000000", colorRanges: ranges);

                // single item with numeric field "num" -> expects value string and color computed with numeric parsing
                var items = new List<object>
            {
                new { num = "7" }
            };

                var response = await svc.CalculateAmKpiAsync<object>(kpi, "user", items);

                Assert.NotNull(response);
                Assert.Equal("state", response.Type);
                Assert.Equal("7", response.Value);
                Assert.Equal("#AAA", response.ActualColor);
            }

            [Fact]
            public async Task CalculateAmKpiAsync_StateMetric_MultipleItems_ReturnsCountAsValue()
            {
                var svc = CreateService();

                var kpi = MakeKpi("state", atributo: "status", extraInfo: "X", multiplier: 2.0, defaultColor: "#FFF");

                var items = new List<object>
            {
                new { status = "X" },
                new { status = "Y" },
                new { status = "X" }
            };

                var response = await svc.CalculateAmKpiAsync<object>(kpi, "user", items);

                Assert.NotNull(response);
                Assert.Equal("state", response.Type);
                // coincidencias = 2 * multiplier 2 = 4
                Assert.Equal(4.0, Convert.ToDouble(response.Value));
                Assert.Equal("#FFF", response.ActualColor);
            }

            [Fact]
            public async Task CalculateAmKpiAsync_EmptyItems_ReturnsEmptyResponse()
            {
                var svc = CreateService();

                var kpi = MakeKpi("count", atributo: "state");

                var response = await svc.CalculateAmKpiAsync<object>(kpi, "user", new List<object>());

                Assert.NotNull(response);
                Assert.Null(response.Value);
                Assert.Null(response.Type);
                Assert.Equal(kpi.DefaultColor, response.ActualColor);
            }

            [Fact]
            public async Task CalculateAmKpiAsync_UnsupportedMetric_ThrowsArgumentException()
            {
                var svc = CreateService();

                var kpi = MakeKpi("unknown_metric", atributo: "x");

                var items = new List<object> { new { x = "1" } };

                await Assert.ThrowsAsync<ArgumentException>(async () => await svc.CalculateAmKpiAsync<object>(kpi, "user", items));
            }


        public async Task GetFieldValuesAsync_EM_TypeFields_ReturnsDistinctOrdered()
        {
            var svc = CreateService();

            var alertDtos = new List<DatasetReducedAlertEMDTO>
            {
                new() { Nombre = "B", Estado = "S", Fuente = "1" },
                new() { Nombre = "A", Estado = "S", Fuente = "1" },
                new() { Nombre = "A", Estado = "N", Fuente = "2" }
            };

            var values = await svc.GetFieldValuesAsync(alertDtos, "Nombre");

            Assert.Equal(new List<string> { "A", "B" }, values);
        }

        [Fact]
        public async Task GetFieldValuesAsync_UM_NewsCategoria_ReturnsDistinctOrdered()
        {
            var svc = CreateService();

            var newsDtos = new List<DatasetReducedNewsUMDTO>
            {
                new() { Titulo = "T1", Categoria = "Z" },
                new() { Titulo = "T2", Categoria = "M" },
                new() { Titulo = "T3", Categoria = "Z" }
            };

            var values = await svc.GetFieldValuesAsync(newsDtos, "Categoria");

            Assert.Equal(new List<string> { "M", "Z" }, values);
        }

        [Fact]
        public async Task GetFieldValuesAsync_FieldDoesNotExist_ReturnsEmptyList()
        {
            var svc = CreateService();

            var items = new List<DatasetReducedEventEMDTO>
            {
                new() { Nombre = "E1", Estado = "S", Origen = "X", Direccion = "D1" }
            };

            // GetFieldValuesAsync uses reflection and will return empty for missing property (GetAssetFieldValue returns null)
            var values = await svc.GetFieldValuesAsync(items, "NonExistentField");

            Assert.Empty(values);
        }
    }
}
