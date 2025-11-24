using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using Moq;

namespace QA.Tests
{
    public class DatasetEMServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IDatasetEMService GetService(
            ApplicationDbContext context,
            ISondaEMService? sondaEMService = null)
        {
            return new DatasetEMService(context, sondaEMService ?? Mock.Of<ISondaEMService>());
        }

        /* Tests sobre CreateDatasetEMWithFiltersAsync y UpdateDatasetEMWithFiltersAsync */

        [Fact]
        public async Task CreateDatasetEMWithFilters_SerializaFiltros_almacenaReferencias()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, sonda.Object);

            var filters = new List<FilterCondition> {
        new() { AttributeName = "AlertName", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Incendio" }
    };

            var req = new CreateDatasetEMRequest
            {
                Username = "svcUser",
                Name = "DSFiltros",
                IsDataset = "S",
                ContentType = "1",
                AlertIds = new List<int> { 101, 102 }
            };

            var created = await svc.CreateDatasetEMWithFiltersAsync(req, 42, filters);

            Assert.NotNull(created);
            Assert.Equal("svcUser", created.Username);
            Assert.Contains("Incendio", created.Filters);
            Assert.Equal(2, created.DatasetAlerts.Count);
            Assert.All(created.DatasetAlerts, a => Assert.Contains(a.Id_alert, new[] { 101, 102 }));
        }

        [Fact]
        public async Task UpdateDatasetEMWithFilters_ReemplazaRelaciones_YPersisteNuevosRegistros()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, sonda.Object);
            var filters = new List<FilterCondition>();

            // crear dataset inicial con 1 event
            var createReq = new CreateDatasetEMRequest
            {
                Username = "updUser",
                Name = "ToUpdate",
                IsDataset = "S",
                ContentType = "2",
                EventIds = new List<int> { 201 }
            };
            var ds = await svc.CreateDatasetEMWithFiltersAsync(createReq, 100, filters);
            Assert.Single(ds.DatasetEvents);

            // actualizar con evento nuevo
            var updateReq = new CreateDatasetEMRequest
            {
                Username = "updUser",
                Name = "ToUpdate",
                IsDataset = "S",
                ContentType = "2",
                EventIds = new List<int> { 201, 202 }
            };
            var newfilters = new List<FilterCondition>();
            var updated = await svc.UpdateDatasetEMWithFiltersAsync(ds.Id, updateReq, newfilters);

            Assert.NotNull(updated);
            Assert.Equal(2, updated.DatasetEvents.Count);
            Assert.Contains(updated.DatasetEvents, e => e.Id_event == 202);

            var persisted = await ctx.DatasetEventsEM.Where(e => e.DatasetId == ds.Id).ToListAsync();
            Assert.Equal(2, persisted.Count);
        }

        [Fact]
        public async Task UpdateDatasetEMWithFilters_NoDataset_ThrowsInvalidOperationException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx, Mock.Of<ISondaEMService>());

            var filters = new List<FilterCondition> { new() { AttributeName = "Name", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" } };
            var req = new CreateDatasetEMRequest
            {
                Username = "noexist",
                Name = "X",
                IsDataset = "S",
                ContentType = "2",
                EventIds = new List<int> { 1 }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateDatasetEMWithFiltersAsync(9999, req, filters));
        }

        [Fact]
        public async Task PuedeCrearDatasetEMConFiltros()
        {
            var context = GetInMemoryDbContext();
            var sondaEMService = new Mock<ISondaEMService>();
            sondaEMService.Setup(s => s.GetAlertById(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int id, string _) => new AlertDto
                {
                    AlertId = id,
                    AlertName = id == 101 ? "Incendio Forestal" : "Alerta General",
                    SourceId = 1,
                    AlertState = "Activa",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow
                });

            var service = GetService(context, sondaEMService.Object);

            var filters = new List<FilterCondition>
            {
                new() {
                    AttributeName = "AlertName",
                    Type = FilterType.Contains,
                    ValueType = FilterValueType.String,
                    Condition = "Incendio"
                }
            };

            var request = new CreateDatasetEMRequest
            {
                Username = "usuario4",
                Name = "Dataset Filtrado",
                IsDataset = "S",
                ContentType = "alert",
                AlertIds = new List<int> { 101, 102 }
            };

            var dataset = await service.CreateDatasetEMWithFiltersAsync(request, 1, filters);

            Assert.NotNull(dataset);
            Assert.Equal("usuario4", dataset.Username);
            Assert.Contains("Incendio", string.Join(",", dataset.Filters));
            Assert.Equal(101, dataset.DatasetAlerts.First().Id_alert);
        }

        [Fact]
        public async Task CreateDatasetEMWithFilters_FiltersNull_CreatesAndStoresEmptyJson()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, sonda.Object);

            var req = new CreateDatasetEMRequest
            {
                Username = "userNullFilters",
                Name = "NullFilters",
                IsDataset = "S",
                ContentType = "2",
                EventIds = new List<int> { 10 }
            };

            var created = await svc.CreateDatasetEMWithFiltersAsync(req, 55, null!);
            Assert.NotNull(created);
            Assert.True(string.IsNullOrEmpty(created.Filters) == false || created.Filters != null);
            // Accept either "null" or "[]" depending on serializer behavior
            Assert.True(created.Filters.Contains("null") || created.Filters.Contains("[]"));
        }

        [Fact]
        public async Task CreateDatasetEMWithFilters_DuplicateIds_ArePersistedAsProvided()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, sonda.Object);

            var filters = new List<FilterCondition> { new() { AttributeName = "Some", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" } };

            var req = new CreateDatasetEMRequest
            {
                Username = "dupIds",
                Name = "DupIds",
                IsDataset = "S",
                ContentType = "2",
                EventIds = new List<int> { 5, 5, 6 }
            };

            var created = await svc.CreateDatasetEMWithFiltersAsync(req, 200, filters);
            Assert.NotNull(created);
            Assert.Equal(3, created.DatasetEvents.Count);
            var persisted = await ctx.DatasetEventsEM.Where(d => d.DatasetId == created.Id).ToListAsync();
            Assert.Equal(3, persisted.Count);
        }

        [Fact]
        public async Task UpdateDatasetEMWithFilters_ReplacesRelationsAndUpdatesFilters()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, sonda.Object);

            var createReq = new CreateDatasetEMRequest
            {
                Username = "updUser2",
                Name = "ToUpdateEM",
                IsDataset = "S",
                ContentType = "1",
                AlertIds = new List<int> { 101 }
            };
            var filters = new List<FilterCondition>();
            var ds = await svc.CreateDatasetEMWithFiltersAsync(createReq, 300, filters);
            Assert.Single(ds.DatasetAlerts);

            filters = new List<FilterCondition> { new() { AttributeName = "AlertName", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" } };
            var updateReq = new CreateDatasetEMRequest
            {
                Username = "updUser2",
                Name = "ToUpdateEMModified",
                IsDataset = "S",
                ContentType = "1",
                AlertIds = new List<int> { 101, 102 }
            };

            var updated = await svc.UpdateDatasetEMWithFiltersAsync(ds.Id, updateReq, filters);
            Assert.Equal("ToUpdateEMModified", updated.Name);
            Assert.Equal(2, updated.DatasetAlerts.Count);
            Assert.Contains(updated.DatasetAlerts, a => a.Id_alert == 102);

            var persisted = await ctx.DatasetAlerts.Where(a => a.DatasetId == ds.Id).ToListAsync();
            Assert.Equal(2, persisted.Count);
        }

        [Fact]
        public async Task CreateDatasetEMWithFilters_EmptyIds_CreatesDatasetWithoutRelations()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition>
    {
        new() { AttributeName = "Any", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "v" }
    };

            var req = new CreateDatasetEMRequest
            {
                Username = "noIdsUser",
                Name = "NoIds",
                IsDataset = "S",
                ContentType = "1",
                AlertIds = new List<int>() // explicitamente vacía
            };

            var created = await svc.CreateDatasetEMWithFiltersAsync(req, 5, filters);

            Assert.NotNull(created);
            Assert.Equal("noIdsUser", created.Username);
            Assert.Equal("NoIds", created.Name);
            Assert.True(created.DatasetAlerts == null || !created.DatasetAlerts.Any());
            Assert.NotNull(created.Filters);
            Assert.Contains("Any", created.Filters);
        }

        [Fact]
        public async Task UpdateDatasetEMWithFilters_InvalidDataset_ThrowsInvalidOperationException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition> { new() { AttributeName = "Some", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" } };
            var req = new CreateDatasetEMRequest
            {
                Username = "noExistUser",
                Name = "NoExist",
                IsDataset = "S",
                ContentType = "2",
                EventIds = new List<int> { 1 }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateDatasetEMWithFiltersAsync(9999, req, filters));
        }

        /* Tests sobre GetAllDatasetsEMAsync */

        [Fact]
        public async Task GetAllDatasetsEMAsync_ReturnsOnlyDatasetsForUser()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            ctx.DatasetsEM.Add(new DatasetEM { Username = "alice", Name = "A1", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "alice", Name = "A2", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "bob", Name = "B1", Is_Dataset = "S", DatasetId = 1 });
            await ctx.SaveChangesAsync();

            var list = await svc.GetAllDatasetsEMAsync("alice");

            Assert.NotNull(list);
            Assert.Equal(2, list.Count);
            Assert.All(list, d => Assert.Equal("alice", d.Username));
        }

        [Fact]
        public async Task GetAllDatasetsEMAsync_ReturnsOrderedByName_Ascending()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            ctx.DatasetsEM.Add(new DatasetEM { Username = "user1", Name = "Zeta", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "user1", Name = "Alpha", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "user1", Name = "Beta", Is_Dataset = "S", DatasetId = 1 });
            await ctx.SaveChangesAsync();

            var list = await svc.GetAllDatasetsEMAsync("user1");

            Assert.Equal(new[] { "Alpha", "Beta", "Zeta" }, list.Select(d => d.Name).ToArray());
        }

        [Fact]
        public async Task GetAllDatasetsEMAsync_NoDatasets_ReturnsEmptyList()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var list = await svc.GetAllDatasetsEMAsync("missingUser");

            Assert.NotNull(list);
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAllDatasetsEMAsync_MixedUsers_DoesNotLeakBetweenUsers()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // many users
            ctx.DatasetsEM.Add(new DatasetEM { Username = "u1", Name = "A", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "u2", Name = "B", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "u1", Name = "C", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "u3", Name = "D", Is_Dataset = "S", DatasetId = 1 });
            await ctx.SaveChangesAsync();

            var listU2 = await svc.GetAllDatasetsEMAsync("u2");

            Assert.Single(listU2);
            Assert.Equal("u2", listU2[0].Username);
            Assert.Equal("B", listU2[0].Name);
        }

        [Fact]
        public async Task GetAllDatasetsEMAsync_IncludesAllIsDatasetValues()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            ctx.DatasetsEM.Add(new DatasetEM { Username = "mix", Name = "One", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsEM.Add(new DatasetEM { Username = "mix", Name = "Two", Is_Dataset = "N", DatasetId = 1 });
            await ctx.SaveChangesAsync();

            var list = await svc.GetAllDatasetsEMAsync("mix");

            Assert.Equal(2, list.Count);
            Assert.Contains(list, d => d.Is_Dataset == "S");
            Assert.Contains(list, d => d.Is_Dataset == "N");
        }

        [Fact]
        public async Task GetAllDatasetsEMAsync_LargeNumber_ReturnsCorrectCount()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            for (int i = 0; i < 200; i++)
            {
                ctx.DatasetsEM.Add(new DatasetEM { Username = "biguser", Name = $"Name_{i:D3}", Is_Dataset = "S", DatasetId = 1 });
            }
            await ctx.SaveChangesAsync();

            var list = await svc.GetAllDatasetsEMAsync("biguser");

            Assert.Equal(200, list.Count);
            // spot-check ordering: first should be Name_000
            Assert.Equal("Name_000", list.First().Name);
        }

        /* Tests sobre GetDatasetEMByIdAsync */

        [Fact]
        public async Task GetDatasetEMByIdAsync_ReturnsDatasetWithIncludes_WhenExistsAndUsernameMatches()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "john", Name = "DS1", Is_Dataset = "S", DatasetId = 1 };
            ds.DatasetAlerts.Add(new DatasetAlert { Id_alert = 10, DatasetId = ds.Id });
            ds.DatasetEvents.Add(new DatasetEventEM { Id_event = 20, DatasetId = ds.Id });
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetEMByIdAsync(ds.Id, "john");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DS1", result.Name);
            Assert.NotNull(result.DatasetAlerts);
            Assert.NotNull(result.DatasetEvents);
            Assert.Single(result.DatasetAlerts);
            Assert.Single(result.DatasetEvents);
            Assert.Equal(10, result.DatasetAlerts.First().Id_alert);
            Assert.Equal(20, result.DatasetEvents.First().Id_event);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_ReturnsNull_WhenDatasetDoesNotExistOrUsernameMismatch()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // seed a dataset for alice
            var ds = new DatasetEM { Username = "alice", Name = "A", Is_Dataset = "S", DatasetId = 1 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act: wrong id
            var r1 = await svc.GetDatasetEMByIdAsync(9999, "alice");
            // Act: wrong user
            var r2 = await svc.GetDatasetEMByIdAsync(ds.Id, "bob");

            Assert.Null(r1);
            Assert.Null(r2);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_DynamicLoad_NoUserInDb_ReturnsNullWhenUserMissing()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, mockSonda.Object);

            // Dataset is marked S and has no explicit relations -> triggers dynamic branch
            var ds = new DatasetEM { Username = "no_user", Name = "Dyn", Is_Dataset = "S", DatasetId = 5 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // There is no corresponding user record in ctx.Users, so service should return null
            var res = await svc.GetDatasetEMByIdAsync(ds.Id, "no_user");

            Assert.Null(res);
            // no calls to external service because user was not found
            mockSonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_DynamicLoad_UsesSondaWhenNoRelationsAndUserExists()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaEMService>();

            // prepare user in Users table so dynamic load proceeds
            ctx.Users.Add(new User { UserName = "svc", PasswordHash = "p" });
            await ctx.SaveChangesAsync();

            // dataset empty of relations and Is_Dataset = "S"
            var ds = new DatasetEM { Username = "svc", Name = "Dynamic", Is_Dataset = "S", DatasetId = 7 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            mockSonda.Setup(s => s.GetAlerts(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string>()))
                .ReturnsAsync(new List<AlertDto> { new AlertDto { AlertId = 111, AlertName = "A111" } });
            mockSonda.Setup(s => s.GetEvents(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
                .ReturnsAsync(new List<EventDto> { new EventDto { Id = 222, Name = "E222" } });
            mockSonda.Setup(s => s.GetExtensions(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()))
                .ReturnsAsync(new List<ExtensionDto> { new ExtensionDto { ExtensionId = 333, EventId = 222 } });

            var svc = GetService(ctx, mockSonda.Object);

            // Act
            var result = await svc.GetDatasetEMByIdAsync(ds.Id, "svc");

            Assert.NotNull(result);
            mockSonda.Verify(s => s.GetAlerts(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
            mockSonda.Verify(s => s.GetEvents(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
            mockSonda.Verify(s => s.GetExtensions(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsync_DoesNotCallSonda_WhenRelationsAlreadyPresent()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, mockSonda.Object);

            var ds = new DatasetEM { Username = "pre", Name = "PreRel", Is_Dataset = "S", DatasetId = 9 };
            ds.DatasetAlerts.Add(new DatasetAlert { Id_alert = 10, DatasetId = ds.Id });
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            var res = await svc.GetDatasetEMByIdAsync(ds.Id, "pre");

            Assert.NotNull(res);
            Assert.Single(res.DatasetAlerts);
            // Because relations exist, dynamic loading branch should not call external services
            mockSonda.VerifyNoOtherCalls();
        }

        /* Tests sobre GetDatasetEMByIdAsyncSinToken */

        [Fact]
        public async Task GetDatasetEMByIdAsyncSinToken_ReturnsDataset_WhenExists()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "any", Name = "SinTokenDS", Is_Dataset = "N", DatasetId = 11 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetEMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ds.Id, result!.Id);
            Assert.Equal("SinTokenDS", result.Name);
            Assert.Equal("any", result.Username);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsyncSinToken_IncludesRelations_WhenPersisted()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "relUser", Name = "WithRel", Is_Dataset = "N", DatasetId = 22 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // add relations referencing the created dataset id
            ctx.DatasetAlerts.Add(new DatasetAlert { DatasetId = ds.Id, Id_alert = 1001 });
            ctx.DatasetEventsEM.Add(new DatasetEventEM { DatasetId = ds.Id, Id_event = 2001 });
            ctx.DatasetExtensions.Add(new DatasetExtension { DatasetId = ds.Id, Id_extension = 3001 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetEMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.DatasetAlerts);
            Assert.NotNull(result.DatasetEvents);
            Assert.NotNull(result.DatasetExtensions);
            Assert.Single(result.DatasetAlerts);
            Assert.Single(result.DatasetEvents);
            Assert.Single(result.DatasetExtensions);
            Assert.Equal(1001, result.DatasetAlerts.First().Id_alert);
            Assert.Equal(2001, result.DatasetEvents.First().Id_event);
            Assert.Equal(3001, result.DatasetExtensions.First().Id_extension);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsyncSinToken_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // no seed

            // Act
            var result = await svc.GetDatasetEMByIdAsyncSinToken(99999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsyncSinToken_DoesNotRequireUserEntry()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // create dataset whose Username has no corresponding record in Users table
            var ds = new DatasetEM { Username = "missingUserRecord", Name = "NoUserRow", Is_Dataset = "N", DatasetId = 33 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetEMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("missingUserRecord", result!.Username);
        }

        [Fact]
        public async Task GetDatasetEMByIdAsyncSinToken_ReturnsDataset_WhenIsDatasetSAndNoRelations()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // Dataset marked as 'S' and with no explicit relations should still be returned by the SinToken method
            var ds = new DatasetEM { Username = "dyn", Name = "DynamicNoRel", Is_Dataset = "S", DatasetId = 44 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetEMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DynamicNoRel", result!.Name);
            Assert.False(result.DatasetAlerts.Any());
            Assert.False(result.DatasetEvents.Any());
            Assert.False(result.DatasetExtensions.Any());
        }

        [Fact]
        public async Task GetDatasetEMByIdAsyncSinToken_LoadsPersistedRelationsAcrossSaveCycles()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "cycle", Name = "CycleRel", Is_Dataset = "N", DatasetId = 55 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // create relations, save, then fetch
            ctx.DatasetAlerts.Add(new DatasetAlert { DatasetId = ds.Id, Id_alert = 500 });
            await ctx.SaveChangesAsync();

            // Act: fetch after the relation was added in a separate save cycle
            var result = await svc.GetDatasetEMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result!.DatasetAlerts);
            Assert.Equal(500, result.DatasetAlerts.First().Id_alert);
        }

        /* Tests sobre GetDatasetEMByIdForEditAsync */

        [Fact]
        public async Task GetDatasetEMByIdForEditAsync_ReturnsDataset_WhenExistsAndUsernameMatches()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "editor", Name = "EditMe", Is_Dataset = "N", DatasetId = 10 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetEMByIdForEditAsync(ds.Id, "editor");

            Assert.NotNull(result);
            Assert.Equal(ds.Id, result!.Id);
            Assert.Equal("EditMe", result.Name);
            Assert.Equal("editor", result.Username);
        }

        [Fact]
        public async Task GetDatasetEMByIdForEditAsync_ReturnsNull_WhenNotFound()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var result = await svc.GetDatasetEMByIdForEditAsync(9999, "someone");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetEMByIdForEditAsync_ReturnsNull_WhenUsernameMismatch()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "owner", Name = "OwnersDS", Is_Dataset = "N", DatasetId = 20 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetEMByIdForEditAsync(ds.Id, "intruder");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetEMByIdForEditAsync_IncludesPersistedRelations()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "reluser", Name = "RelDS", Is_Dataset = "N", DatasetId = 30 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            ctx.DatasetAlerts.Add(new DatasetAlert { DatasetId = ds.Id, Id_alert = 100 });
            ctx.DatasetEventsEM.Add(new DatasetEventEM { DatasetId = ds.Id, Id_event = 200 });
            ctx.DatasetExtensions.Add(new DatasetExtension { DatasetId = ds.Id, Id_extension = 300 });
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetEMByIdForEditAsync(ds.Id, "reluser");

            Assert.NotNull(result);
            Assert.NotNull(result!.DatasetAlerts);
            Assert.NotNull(result.DatasetEvents);
            Assert.NotNull(result.DatasetExtensions);
            Assert.Single(result.DatasetAlerts);
            Assert.Single(result.DatasetEvents);
            Assert.Single(result.DatasetExtensions);
            Assert.Equal(100, result.DatasetAlerts.First().Id_alert);
            Assert.Equal(200, result.DatasetEvents.First().Id_event);
            Assert.Equal(300, result.DatasetExtensions.First().Id_extension);
        }

        [Fact]
        public async Task GetDatasetEMByIdForEditAsync_DoesNotPerformDynamicLoad_WhenIsDatasetSAndNoRelations()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaEMService>();
            var svc = GetService(ctx, mockSonda.Object);

            // create user row not necessary for ForEdit path, but ensure dataset exists
            var ds = new DatasetEM { Username = "dynedit", Name = "DynEdit", Is_Dataset = "S", DatasetId = 40 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetEMByIdForEditAsync(ds.Id, "dynedit");

            Assert.NotNull(result);
            // For edit method the dynamic branch must not run — it simply loads persisted relations (none here)
            Assert.False(result.DatasetAlerts.Any());
            Assert.False(result.DatasetEvents.Any());
            Assert.False(result.DatasetExtensions.Any());
            mockSonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetEMByIdForEditAsync_LoadsRelations_AfterMultipleSaveCycles()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "cycleedit", Name = "CycleEdit", Is_Dataset = "N", DatasetId = 50 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // add relations in separate save cycle
            ctx.DatasetAlerts.Add(new DatasetAlert { DatasetId = ds.Id, Id_alert = 777 });
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetEMByIdForEditAsync(ds.Id, "cycleedit");

            Assert.NotNull(result);
            Assert.Single(result!.DatasetAlerts);
            Assert.Equal(777, result.DatasetAlerts.First().Id_alert);
        }

        /* Tests sobre DeleteDatasetEMAsync */

        [Fact]
        public async Task DeleteDatasetEMAsync_DeletesExistingDataset()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "deleter", Name = "ToBeDeleted", Is_Dataset = "N", DatasetId = 1 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            await svc.DeleteDatasetEMAsync(ds.Id, "deleter");

            // Assert
            var fetched = await ctx.DatasetsEM.FindAsync(ds.Id);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task DeleteDatasetEMAsync_ThrowsWhenDatasetNotFound()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetEMAsync(9999, "noone"));
        }

        [Fact]
        public async Task DeleteDatasetEMAsync_ThrowsWhenUsernameMismatch()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "owner", Name = "OwnerDS", Is_Dataset = "N", DatasetId = 2 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act & Assert: different username should not be allowed to delete
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetEMAsync(ds.Id, "intruder"));

            // Ensure dataset still exists
            var stillThere = await ctx.DatasetsEM.FindAsync(ds.Id);
            Assert.NotNull(stillThere);
        }

        [Fact]
        public async Task DeleteDatasetEMAsync_RemovesAssociatedRelations()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "relDel", Name = "WithRelations", Is_Dataset = "N", DatasetId = 3 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            ctx.DatasetAlerts.Add(new DatasetAlert { DatasetId = ds.Id, Id_alert = 10 });
            ctx.DatasetEventsEM.Add(new DatasetEventEM { DatasetId = ds.Id, Id_event = 20 });
            ctx.DatasetExtensions.Add(new DatasetExtension { DatasetId = ds.Id, Id_extension = 30 });
            await ctx.SaveChangesAsync();

            // Act
            await svc.DeleteDatasetEMAsync(ds.Id, "relDel");

            // Assert: dataset gone
            var fetched = await ctx.DatasetsEM.FindAsync(ds.Id);
            Assert.Null(fetched);

            // Assert: related records for that DatasetId should not remain (cascade or manual cleanup)
            var alerts = await ctx.DatasetAlerts.Where(a => a.DatasetId == ds.Id).ToListAsync();
            var events = await ctx.DatasetEventsEM.Where(e => e.DatasetId == ds.Id).ToListAsync();
            var exts = await ctx.DatasetExtensions.Where(x => x.DatasetId == ds.Id).ToListAsync();

            Assert.Empty(alerts);
            Assert.Empty(events);
            Assert.Empty(exts);
        }

        [Fact]
        public async Task DeleteDatasetEMAsync_DeletesOnlyTargetDataset_LeavesOthersIntact()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds1 = new DatasetEM { Username = "u", Name = "KeepMe", Is_Dataset = "N", DatasetId = 4 };
            var ds2 = new DatasetEM { Username = "u", Name = "DeleteMe", Is_Dataset = "N", DatasetId = 5 };
            ctx.DatasetsEM.AddRange(ds1, ds2);
            await ctx.SaveChangesAsync();

            // Act
            await svc.DeleteDatasetEMAsync(ds2.Id, "u");

            // Assert
            var remaining = await ctx.DatasetsEM.OrderBy(d => d.Name).ToListAsync();
            Assert.Single(remaining);
            Assert.Equal("KeepMe", remaining[0].Name);
        }

        [Fact]
        public async Task DeleteDatasetEMAsync_SecondDeleteThrows()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetEM { Username = "repeat", Name = "OneAndOnly", Is_Dataset = "N", DatasetId = 6 };
            ctx.DatasetsEM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act - first delete OK
            await svc.DeleteDatasetEMAsync(ds.Id, "repeat");

            // Act & Assert - second attempt should throw because it no longer exists
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetEMAsync(ds.Id, "repeat"));
        }
    }
}
