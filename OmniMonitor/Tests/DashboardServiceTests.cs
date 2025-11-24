using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Models;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace QA.Tests
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly ApplicationDbContext _context;
        private readonly Mock<IPasswordHasher<SharedLink>> _mockHasher = new();

        public DashboardServiceTests()
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

        private DashboardService CreateService() => new DashboardService(_context, _mockHasher.Object);

        /* Tests sobre CreateDashboardAsync */

        [Fact]
        public async Task CreateDashboardAsync_CreatesDashboardAndCards_WhenValidInput()
        {
            var svc = CreateService();
            var user = "u1";

            // prepare referenced visualizacion and kpi
            _context.Visualizaciones.Add(new Visualizacion { IdVisualizacion = 101, Nombre = "V101", Username = user, JsonDesign = "{}" });
            _context.Kpi.Add(new Kpi { Id = 201, Name = "K201", Username = user, Atributo = "news", SourceModule = "UM" });
            await _context.SaveChangesAsync();

            var layout = new DashboardLayout
            {
                Tarjetas = new List<DashboardCard>
                {
                    new DashboardCard { CardId = 101, TipoCard = 1 },
                    new DashboardCard { CardId = 201, TipoCard = 2 }
                },
                Configuracion = new LayoutConfig { Configuracion = JsonDocument.Parse("{}").RootElement }
            };
            var req = new CreateDashboardRequest { Nombre = "BoardA", Descripcion = "desc", Layout = layout };

            var res = await svc.CreateDashboardAsync(req, user);

            Assert.NotNull(res);
            Assert.Equal("BoardA", res.Nombre);
            Assert.Equal(2, res.Tarjetas.Count);
            var persisted = await _context.Dashboards.Include(d => d.GrupoVisualizaciones).FirstOrDefaultAsync(d => d.IdDashboard == res.IdDashboard);
            Assert.NotNull(persisted);
            Assert.Equal(2, persisted.GrupoVisualizaciones.Count);
        }

        [Fact]
        public async Task CreateDashboardAsync_ThrowsWhenVisualizacionMissing()
        {
            var svc = CreateService();
            var user = "u2";

            // only KPI exists, visualizacion 999 missing
            _context.Kpi.Add(new Kpi { Id = 300, Name = "K300", Username = user, SourceModule = "IM", Atributo = "sensor" });
            await _context.SaveChangesAsync();

            var layout = new DashboardLayout
            {
                Tarjetas = new List<DashboardCard>
                {
                    new DashboardCard { CardId = 999, TipoCard = 1 }, // missing visualizacion
                    new DashboardCard { CardId = 300, TipoCard = 2 }
                }
            };
            var req = new CreateDashboardRequest { Nombre = "BoardBad", Layout = layout };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateDashboardAsync(req, user));
        }

        [Fact]
        public async Task CreateDashboardAsync_ThrowsOnDuplicateName_ForSameUser()
        {
            var svc = CreateService();
            var user = "u3";

            _context.Dashboards.Add(new DashboardDto { Username = user, Nombre = "Dup", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var req = new CreateDashboardRequest { Nombre = "Dup" };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateDashboardAsync(req, user));
        }

        /* Tests for GetDashboardByIdAsync */

        [Fact]
        public async Task GetDashboardByIdAsync_ReturnsDashboardWithCardsAndVisualizacion_WhenOwner()
        {
            var svc = CreateService();
            var user = "owner1";

            var dash = new DashboardDto { Username = user, Nombre = "D1", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var viz = new Visualizacion { IdVisualizacion = 401, Nombre = "Viz401", Username = user , JsonDesign = "{}" };
            _context.Visualizaciones.Add(viz);
            await _context.SaveChangesAsync();

            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = viz.IdVisualizacion, TipoCard = 1, Orden = 1, FechaAgregado = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var got = await svc.GetDashboardByIdAsync(dash.IdDashboard, user);

            Assert.NotNull(got);
            Assert.Equal(dash.IdDashboard, got.IdDashboard);
            Assert.Single(got.Tarjetas);
            Assert.NotNull(got.Tarjetas[0].Visualizacion);
            Assert.Equal(viz.IdVisualizacion, got.Tarjetas[0].Visualizacion.IdVisualizacion);
        }

        [Fact]
        public async Task GetDashboardByIdAsync_ReturnsNull_WhenNotOwner()
        {
            var svc = CreateService();
            var owner = "owner2";
            var other = "otherUser";

            var dash = new DashboardDto { Username = owner, Nombre = "D2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var got = await svc.GetDashboardByIdAsync(dash.IdDashboard, other);

            Assert.Null(got);
        }

        [Fact]
        public async Task GetDashboardByIdAsync_IncludesKpiInfo_ForKpiCards()
        {
            var svc = CreateService();
            var user = "owner3";

            var dash = new DashboardDto { Username = user, Nombre = "D3", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            _context.Kpi.Add(new Kpi { Id = 501, Name = "K501", Unit = "u", Username = user, SourceModule = "EM", Atributo = "sensor" });
            await _context.SaveChangesAsync();

            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, KpiId = 501, TipoCard = 2, Orden = 1, FechaAgregado = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var got = await svc.GetDashboardByIdAsync(dash.IdDashboard, user);

            Assert.NotNull(got);
            Assert.Single(got.Tarjetas);
            Assert.NotNull(got.Tarjetas[0].Kpi);
            Assert.Equal(501, got.Tarjetas[0].Kpi.Id);
            Assert.Equal("K501", got.Tarjetas[0].Kpi.Name);
        }

        /* Tests para GetDashboardByIdAsyncSinToken */

        [Fact]
        public async Task GetDashboardByIdAsyncSinToken_ReturnsDashboard_WhenExistsRegardlessOfUser()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "uX", Nombre = "Public", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var got = await svc.GetDashboardByIdAsyncSinToken(dash.IdDashboard);

            Assert.NotNull(got);
            Assert.Equal(dash.IdDashboard, got.IdDashboard);
        }

        [Fact]
        public async Task GetDashboardByIdAsyncSinToken_ReturnsNull_WhenNotExists()
        {
            var svc = CreateService();

            var got = await svc.GetDashboardByIdAsyncSinToken(9999);

            Assert.Null(got);
        }

        [Fact]
        public async Task GetDashboardByIdAsyncSinToken_IncludesVisualizacionAndKpi()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "u", Nombre = "Both", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            _context.Visualizaciones.Add(new Visualizacion { IdVisualizacion = 701, Nombre = "V701", Username = "u", JsonDesign = "{}" });
            _context.Kpi.Add(new Kpi { Id = 702, Name = "K702", Username = "u", Atributo = "event", SourceModule = "EM" });
            await _context.SaveChangesAsync();

            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = 701, TipoCard = 1, Orden = 1, FechaAgregado = DateTime.UtcNow });
            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, KpiId = 702, TipoCard = 2, Orden = 2, FechaAgregado = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var got = await svc.GetDashboardByIdAsyncSinToken(dash.IdDashboard);

            Assert.NotNull(got);
            Assert.Equal(2, got.Tarjetas.Count);
            Assert.NotNull(got.Tarjetas.First(t => t.TipoCard == 1).Visualizacion);
            Assert.NotNull(got.Tarjetas.First(t => t.TipoCard == 2).Kpi);
        }

        /* Tests de GetAllDashboardsAsync */

        [Fact]
        public async Task GetAllDashboardsAsync_ReturnsAllForUser_WithoutQuery()
        {
            var svc = CreateService();
            var user = "listUser";

            var d1 = new DashboardDto { Username = user, Nombre = "One", FechaCreacion = DateTime.UtcNow.AddDays(-2), FechaModificacion = DateTime.UtcNow.AddDays(-1) };
            var d2 = new DashboardDto { Username = user, Nombre = "Two", FechaCreacion = DateTime.UtcNow.AddDays(-1), FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.AddRange(d1, d2);
            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion { GrupoVisualizacionId = d2.IdDashboard, TipoCard = 1, IdVisualizacion = null, Orden = 1, FechaAgregado = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var list = await svc.GetAllDashboardsAsync(user, null);

            Assert.Equal(2, list.Count);
            Assert.Equal(d2.IdDashboard, list[0].IdDashboard); // ordered by FechaModificacion desc
            Assert.Equal(1, list[0].CantidadTarjetas);
        }

        [Fact]
        public async Task GetAllDashboardsAsync_FiltersByQuery_CaseInsensitive()
        {
            var svc = CreateService();
            var user = "searchUser";

            _context.Dashboards.Add(new DashboardDto { Username = user, Nombre = "Panel Special", Descripcion = "Something", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            _context.Dashboards.Add(new DashboardDto { Username = user, Nombre = "Other", Descripcion = "Special stuff", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            _context.Dashboards.Add(new DashboardDto { Username = "other", Nombre = "Hidden", Descripcion = "no", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var results = await svc.GetAllDashboardsAsync(user, "special");

            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Contains("Special", (r.Nombre + r.Descripcion)!, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetAllDashboardsAsync_ReturnsEmpty_WhenNone()
        {
            var svc = CreateService();
            var res = await svc.GetAllDashboardsAsync("nouser", null);
            Assert.Empty(res);
        }

        /* Tests de ValidateCardIdsAsync */

        [Fact]
        public async Task ValidateCardIdsAsync_ReturnsTrue_WhenAllExist()
        {
            var svc = CreateService();
            _context.Visualizaciones.Add(new Visualizacion { IdVisualizacion = 801, Nombre = "V801", JsonDesign = "{}" });
            _context.Visualizaciones.Add(new Visualizacion { IdVisualizacion = 802, Nombre = "V802", JsonDesign = "{}" });
            await _context.SaveChangesAsync();

            var ok = await svc.ValidateCardIdsAsync(new List<int> { 801, 802 });
            Assert.True(ok);
        }

        [Fact]
        public async Task ValidateCardIdsAsync_ReturnsFalse_WhenSomeMissing()
        {
            var svc = CreateService();
            _context.Visualizaciones.Add(new Visualizacion { IdVisualizacion = 901, Nombre = "V901", JsonDesign = "{}" });
            await _context.SaveChangesAsync();

            var ok = await svc.ValidateCardIdsAsync(new List<int> { 901, 999 });
            Assert.False(ok);
        }

        [Fact]
        public async Task ValidateCardIdsAsync_ReturnsTrue_WhenEmptyList()
        {
            var svc = CreateService();
            var ok = await svc.ValidateCardIdsAsync(new List<int>());
            Assert.True(ok);
        }

        /* Tests sobre ValidateLayoutAsync */

        [Fact]
        public async Task ValidateLayoutAsync_ReturnsTrue_WhenLayoutIsNullOrEmpty_List()
        {
            var svc = CreateService();

            DashboardLayout? nullLayout = null;
            var resNull = await svc.ValidateLayoutAsync(nullLayout!);
            Assert.True(resNull);

            var emptyLayout = new DashboardLayout { Tarjetas = new List<DashboardCard>() };
            var resEmpty = await svc.ValidateLayoutAsync(emptyLayout);
            Assert.True(resEmpty);
        }

        [Fact]
        public async Task ValidateLayoutAsync_ReturnsTrue_ForSimpleLayout()
        {
            var svc = CreateService();

            var layout = new DashboardLayout
            {
                Tarjetas = new List<DashboardCard>
        {
            new DashboardCard { CardId = 1, TipoCard = 1 },
            new DashboardCard { CardId = 2, TipoCard = 2 }
        }
            };

            var ok = await svc.ValidateLayoutAsync(layout);
            Assert.True(ok);
        }

        [Fact]
        public async Task ValidateLayoutAsync_AllowsDuplicateCards_DueToNoOverlapCheck()
        {
            var svc = CreateService();

            var layout = new DashboardLayout
            {
                Tarjetas = new List<DashboardCard>
        {
            new DashboardCard { CardId = 10, TipoCard = 1 },
            new DashboardCard { CardId = 10, TipoCard = 1 }
        }
            };

            var ok = await svc.ValidateLayoutAsync(layout);
            Assert.True(ok);
        }

        /* Tests para DeleteDashboardAsync */

        [Fact]
        public async Task DeleteDashboardAsync_DeletesDashboardAndChildren_WhenOwner()
        {
            var svc = CreateService();

            var dash = new DashboardDto
            {
                Username = "ownerDel",
                Nombre = "Ddelete",
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            // agregar una GrupoVisualizacion asociada
            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion
            {
                GrupoVisualizacionId = dash.IdDashboard,
                TipoCard = 1,
                IdVisualizacion = null,
                KpiId = null,
                Orden = 1,
                FechaAgregado = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await svc.DeleteDashboardAsync(dash.IdDashboard, "ownerDel");
            Assert.True(result);

            var dbDash = await _context.Dashboards.FindAsync(dash.IdDashboard);
            Assert.Null(dbDash);

            var gv = await _context.GrupoVisualizaciones.FirstOrDefaultAsync(g => g.GrupoVisualizacionId == dash.IdDashboard);
            Assert.Null(gv);
        }

        [Fact]
        public async Task DeleteDashboardAsync_ReturnsFalse_WhenNotFoundOrNotOwner()
        {
            var svc = CreateService();

            // no existe
            var resMissing = await svc.DeleteDashboardAsync(9999, "any");
            Assert.False(resMissing);

            // existe pero usuario diferente
            var dash = new DashboardDto { Username = "ownerX", Nombre = "DX", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var resWrongUser = await svc.DeleteDashboardAsync(dash.IdDashboard, "otherUser");
            Assert.False(resWrongUser);
        }

        [Fact]
        public async Task DeleteDashboardAsync_RemovesOnlyAssociations_NotExternalEntities()
        {
            var svc = CreateService();

            // crear un Kpi y una Visualizacion globales
            var kpi = new Kpi { Name = "K", SourceModule = "AM", DatasetId = 1, Username = "u", Atributo = "asset" };
            _context.Kpi.Add(kpi);
            var viz = new Visualizacion { Nombre = "V", Username = "u", JsonDesign = "{}" };
            _context.Visualizaciones.Add(viz);
            await _context.SaveChangesAsync();

            // crear dashboard que los referencia
            var dash = new DashboardDto { Username = "ownerRel", Nombre = "Rel", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion
            {
                GrupoVisualizacionId = dash.IdDashboard,
                TipoCard = 1,
                IdVisualizacion = viz.IdVisualizacion,
                Orden = 1,
                FechaAgregado = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var res = await svc.DeleteDashboardAsync(dash.IdDashboard, "ownerRel");
            Assert.True(res);

            // Kpi and Visualizacion must still exist
            Assert.NotNull(await _context.Kpi.FindAsync(kpi.Id));
            Assert.NotNull(await _context.Visualizaciones.FindAsync(viz.IdVisualizacion));
        }

        /* Tests para UpdateDashboardConfigAsync */

        [Fact]
        public async Task UpdateDashboardConfigAsync_UpdatesJsonAndDate_WhenOwner()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "cfgUser", Nombre = "Cfg", JsonDiseno = "{\"a\":1}", FechaCreacion = DateTime.UtcNow.AddDays(-2), FechaModificacion = DateTime.UtcNow.AddDays(-2) };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var newJson = "{\"layout\":\"new\"}";
            var ok = await svc.UpdateDashboardConfigAsync(dash.IdDashboard, "cfgUser", newJson);
            Assert.True(ok);

            var db = await _context.Dashboards.FindAsync(dash.IdDashboard);
            Assert.Equal(newJson, db!.JsonDiseno);
            Assert.True(db.FechaModificacion > db.FechaCreacion);
        }

        [Fact]
        public async Task UpdateDashboardConfigAsync_ReturnsFalse_WhenNotFoundOrWrongUser()
        {
            var svc = CreateService();

            var resMissing = await svc.UpdateDashboardConfigAsync(9999, "u", "{\"x\":1}");
            Assert.False(resMissing);

            var dash = new DashboardDto { Username = "ownerCfg", Nombre = "Cfg2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var resWrong = await svc.UpdateDashboardConfigAsync(dash.IdDashboard, "someoneElse", "{\"x\":2}");
            Assert.False(resWrong);
        }

        [Fact]
        public async Task UpdateDashboardConfigAsync_AllowsEmptyJsonString_UpdatePersisted()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "ownerCfg2", Nombre = "Cfg3", JsonDiseno = "{\"p\":1}", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var ok = await svc.UpdateDashboardConfigAsync(dash.IdDashboard, "ownerCfg2", string.Empty);
            Assert.True(ok);

            var db = await _context.Dashboards.FindAsync(dash.IdDashboard);
            Assert.Equal(string.Empty, db!.JsonDiseno);
        }

        /* Tests para AddDashboardCardAsync */

        [Fact]
        public async Task AddDashboardCardAsync_AddsVisualizacionCard_WhenVisualizacionExists()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "addUser", Nombre = "AddD", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);

            var viz = new Visualizacion { Nombre = "Vadd", Username = "addUser", JsonDesign = "{}" };
            _context.Visualizaciones.Add(viz);

            await _context.SaveChangesAsync();

            var card = new DashboardCard { CardId = viz.IdVisualizacion, TipoCard = 1, Props = null };
            var ok = await svc.AddDashboardCardAsync(dash.IdDashboard, "addUser", "{}", card);
            Assert.True(ok);

            var gv = await _context.GrupoVisualizaciones.FirstOrDefaultAsync(g => g.GrupoVisualizacionId == dash.IdDashboard && g.IdVisualizacion == viz.IdVisualizacion);
            Assert.NotNull(gv);
        }

        [Fact]
        public async Task AddDashboardCardAsync_Throws_WhenVisualizacionMissing_ForTipo1()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "addUser2", Nombre = "AddD2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var card = new DashboardCard { CardId = 999999, TipoCard = 1 };
            await Assert.ThrowsAsync<ArgumentException>(async () => await svc.AddDashboardCardAsync(dash.IdDashboard, "addUser2", "{}", card));
        }

        [Fact]
        public async Task AddDashboardCardAsync_Throws_WhenDuplicateCardAlreadyPresent()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "addUser3", Nombre = "AddD3", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var viz = new Visualizacion { Nombre = "Vdup", Username = "addUser3", JsonDesign = "{}" };
            _context.Visualizaciones.Add(viz);
            await _context.SaveChangesAsync();

            var card = new DashboardCard { CardId = viz.IdVisualizacion, TipoCard = 1 };
            var ok1 = await svc.AddDashboardCardAsync(dash.IdDashboard, "addUser3", "{}", card);
            Assert.True(ok1);

            // second attempt must throw duplicate exception
            await Assert.ThrowsAsync<ArgumentException>(async () => await svc.AddDashboardCardAsync(dash.IdDashboard, "addUser3", "{}", card));
        }

        /* Tests para ReorderDashboardCardsAsync */

        [Fact]
        public async Task ReorderDashboardCardsAsync_UpdatesOrderAndJson_WhenOwnerAndJsonProvided()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "reordUser", Nombre = "R", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);

            var v1 = new Visualizacion { Nombre = "V1", JsonDesign = "{}" };
            var v2 = new Visualizacion { Nombre = "V2", JsonDesign = "{}" };
            _context.Visualizaciones.AddRange(v1, v2);
            await _context.SaveChangesAsync();

            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = v1.IdVisualizacion, TipoCard = 1, Orden = 1, FechaAgregado = DateTime.UtcNow });
            _context.GrupoVisualizaciones.Add(new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = v2.IdVisualizacion, TipoCard = 1, Orden = 2, FechaAgregado = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var ordered = new List<DashboardCard>
            {
                new DashboardCard { CardId = v2.IdVisualizacion, TipoCard = 1 },
                new DashboardCard { CardId = v1.IdVisualizacion, TipoCard = 1 }
            };

            var newJson = "{\"reordered\":true}";
            var ok = await svc.ReorderDashboardCardsAsync(dash.IdDashboard, "reordUser", newJson, ordered);
            Assert.True(ok);

            var gvs = await _context.GrupoVisualizaciones.Where(g => g.GrupoVisualizacionId == dash.IdDashboard).OrderBy(g => g.Orden).ToListAsync();
            Assert.Equal(v2.IdVisualizacion, gvs[0].IdVisualizacion);
            Assert.Equal(v1.IdVisualizacion, gvs[1].IdVisualizacion);

            var db = await _context.Dashboards.FindAsync(dash.IdDashboard);
            Assert.Equal(newJson, db!.JsonDiseno);
        }

        [Fact]
        public async Task ReorderDashboardCardsAsync_ReturnsFalse_WhenDashboardNotFoundOrUserMismatch()
        {
            var svc = CreateService();

            var resMissing = await svc.ReorderDashboardCardsAsync(99999, "u", "{}", new List<DashboardCard>());
            Assert.False(resMissing);

            var dash = new DashboardDto { Username = "ownerR", Nombre = "RR", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var resWrongUser = await svc.ReorderDashboardCardsAsync(dash.IdDashboard, "notOwner", "{}", new List<DashboardCard>());
            Assert.False(resWrongUser);
        }

        [Fact]
        public async Task ReorderDashboardCardsAsync_ThrowsArgumentException_WhenJsonConfigIsNull()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "ownerR2", Nombre = "RR2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentException>(async () => await svc.ReorderDashboardCardsAsync(dash.IdDashboard, "ownerR2", null!, new List<DashboardCard>()));
        }

        /* Tests sobre DeleteDashboardCardAsync */

        [Fact]
        public async Task DeleteDashboardCardAsync_RemovesCardAndUpdatesOrder_WhenExists()
        {
            var svc = CreateService();

            var dash = new DashboardDto
            {
                Username = "user1",
                Nombre = "D1",
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var gv1 = new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = 10, TipoCard = 1, Orden = 1 };
            var gv2 = new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = 11, TipoCard = 1, Orden = 2 };
            var gv3 = new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, KpiId = 5, TipoCard = 2, Orden = 3 };
            _context.GrupoVisualizaciones.AddRange(gv1, gv2, gv3);
            await _context.SaveChangesAsync();

            var result = await svc.DeleteDashboardCardAsync(dash.IdDashboard, "user1", 11, 1);
            Assert.True(result);

            var remaining = _context.GrupoVisualizaciones.Where(g => g.GrupoVisualizacionId == dash.IdDashboard).OrderBy(g => g.Orden).ToList();
            Assert.Equal(2, remaining.Count);
            Assert.Equal(1, remaining[0].Orden);
            Assert.Equal(2, remaining[1].Orden);
            Assert.DoesNotContain(remaining, g => (g.TipoCard == 1 && g.IdVisualizacion == 11));
        }

        [Fact]
        public async Task DeleteDashboardCardAsync_ReturnsFalse_WhenDashboardNotFoundOrNotOwner()
        {
            var svc = CreateService();

            var result = await svc.DeleteDashboardCardAsync(999, "someone", 1, 1);
            Assert.False(result);

            var dash = new DashboardDto { Username = "owner", Nombre = "X", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var res2 = await svc.DeleteDashboardCardAsync(dash.IdDashboard, "other", 1, 1);
            Assert.False(res2);
        }

        [Fact]
        public async Task DeleteDashboardCardAsync_ReturnsFalse_WhenCardNotFound()
        {
            var svc = CreateService();
            var dash = new DashboardDto { Username = "u", Nombre = "D", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var result = await svc.DeleteDashboardCardAsync(dash.IdDashboard, "u", 999, 1);
            Assert.False(result);
        }

        /* Tests sobre UpdateDashboardInfoAsync */

        [Fact]
        public async Task UpdateDashboardInfoAsync_UpdatesNameAndDescription_WhenValid()
        {
            var svc = CreateService();
            var dash = new DashboardDto { Username = "bob", Nombre = "Old", Descripcion = "olddesc", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var updated = await svc.UpdateDashboardInfoAsync(dash.IdDashboard, "bob", "NewName", "NewDesc");
            Assert.NotNull(updated);
            Assert.Equal("NewName", updated.Nombre);
            Assert.Equal("NewDesc", updated.Descripcion);

            var fromDb = await _context.Dashboards.FindAsync(dash.IdDashboard);
            Assert.Equal("NewName", fromDb!.Nombre);
            Assert.Equal("NewDesc", fromDb.Descripcion);
        }

        [Fact]
        public async Task UpdateDashboardInfoAsync_ReturnsNull_WhenDashboardNotFoundOrNotOwner()
        {
            var svc = CreateService();
            var res = await svc.UpdateDashboardInfoAsync(999, "u", "X", "Y");
            Assert.Null(res);

            var dash = new DashboardDto { Username = "alice", Nombre = "D", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var res2 = await svc.UpdateDashboardInfoAsync(dash.IdDashboard, "other", "X", "Y");
            Assert.Null(res2);
        }

        [Fact]
        public async Task UpdateDashboardInfoAsync_ThrowsArgumentException_WhenNewNameClashes()
        {
            var svc = CreateService();
            var dash1 = new DashboardDto { Username = "u", Nombre = "Exists", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            var dash2 = new DashboardDto { Username = "u", Nombre = "ToRename", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.AddRange(dash1, dash2);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentException>(async () => await svc.UpdateDashboardInfoAsync(dash2.IdDashboard, "u", "Exists", null));
        }

        /* Tests sobre EditDashboardCard */

        [Fact]
        public async Task EditDashboardCard_ReturnsTrue_UpdatesVisualizacionAndGrupoDatasets_WhenValid()
        {
            var svc = CreateService();

            var viz = new Visualizacion
            {
                Username = "u",
                Nombre = "V1",
                JsonDesign = "{}",
                FechaDesde = DateTime.UtcNow.AddDays(-1),
                FechaHasta = DateTime.UtcNow.AddDays(1)
            };
            _context.Visualizaciones.Add(viz);
            await _context.SaveChangesAsync();

            var dash = new DashboardDto { Username = "u", Nombre = "d", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var grupo = new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = viz.IdVisualizacion, TipoCard = 1, Orden = 1 };
            _context.GrupoVisualizaciones.Add(grupo);
            await _context.SaveChangesAsync();

            var req = new CreateVisualizacionRequest
            {
                Nombre = "NewViz",
                FechaDesde = DateTime.UtcNow.AddDays(-2),
                FechaHasta = DateTime.UtcNow.AddDays(2),
                JsonDiseñoGeneral = "{\"g\":\"x\"}",
                Datasets = new List<DatasetConfig>
                {
                    new DatasetConfig { DatasetId = 100, JsonDiseño = "{}" }
                }
            };

            var res = await svc.EditDashboardCard(dash.IdDashboard, "u", null, viz.IdVisualizacion, req);
            Assert.True(res);

            var updatedViz = await _context.Visualizaciones.Include(v => v.GrupoDatasets).FirstOrDefaultAsync(v => v.IdVisualizacion == viz.IdVisualizacion);
            Assert.Equal("NewViz", updatedViz!.Nombre);
            Assert.Equal(req.JsonDiseñoGeneral, updatedViz.JsonDesign);
            Assert.Single(updatedViz.GrupoDatasets);
            Assert.Equal(100, updatedViz.GrupoDatasets.First().DatasetId);
        }

        [Fact]
        public async Task EditDashboardCard_ReturnsFalse_WhenVisualizacionNotFoundOrNotOwner()
        {
            var svc = CreateService();
            var res = await svc.EditDashboardCard(1, "u", null, 9999, new CreateVisualizacionRequest { Nombre = "X" });
            Assert.False(res);

            var viz = new Visualizacion { Username = "alice", Nombre = "v", JsonDesign = "{}", FechaDesde = DateTime.UtcNow, FechaHasta = DateTime.UtcNow };
            _context.Visualizaciones.Add(viz);
            await _context.SaveChangesAsync();

            var dash = new DashboardDto { Username = "bob", Nombre = "d", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var group = new GrupoVisualizacion { GrupoVisualizacionId = dash.IdDashboard, IdVisualizacion = viz.IdVisualizacion, TipoCard = 1, Orden = 1 };
            _context.GrupoVisualizaciones.Add(group);
            await _context.SaveChangesAsync();

            var req = new CreateVisualizacionRequest { Nombre = "nm" };

            var res2 = await svc.EditDashboardCard(dash.IdDashboard, "bob", null, viz.IdVisualizacion, req);
            Assert.False(res2);
        }

        [Fact]
        public async Task EditDashboardCard_ReturnsFalse_WhenGrupoVisualizacionNotFound()
        {
            var svc = CreateService();

            var viz = new Visualizacion { Username = "u", Nombre = "v", JsonDesign = "{}", FechaDesde = DateTime.UtcNow, FechaHasta = DateTime.UtcNow };
            _context.Visualizaciones.Add(viz);
            await _context.SaveChangesAsync();

            var dash = new DashboardDto { Username = "u", Nombre = "d", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var req = new CreateVisualizacionRequest { Nombre = "nm" };
            var res = await svc.EditDashboardCard(dash.IdDashboard, "u", null, viz.IdVisualizacion, req);
            Assert.False(res);
        }

        /* Tests sobre SearchDashboardsByTextAsync */

        [Fact]
        public async Task SearchDashboardsByTextAsync_ReturnsEmptyList_WhenQueryEmptyOrWhitespace()
        {
            var svc = CreateService();
            var emptyRes = await svc.SearchDashboardsByTextAsync("   ");
            Assert.Empty(emptyRes);

            var emptyRes2 = await svc.SearchDashboardsByTextAsync(string.Empty);
            Assert.Empty(emptyRes2);
        }

        [Fact]
        public async Task SearchDashboardsByTextAsync_ReturnsMatches_CaseInsensitive()
        {
            var svc = CreateService();
            var d1 = new DashboardDto { Username = "u", Nombre = "My Dashboard", Descripcion = "foo bar", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            var d2 = new DashboardDto { Username = "u2", Nombre = "Another", Descripcion = "contains SEARCHterm", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.AddRange(d1, d2);
            await _context.SaveChangesAsync();

            var results = await svc.SearchDashboardsByTextAsync("searchterm");
            Assert.Single(results);
            Assert.Equal(d2.IdDashboard, results[0].IdDashboard);

            var results2 = await svc.SearchDashboardsByTextAsync("MY DASH");
            Assert.Single(results2);
            Assert.Equal(d1.IdDashboard, results2[0].IdDashboard);
        }

        [Fact]
        public async Task SearchDashboardsByTextAsync_ReturnsMultipleMatches_WhenAppropriate()
        {
            var svc = CreateService();
            var d1 = new DashboardDto { Username = "x", Nombre = "alpha one", Descripcion = "", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow.AddMinutes(-1) };
            var d2 = new DashboardDto { Username = "y", Nombre = "alpha two", Descripcion = "", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            var d3 = new DashboardDto { Username = "z", Nombre = "beta", Descripcion = "", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow.AddHours(-1) };
            _context.Dashboards.AddRange(d1, d2, d3);
            await _context.SaveChangesAsync();

            var res = await svc.SearchDashboardsByTextAsync("alpha");
            Assert.Equal(2, res.Count);
            var ids = res.Select(r => r.IdDashboard).ToHashSet();
            Assert.Contains(d1.IdDashboard, ids);
            Assert.Contains(d2.IdDashboard, ids);
        }

        /* Tests sobre CreateShareLinkAsync */

        [Fact]
        public async Task CreateShareLinkAsync_CreatesPublicLink_WhenDashboardExistsAndUserIsOwner()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "owner", Nombre = "D", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var req = new ShareRequestDto { Visibility = "public", ExpiresAt = null, Password = null };
            var res = await svc.CreateShareLinkAsync(dash.IdDashboard, req, "owner");

            Assert.NotNull(res);
            Assert.Equal(dash.IdDashboard, res.dashBoardId);
            Assert.Equal("Public", res.Visibility, ignoreCase: true);
            Assert.False(string.IsNullOrWhiteSpace(res.Slug));
        }

        [Fact]
        public async Task CreateShareLinkAsync_ThrowsKeyNotFoundException_WhenDashboardNotFoundOrNotOwner()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await svc.CreateShareLinkAsync(999, new ShareRequestDto { Visibility = "public" }, "someone");
            });

            var dash = new DashboardDto { Username = "owner", Nombre = "D2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await svc.CreateShareLinkAsync(dash.IdDashboard, new ShareRequestDto { Visibility = "public" }, "intruder");
            });
        }

        [Fact]
        public async Task CreateShareLinkAsync_CreatesPrivateLinkWithPasswordHash_WhenVisibilityPrivate()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "u", Nombre = "D", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            // mock hasher so HashPassword returns predictable value
            _mockHasher.Setup(h => h.HashPassword(It.IsAny<SharedLink>(), It.IsAny<string>())).Returns("HASHED");

            var req = new ShareRequestDto { Visibility = "private", Password = "secret", ExpiresAt = DateTime.UtcNow.AddDays(1) };
            var res = await svc.CreateShareLinkAsync(dash.IdDashboard, req, "u");

            Assert.NotNull(res);
            Assert.Equal(dash.IdDashboard, res.dashBoardId);
            Assert.Equal("Private", res.Visibility, ignoreCase: true);

            var stored = await _context.SharedLinks.FirstOrDefaultAsync(s => s.Slug == res.Slug);
            Assert.NotNull(stored);
            Assert.Equal("HASHED", stored.PasswordHash);
        }

        /* Tests sobre GetAllByDashboardAsync */

        [Fact]
        public async Task GetAllByDashboardAsync_ReturnsAllLinks_ForOwner()
        {
            var svc = CreateService();
            var dash = new DashboardDto { Username = "ownerA", Nombre = "D1", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            _context.SharedLinks.Add(new SharedLink { DashboardId = dash.IdDashboard, Slug = "s1", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public });
            _context.SharedLinks.Add(new SharedLink { DashboardId = dash.IdDashboard, Slug = "s2", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Private });
            await _context.SaveChangesAsync();

            var res = await svc.GetAllByDashboardAsync(dash.IdDashboard, "ownerA");

            Assert.Equal(2, res.Count);
            Assert.Contains(res, r => r.Slug == "s1");
            Assert.Contains(res, r => r.Slug == "s2");
        }

        [Fact]
        public async Task GetAllByDashboardAsync_ReturnsEmpty_WhenNoLinks()
        {
            var svc = CreateService();
            var dash = new DashboardDto { Username = "ownerB", Nombre = "D2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var res = await svc.GetAllByDashboardAsync(dash.IdDashboard, "ownerB");
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetAllByDashboardAsync_ReturnsEmpty_WhenNotOwner()
        {
            var svc = CreateService();
            var dash = new DashboardDto { Username = "ownerC", Nombre = "D3", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            _context.SharedLinks.Add(new SharedLink { DashboardId = dash.IdDashboard, Slug = "sx", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public });
            await _context.SaveChangesAsync();

            var res = await svc.GetAllByDashboardAsync(dash.IdDashboard, "someoneElse");
            Assert.Empty(res);
        }

        /* Tests sobre GetBySlugAsync */

        [Fact]
        public async Task GetBySlugAsync_ReturnsDto_WhenActiveAndNotExpired()
        {
            var svc = CreateService();
            var dash = new DashboardDto { Username = "ownerG", Nombre = "Dg", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "act", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public };
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            var got = await svc.GetBySlugAsync("act");
            Assert.NotNull(got);
            Assert.Equal("act", got!.Slug);
        }

        [Fact]
        public async Task GetBySlugAsync_ReturnsNull_WhenHiddenOrExpired()
        {
            var svc = CreateService();
            var dash = new DashboardDto { Username = "ownerH", Nombre = "Dh", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);

            var hidden = new SharedLink { DashboardId = dash.IdDashboard, Slug = "hid", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Hidden, Visibility = ShareVisibility.Public };
            var expired = new SharedLink { DashboardId = dash.IdDashboard, Slug = "exp", CreatedAt = DateTime.UtcNow.AddDays(-10), Status = ShareStatus.Active, Visibility = ShareVisibility.Public, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
            _context.SharedLinks.AddRange(hidden, expired);
            await _context.SaveChangesAsync();

            Assert.Null(await svc.GetBySlugAsync("hid"));
            Assert.Null(await svc.GetBySlugAsync("exp"));
        }

        [Fact]
        public async Task GetBySlugAsync_ReturnsNull_WhenSlugNotFound()
        {
            var svc = CreateService();
            Assert.Null(await svc.GetBySlugAsync("nope"));
        }

        /* Tests sobre ValidatePasswordAsync */

        [Fact]
        public async Task ValidatePasswordAsync_ReturnsTrueAndDashboardId_ForCorrectPasswordPrivateLink()
        {
            var passwordHasher = new PasswordHasher<SharedLink>();
            var svc = new DashboardService(_context, passwordHasher);

            var dash = new DashboardDto { Username = "ownerV", Nombre = "Dv", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "priv", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Private };
            link.PasswordHash = passwordHasher.HashPassword(link, "TopSecret");
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            var res = await svc.ValidatePasswordAsync("priv", "TopSecret");
            Assert.True(res.IsValid);
            Assert.Equal(dash.IdDashboard, res.DashboardId);
        }

        [Fact]
        public async Task ValidatePasswordAsync_ReturnsFalse_WhenWrongPasswordOrPublicLink()
        {
            var passwordHasher = new PasswordHasher<SharedLink>();
            var svc = new DashboardService(_context, passwordHasher);

            var dash = new DashboardDto { Username = "ownerV2", Nombre = "Dv2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            await _context.SaveChangesAsync();

            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "priv2", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Private };
            link.PasswordHash = passwordHasher.HashPassword(link, "Secret");
            _context.SharedLinks.Add(link);
            _context.SharedLinks.Add(new SharedLink { DashboardId = dash.IdDashboard, Slug = "pub", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public });
            await _context.SaveChangesAsync();

            var wrong = await svc.ValidatePasswordAsync("priv2", "Bad");
            Assert.False(wrong.IsValid);
            Assert.Null(wrong.DashboardId);

            var pub = await svc.ValidatePasswordAsync("pub", "anything");
            Assert.False(pub.IsValid);
            Assert.Null(pub.DashboardId);
        }

        [Fact]
        public async Task ValidatePasswordAsync_ReturnsFalse_WhenSlugNotFound()
        {
            var svc = CreateService();
            var res = await svc.ValidatePasswordAsync("missing", "x");
            Assert.False(res.IsValid);
            Assert.Null(res.DashboardId);
        }

        /* Tests sobre UpdateShareLinkAsync */

        [Fact]
        public async Task UpdateShareLinkAsync_UpdatesVisibilityAndPassword_WhenOwner()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "ownerU", Nombre = "Du", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "upd1", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public };
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            _mockHasher.Setup(h => h.HashPassword(It.IsAny<SharedLink>(), It.IsAny<string>())).Returns("HASH");
            var svcWithMock = CreateService();

            var req = new ShareRequestDto { Visibility = "private", Password = "P@ss", ExpiresAt = DateTime.UtcNow.AddDays(1) };
            var updated = await svcWithMock.UpdateShareLinkAsync("upd1", req, "ownerU");

            Assert.NotNull(updated);
            Assert.Equal("Private", updated!.Visibility, ignoreCase: true);

            var stored = await _context.SharedLinks.FirstOrDefaultAsync(s => s.Slug == "upd1");
            Assert.Equal("HASH", stored!.PasswordHash);
        }

        [Fact]
        public async Task UpdateShareLinkAsync_ReturnsNull_WhenLinkNotFoundOrWrongUser()
        {
            var svc = CreateService();
            Assert.Null(await svc.UpdateShareLinkAsync("nope", new ShareRequestDto { Visibility = "public" }, "u"));

            var dash = new DashboardDto { Username = "ownerX", Nombre = "Dx", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "slink", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public };
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            Assert.Null(await svc.UpdateShareLinkAsync("slink", new ShareRequestDto { Visibility = "private", Password = "p" }, "intruder"));
        }

        [Fact]
        public async Task UpdateShareLinkAsync_SetsPasswordHashToNull_WhenSwitchingToPublic()
        {
            var svc = CreateService();
            _mockHasher.Setup(h => h.HashPassword(It.IsAny<SharedLink>(), It.IsAny<string>())).Returns("HSH");

            var dash = new DashboardDto { Username = "ownerY", Nombre = "Dy", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "tog", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Private, PasswordHash = "OLD" };
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            var svcMock = CreateService();
            var updated = await svcMock.UpdateShareLinkAsync("tog", new ShareRequestDto { Visibility = "public" }, "ownerY");

            Assert.NotNull(updated);
            var stored = await _context.SharedLinks.FirstOrDefaultAsync(s => s.Slug == "tog");
            Assert.Null(stored!.PasswordHash);
        }

        /* Tests sobre DeleteShareLinkAsync */

        [Fact]
        public async Task DeleteShareLinkAsync_DeletesLink_WhenOwner()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "ownerD", Nombre = "Dd", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "del1", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public };
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            var ok = await svc.DeleteShareLinkAsync("del1", "ownerD");
            Assert.True(ok);
            Assert.Null(await _context.SharedLinks.FirstOrDefaultAsync(s => s.Slug == "del1"));
        }

        [Fact]
        public async Task DeleteShareLinkAsync_ReturnsFalse_WhenNotFoundOrWrongUser()
        {
            var svc = CreateService();
            Assert.False(await svc.DeleteShareLinkAsync("missing", "u"));

            var dash = new DashboardDto { Username = "ownerZ", Nombre = "Dz", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "del2", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public };
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            Assert.False(await svc.DeleteShareLinkAsync("del2", "someoneElse"));
        }

        [Fact]
        public async Task DeleteShareLinkAsync_AllowsMultipleDeletes_IdempotentBehavior()
        {
            var svc = CreateService();

            var dash = new DashboardDto { Username = "ownerI", Nombre = "Di", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow };
            _context.Dashboards.Add(dash);
            var link = new SharedLink { DashboardId = dash.IdDashboard, Slug = "del3", CreatedAt = DateTime.UtcNow, Status = ShareStatus.Active, Visibility = ShareVisibility.Public };
            _context.SharedLinks.Add(link);
            await _context.SaveChangesAsync();

            Assert.True(await svc.DeleteShareLinkAsync("del3", "ownerI"));
            // second delete attempt should return false because link no longer exists
            Assert.False(await svc.DeleteShareLinkAsync("del3", "ownerI"));
        }

        /* Tests sobre GetDashboardsCount */

        [Fact]
        public async Task GetDashboardsCount_ReturnsTotal_WhenNoQuery()
        {
            var svc = CreateService();

            _context.Dashboards.Add(new DashboardDto { Username = "userA", Nombre = "Dash 1", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            _context.Dashboards.Add(new DashboardDto { Username = "userA", Nombre = "Dash 2", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            _context.Dashboards.Add(new DashboardDto { Username = "userB", Nombre = "Other", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var count = await svc.GetDashboardsCount("userA", null);

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetDashboardsCount_AppliesQueryFilter_CaseInsensitive()
        {
            var svc = CreateService();

            _context.Dashboards.Add(new DashboardDto { Username = "alice", Nombre = "Sales Dashboard", Descripcion = "Monthly sales", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            _context.Dashboards.Add(new DashboardDto { Username = "alice", Nombre = "Inventory", Descripcion = "Warehouse", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            _context.Dashboards.Add(new DashboardDto { Username = "alice", Nombre = "SALES Summary", Descripcion = "Yearly", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var count = await svc.GetDashboardsCount("alice", "sales");

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetDashboardsCount_ReturnsZero_WhenNoMatches()
        {
            var svc = CreateService();

            _context.Dashboards.Add(new DashboardDto { Username = "bob", Nombre = "Finance", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var count = await svc.GetDashboardsCount("bob", "nonexistent");
            Assert.Equal(0, count);
        }

        /* Tests sobre GetAllDashboardsPaginatedAsync */

        [Fact]
        public async Task GetAllDashboardsPaginatedAsync_ReturnsFirstPage_WithPageSize()
        {
            var svc = CreateService();
            var username = "pager";

            for (int i = 1; i <= 15; i++)
            {
                _context.Dashboards.Add(new DashboardDto
                {
                    Username = username,
                    Nombre = $"Dash {i}",
                    Descripcion = $"Desc {i}",
                    FechaCreacion = DateTime.UtcNow.AddDays(-i),
                    FechaModificacion = DateTime.UtcNow.AddMinutes(i)
                });
            }
            await _context.SaveChangesAsync();

            var page1 = await svc.GetAllDashboardsPaginatedAsync(username, null, page: 1, pageSize: 5);

            Assert.Equal(5, page1.Count);
            Assert.True(page1[0].FechaModificacion >= page1[1].FechaModificacion);
            Assert.Contains("Dash", page1[0].Nombre);
        }

        [Fact]
        public async Task GetAllDashboardsPaginatedAsync_AppliesQueryFilterAndPagination()
        {
            var svc = CreateService();
            var username = "filterUser";

            _context.Dashboards.Add(new DashboardDto { Username = username, Nombre = "Sales Q1", Descripcion = "report", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow.AddMinutes(5) });
            _context.Dashboards.Add(new DashboardDto { Username = username, Nombre = "Sales Q2", Descripcion = "report", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow.AddMinutes(4) });
            _context.Dashboards.Add(new DashboardDto { Username = username, Nombre = "Engineering", Descripcion = "team dashboard", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow.AddMinutes(3) });
            _context.Dashboards.Add(new DashboardDto { Username = username, Nombre = "Sales Q3", Descripcion = "monthly", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow.AddMinutes(2) });
            _context.Dashboards.Add(new DashboardDto { Username = username, Nombre = "HR", Descripcion = "hiring", FechaCreacion = DateTime.UtcNow, FechaModificacion = DateTime.UtcNow.AddMinutes(1) });

            await _context.SaveChangesAsync();

            var page1 = await svc.GetAllDashboardsPaginatedAsync(username, "sales", page: 1, pageSize: 2);
            var page2 = await svc.GetAllDashboardsPaginatedAsync(username, "sales", page: 2, pageSize: 2);

            Assert.Equal(2, page1.Count);
            Assert.Single(page2);

            Assert.All(page1, d => Assert.Contains("sales", d.Nombre.ToLower() + (d.Descripcion ?? "").ToLower()));
            Assert.All(page2, d => Assert.Contains("sales", d.Nombre.ToLower() + (d.Descripcion ?? "").ToLower()));
        }
    }
}
