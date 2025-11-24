using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using OmniMonitor.Shared.Dtos;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Xunit;

namespace QA.Tests
{
    public class DatasetUMServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private DatasetUMService GetService(
            ApplicationDbContext context,
            ISondaUMService? sondaUMService = null)
        {
            return new DatasetUMService(context, sondaUMService ?? Mock.Of<ISondaUMService>());
        }

        /* Tests sobre CreateDatasetUMWithFiltersAsync y UpdateDatasetUMWithFiltersAsync */

        [Fact]
        public async Task CreateDatasetUMWithFilters_SerializaFiltros_AlmacenaReferencias()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, sonda.Object);

            var filters = new List<FilterCondition> {
            new() { AttributeName = "Title", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Alerta" }
        };

            var req = new CreateDatasetUMRequest
            {
                Username = "svcUser",
                Name = "DSFiltros",
                IsDataset = "S",
                ContentType = "2", // News
                NewsIds = new List<int> { 101, 102 }
            };

            var created = await svc.CreateDatasetUMWithFiltersAsync(req, 42, filters);

            Assert.NotNull(created);
            Assert.Equal("svcUser", created.Username);
            Assert.Contains("Alerta", created.Filters);
            Assert.Equal(2, created.DatasetNews.Count);
            Assert.All(created.DatasetNews, n => Assert.Contains(n.Id_news, new[] { 101, 102 }));
        }

        [Fact]
        public async Task UpdateDatasetUMWithFilters_ReemplazaRelaciones_YPersisteNuevosRegistros()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, sonda.Object);
            var filters = new List<FilterCondition>();

            // crear dataset inicial con 1 event
            var createReq = new CreateDatasetUMRequest
            {
                Username = "updUser",
                Name = "ToUpdate",
                IsDataset = "S",
                ContentType = "1", // Event
                EventIds = new List<int> { 201 }
            };
            var ds = await svc.CreateDatasetUMWithFiltersAsync(createReq, 100, filters);
            Assert.Single(ds.DatasetEvents);

            // actualizar con evento nuevo
            var updateReq = new CreateDatasetUMRequest
            {
                Username = "updUser",
                Name = "ToUpdate",
                IsDataset = "S",
                ContentType = "1",
                EventIds = new List<int> { 201, 202 }
            };
            var newfilters = new List<FilterCondition>();
            var updated = await svc.UpdateDatasetUMWithFiltersAsync(ds.Id, updateReq, newfilters);

            Assert.NotNull(updated);
            Assert.Equal(2, updated.DatasetEvents.Count);
            Assert.Contains(updated.DatasetEvents, e => e.Id_event == 202);

            var persisted = await ctx.DatasetEvents.Where(e => e.DatasetId == ds.Id).ToListAsync();
            Assert.Equal(2, persisted.Count);
        }

        [Fact]
        public async Task UpdateDatasetUMWithFilters_NoDataset_ThrowsInvalidOperationException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx, Mock.Of<ISondaUMService>());

            var filters = new List<FilterCondition> { new() { AttributeName = "Name", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" } };
            var req = new CreateDatasetUMRequest
            {
                Username = "noexist",
                Name = "X",
                IsDataset = "S",
                ContentType = "1",
                EventIds = new List<int> { 1 }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateDatasetUMWithFiltersAsync(9999, req, filters));
        }

        [Fact]
        public async Task PuedeCrearDatasetUMConFiltros()
        {
            var context = GetInMemoryDbContext();
            var sondaUMService = new Mock<ISondaUMService>();
            sondaUMService.Setup(s => s.GetEventById(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int id, string _) => new Event
                {
                    Id = id,
                    Name = id == 101 ? "Evento Importante" : "Evento General",
                    TypeId = 1,
                    ApprovalState = "Aprobado"
                });

            var service = GetService(context, sondaUMService.Object);

            var filters = new List<FilterCondition>
        {
            new() {
                AttributeName = "Name",
                Type = FilterType.Contains,
                ValueType = FilterValueType.String,
                Condition = "Importante"
            }
        };

            var request = new CreateDatasetUMRequest
            {
                Username = "usuario4",
                Name = "Dataset Filtrado",
                IsDataset = "S",
                ContentType = "1",
                EventIds = new List<int> { 101, 102 }
            };

            var dataset = await service.CreateDatasetUMWithFiltersAsync(request, 1, filters);

            Assert.NotNull(dataset);
            Assert.Equal("usuario4", dataset.Username);
            Assert.Contains("Importante", string.Join(",", dataset.Filters));
            Assert.Equal(101, dataset.DatasetEvents.First().Id_event);
        }

        [Fact]
        public async Task CreateDatasetUMWithFilters_FiltersNull_CreatesAndStoresEmptyJson()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, sonda.Object);

            var req = new CreateDatasetUMRequest
            {
                Username = "userNullFilters",
                Name = "NullFilters",
                IsDataset = "S",
                ContentType = "2",
                NewsIds = new List<int> { 10 }
            };

            var created = await svc.CreateDatasetUMWithFiltersAsync(req, 55, null!);
            Assert.NotNull(created);
            Assert.True(string.IsNullOrEmpty(created.Filters) == false || created.Filters != null);
            Assert.True(created.Filters.Contains("null") || created.Filters.Contains("[]"));
        }

        [Fact]
        public async Task CreateDatasetUMWithFilters_DuplicateIds_ArePersistedAsProvided()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, sonda.Object);

            var filters = new List<FilterCondition> { new() { AttributeName = "Some", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" } };

            var req = new CreateDatasetUMRequest
            {
                Username = "dupIds",
                Name = "DupIds",
                IsDataset = "S",
                ContentType = "2",
                NewsIds = new List<int> { 5, 5, 6 }
            };

            var created = await svc.CreateDatasetUMWithFiltersAsync(req, 200, filters);
            Assert.NotNull(created);
            Assert.Equal(3, created.DatasetNews.Count);
            var persisted = await ctx.DatasetNews.Where(d => d.DatasetId == created.Id).ToListAsync();
            Assert.Equal(3, persisted.Count);
        }

        [Fact]
        public async Task UpdateDatasetUMWithFilters_ReplacesRelationsAndUpdatesFilters()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, sonda.Object);

            var createReq = new CreateDatasetUMRequest
            {
                Username = "updUser2",
                Name = "ToUpdateUM",
                IsDataset = "S",
                ContentType = "2",
                NewsIds = new List<int> { 101 }
            };
            var ds = await svc.CreateDatasetUMWithFiltersAsync(createReq, 300, new List<FilterCondition>());
            Assert.Single(ds.DatasetNews);

            var filters = new List<FilterCondition> { new() { AttributeName = "Title", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" } };
            var updateReq = new CreateDatasetUMRequest
            {
                Username = "updUser2",
                Name = "ToUpdateUMModified",
                IsDataset = "S",
                ContentType = "2",
                NewsIds = new List<int> { 101, 102 }
            };

            var updated = await svc.UpdateDatasetUMWithFiltersAsync(ds.Id, updateReq, filters);
            Assert.Equal("ToUpdateUMModified", updated.Name);
            Assert.Equal(2, updated.DatasetNews.Count);
            Assert.Contains(updated.DatasetNews, n => n.Id_news == 102);

            var persisted = await ctx.DatasetNews.Where(n => n.DatasetId == ds.Id).ToListAsync();
            Assert.Equal(2, persisted.Count);
        }

        [Fact]
        public async Task CreateDatasetUMWithFilters_EmptyIds_CreatesDatasetWithoutRelations()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition>
        {
            new() { AttributeName = "Any", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "v" }
        };

            var req = new CreateDatasetUMRequest
            {
                Username = "noIdsUser",
                Name = "NoIds",
                IsDataset = "S",
                ContentType = "1",
                EventIds = new List<int>() // explícitamente vacía
            };

            var created = await svc.CreateDatasetUMWithFiltersAsync(req, 5, filters);

            Assert.NotNull(created);
            Assert.Equal("noIdsUser", created.Username);
            Assert.Equal("NoIds", created.Name);
            Assert.True(created.DatasetEvents == null || !created.DatasetEvents.Any());
            Assert.NotNull(created.Filters);
            Assert.Contains("Any", created.Filters);
        }

        [Fact]
        public async Task UpdateDatasetUMWithFilters_InvalidDataset_ThrowsInvalidOperationException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition> { new() { AttributeName = "Some", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" } };
            var req = new CreateDatasetUMRequest
            {
                Username = "noExistUser",
                Name = "NoExist",
                IsDataset = "S",
                ContentType = "2",
                NewsIds = new List<int> { 1 }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateDatasetUMWithFiltersAsync(9999, req, filters));
        }

        /* Tests sobre GetAllDatasetUMAsync */

        [Fact]
        public async Task GetAllDatasetsUMAsync_ReturnsOnlyDatasetsForUser()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            ctx.DatasetsUM.Add(new DatasetUM { Username = "alice", Name = "A1", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsUM.Add(new DatasetUM { Username = "alice", Name = "A2", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsUM.Add(new DatasetUM { Username = "bob", Name = "B1", Is_Dataset = "S", DatasetId = 1 });
            await ctx.SaveChangesAsync();

            // Act
            var list = await svc.GetAllDatasetsUMAsync("alice");

            // Assert
            Assert.NotNull(list);
            Assert.Equal(2, list.Count);
            Assert.All(list, d => Assert.Equal("alice", d.Username));
        }

        [Fact]
        public async Task GetAllDatasetsUMAsync_NoDatasets_ReturnsEmptyList()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // Act
            var list = await svc.GetAllDatasetsUMAsync("missingUser");

            // Assert
            Assert.NotNull(list);
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetAllDatasetsUMAsync_MixedUsers_DoesNotLeakBetweenUsers()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            ctx.DatasetsUM.Add(new DatasetUM { Username = "u1", Name = "A", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsUM.Add(new DatasetUM { Username = "u2", Name = "B", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsUM.Add(new DatasetUM { Username = "u1", Name = "C", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsUM.Add(new DatasetUM { Username = "u3", Name = "D", Is_Dataset = "S", DatasetId = 1 });
            await ctx.SaveChangesAsync();

            // Act
            var listU2 = await svc.GetAllDatasetsUMAsync("u2");

            // Assert
            Assert.Single(listU2);
            Assert.Equal("u2", listU2[0].Username);
            Assert.Equal("B", listU2[0].Name);
        }

        [Fact]
        public async Task GetAllDatasetsUMAsync_IncludesDifferentIsDatasetValues()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            ctx.DatasetsUM.Add(new DatasetUM { Username = "mix", Name = "One", Is_Dataset = "S", DatasetId = 1 });
            ctx.DatasetsUM.Add(new DatasetUM { Username = "mix", Name = "Two", Is_Dataset = "N", DatasetId = 1 });
            await ctx.SaveChangesAsync();

            // Act
            var list = await svc.GetAllDatasetsUMAsync("mix");

            // Assert
            Assert.Equal(2, list.Count);
            Assert.Contains(list, d => d.Is_Dataset == "S");
            Assert.Contains(list, d => d.Is_Dataset == "N");
        }

        [Fact]
        public async Task GetAllDatasetsUMAsync_LargeNumber_ReturnsCorrectCount()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            for (int i = 0; i < 150; i++)
            {
                ctx.DatasetsUM.Add(new DatasetUM { Username = "biguser", Name = $"Name_{i:D3}", Is_Dataset = "S", DatasetId = 1 });
            }
            await ctx.SaveChangesAsync();

            // Act
            var list = await svc.GetAllDatasetsUMAsync("biguser");

            // Assert
            Assert.Equal(150, list.Count);
            // spot-check one deterministic item exists
            Assert.Contains(list, d => d.Name == "Name_000");
        }

        [Fact]
        public async Task GetAllDatasetsUMAsync_DoesNotRequireSondaOrUserRecords()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            // do not add any Users row and do not provide ISondaUMService
            var svc = GetService(ctx);

            ctx.DatasetsUM.Add(new DatasetUM { Username = "nouserrow", Name = "Standalone", Is_Dataset = "N", DatasetId = 7 });
            await ctx.SaveChangesAsync();

            // Act
            var list = await svc.GetAllDatasetsUMAsync("nouserrow");

            // Assert
            Assert.Single(list);
            Assert.Equal("Standalone", list[0].Name);
            Assert.Equal("nouserrow", list[0].Username);
        }

        /* Tests sobre GetDatasetUMByIdAsync */

        [Fact]
        public async Task GetDatasetUMByIdAsync_ReturnsDatasetWithIncludes_WhenExistsAndUsernameMatches()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "john", Name = "DS1", Is_Dataset = "S", DatasetId = 1 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Add relations pointing to persisted dataset id
            ctx.DatasetEvents.Add(new DatasetEvent { DatasetId = ds.Id, Id_event = 10 });
            ctx.DatasetNews.Add(new DatasetNews { DatasetId = ds.Id, Id_news = 20 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetUMByIdAsync(ds.Id, "john");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DS1", result.Name);
            Assert.NotNull(result.DatasetEvents);
            Assert.NotNull(result.DatasetNews);
            Assert.Single(result.DatasetEvents);
            Assert.Single(result.DatasetNews);
            Assert.Equal(10, result.DatasetEvents.First().Id_event);
            Assert.Equal(20, result.DatasetNews.First().Id_news);
        }

        [Fact]
        public async Task GetDatasetUMByIdAsync_ReturnsNull_WhenNotFoundOrUsernameMismatch()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // seed a dataset for alice
            var ds = new DatasetUM { Username = "alice", Name = "A", Is_Dataset = "S", DatasetId = 1 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act: wrong id
            var r1 = await svc.GetDatasetUMByIdAsync(9999, "alice");
            // Act: wrong user
            var r2 = await svc.GetDatasetUMByIdAsync(ds.Id, "bob");

            Assert.Null(r1);
            Assert.Null(r2);
        }

        [Fact]
        public async Task GetDatasetUMByIdAsync_DynamicLoad_ReturnsNullWhenUserMissing_AndDoesNotCallSonda()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, mockSonda.Object);

            // Dataset is marked S and has no explicit relations -> triggers dynamic branch
            var ds = new DatasetUM { Username = "no_user", Name = "Dyn", Is_Dataset = "S", DatasetId = 5 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // There is no corresponding user record in ctx.Users, so service should return null
            var res = await svc.GetDatasetUMByIdAsync(ds.Id, "no_user");

            Assert.Null(res);
            mockSonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetUMByIdAsync_DynamicLoad_UsesSondaAndAddsRelations_WhenUserExistsAndNoRelations()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaUMService>();

            // Create user row so dynamic load proceeds
            ctx.Users.Add(new User { UserName = "svc"});
            await ctx.SaveChangesAsync();

            // dataset empty of relations and Is_Dataset = "S"
            var ds = new DatasetUM { Username = "svc", Name = "Dynamic", Is_Dataset = "S", DatasetId = 7 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            var events = new List<Event>
            {
                new Event { Id = 100, Name = "E100", Location = new Location() },
                new Event { Id = 101, Name = "E101", Location = new Location() }
            };
            var news = new List<News>
            {
                new News { Id = 200, Title = "N200", Zone = new Zone { Id = 1 } },
                new News { Id = 201, Title = "N201", Zone = new Zone { Id = 1 } }
            };

            mockSonda.Setup(s => s.GetAllEvents("svc")).ReturnsAsync(events);
            mockSonda.Setup(s => s.GetAllNews("svc", It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()))
                     .ReturnsAsync(news);

            var svc = GetService(ctx, mockSonda.Object);

            // Act
            var result = await svc.GetDatasetUMByIdAsync(ds.Id, "svc");

            Assert.NotNull(result);
            Assert.True(result.DatasetEvents.Any() || result.DatasetNews.Any());
            mockSonda.Verify(s => s.GetAllEvents("svc"), Times.AtLeastOnce);
            mockSonda.Verify(s => s.GetAllNews("svc", It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetDatasetUMByIdAsync_DoesNotCallSonda_WhenRelationsAlreadyPresent()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, mockSonda.Object);

            var ds = new DatasetUM { Username = "pre", Name = "PreRel", Is_Dataset = "S", DatasetId = 9 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            ctx.DatasetEvents.Add(new DatasetEvent { DatasetId = ds.Id, Id_event = 10 });
            await ctx.SaveChangesAsync();

            var res = await svc.GetDatasetUMByIdAsync(ds.Id, "pre");

            Assert.NotNull(res);
            Assert.Single(res.DatasetEvents);
            mockSonda.VerifyNoOtherCalls();
        }

        /* Tests sobre GetDatasetUMByIdAsyncSinToken */

        [Fact]
        public async Task GetDatasetUMByIdAsyncSinToken_ReturnsDataset_WhenExistsAndRelationsPersisted()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "owner", Name = "SinTokenDS", Is_Dataset = "N", DatasetId = 11 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // add relations referencing the created dataset id
            ctx.DatasetEvents.Add(new DatasetEvent { DatasetId = ds.Id, Id_event = 1001 });
            ctx.DatasetNews.Add(new DatasetNews { DatasetId = ds.Id, Id_news = 2001 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetUMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ds.Id, result!.Id);
            Assert.Equal("SinTokenDS", result.Name);
            Assert.Single(result.DatasetEvents);
            Assert.Single(result.DatasetNews);
            Assert.Equal(1001, result.DatasetEvents.First().Id_event);
            Assert.Equal(2001, result.DatasetNews.First().Id_news);
        }

        [Fact]
        public async Task GetDatasetUMByIdAsyncSinToken_ReturnsNull_WhenDatasetNotFound()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // Act
            var result = await svc.GetDatasetUMByIdAsyncSinToken(99999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetUMByIdAsyncSinToken_ReturnsNull_WhenDynamicLoadNeededButUserMissing()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, mockSonda.Object);

            // Dataset marked as 'S' and with no persisted relations -> triggers dynamic branch inside GetDatasetUMByIdAsync
            var ds = new DatasetUM { Username = "no_user_row", Name = "DynNoUser", Is_Dataset = "S", DatasetId = 33 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var res = await svc.GetDatasetUMByIdAsyncSinToken(ds.Id);

            // Assert: because there is no Users row for that username, GetDatasetUMByIdAsync returns null and so does SinToken
            Assert.Null(res);
            // sonda should not be called because user lookup fails before any sonda calls
            mockSonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetUMByIdAsyncSinToken_PerformsDynamicLoad_WhenUserExistsAndNoRelations()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaUMService>();

            ctx.Users.Add(new User { UserName = "svc"});
            await ctx.SaveChangesAsync();

            // dataset empty of relations and Is_Dataset = "S"
            var ds = new DatasetUM { Username = "svc", Name = "DynamicSinToken", Is_Dataset = "S", DatasetId = 77 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Prepare sonda returns: events and news lists
            var events = new List<Event>
            {
                new Event { Id = 100, Name = "E100", Location = new Location() },
                new Event { Id = 101, Name = "E101", Location = new Location() }
            };
            var news = new List<News>
            {
                new News { Id = 200, Title = "N200", Zone = new Zone { Id = 1 } },
                new News { Id = 201, Title = "N201", Zone = new Zone { Id = 1 } }
            };

            mockSonda.Setup(s => s.GetAllEvents("svc")).ReturnsAsync(events);
            mockSonda.Setup(s => s.GetAllNews("svc", It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()))
                     .ReturnsAsync(news);

            var svc = GetService(ctx, mockSonda.Object);

            // Act
            var result = await svc.GetDatasetUMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            // dynamic load code appends DatasetEvents/DatasetNews entries when lists are returned
            Assert.True(result.DatasetEvents.Any() || result.DatasetNews.Any());
            mockSonda.Verify(s => s.GetAllEvents("svc"), Times.AtLeastOnce);
            mockSonda.Verify(s => s.GetAllNews("svc", It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetDatasetUMByIdAsyncSinToken_WorksWithoutPassingUsername()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "nouserparam", Name = "NoUserParam", Is_Dataset = "N", DatasetId = 88 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act: note we call SinToken which internally looks up the owner
            var result = await svc.GetDatasetUMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("nouserparam", result!.Username);
            Assert.Equal("NoUserParam", result.Name);
        }

        [Fact]
        public async Task GetDatasetUMByIdAsyncSinToken_LoadsRelationsAddedInSeparateSaveCycle()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "cycle", Name = "CycleSinToken", Is_Dataset = "N", DatasetId = 55 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // add relation after initial save
            ctx.DatasetEvents.Add(new DatasetEvent { DatasetId = ds.Id, Id_event = 555 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetUMByIdAsyncSinToken(ds.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result!.DatasetEvents);
            Assert.Equal(555, result.DatasetEvents.First().Id_event);
        }

        /* Tests sobre GetDatasetUMByIdForEditAsync */

        [Fact]
        public async Task GetDatasetUMByIdForEditAsync_ReturnsDataset_WhenExistsAndUsernameMatches()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "editor", Name = "EditMe", Is_Dataset = "N", DatasetId = 10 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetUMByIdForEditAsync(ds.Id, "editor");

            Assert.NotNull(result);
            Assert.Equal(ds.Id, result!.Id);
            Assert.Equal("EditMe", result.Name);
            Assert.Equal("editor", result.Username);
        }

        [Fact]
        public async Task GetDatasetUMByIdForEditAsync_ReturnsNull_WhenNotFound()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var result = await svc.GetDatasetUMByIdForEditAsync(9999, "someone");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetUMByIdForEditAsync_ReturnsNull_WhenUsernameMismatch()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "owner", Name = "OwnersDS", Is_Dataset = "N", DatasetId = 20 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetUMByIdForEditAsync(ds.Id, "intruder");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetUMByIdForEditAsync_IncludesPersistedRelations()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "reluser", Name = "RelDS", Is_Dataset = "N", DatasetId = 30 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            ctx.DatasetEvents.Add(new DatasetEvent { DatasetId = ds.Id, Id_event = 100 });
            ctx.DatasetNews.Add(new DatasetNews { DatasetId = ds.Id, Id_news = 200 });
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetUMByIdForEditAsync(ds.Id, "reluser");

            Assert.NotNull(result);
            Assert.NotNull(result!.DatasetEvents);
            Assert.NotNull(result.DatasetNews);
            Assert.Single(result.DatasetEvents);
            Assert.Single(result.DatasetNews);
            Assert.Equal(100, result.DatasetEvents.First().Id_event);
            Assert.Equal(200, result.DatasetNews.First().Id_news);
        }

        [Fact]
        public async Task GetDatasetUMByIdForEditAsync_DoesNotPerformDynamicLoad_WhenIsDatasetSAndNoRelations()
        {
            var ctx = GetInMemoryDbContext();
            var mockSonda = new Mock<ISondaUMService>();
            var svc = GetService(ctx, mockSonda.Object);

            var ds = new DatasetUM { Username = "dynedit", Name = "DynEdit", Is_Dataset = "S", DatasetId = 40 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetUMByIdForEditAsync(ds.Id, "dynedit");

            Assert.NotNull(result);
            Assert.False(result.DatasetEvents.Any());
            Assert.False(result.DatasetNews.Any());
            // For edit path dynamic loading isn't executed; no calls to ISondaUMService expected
            mockSonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetUMByIdForEditAsync_LoadsRelations_AfterMultipleSaveCycles()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "cycleedit", Name = "CycleEdit", Is_Dataset = "N", DatasetId = 50 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // add relations in separate save cycle
            ctx.DatasetEvents.Add(new DatasetEvent { DatasetId = ds.Id, Id_event = 777 });
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetUMByIdForEditAsync(ds.Id, "cycleedit");

            Assert.NotNull(result);
            Assert.Single(result!.DatasetEvents);
            Assert.Equal(777, result.DatasetEvents.First().Id_event);
        }

        /* Tests sobre DeleteDatasetUMAsync */

        [Fact]
        public async Task DeleteDatasetUMAsync_DeletesExistingDataset()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "deleter", Name = "ToBeDeleted", Is_Dataset = "N", DatasetId = 1 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            await svc.DeleteDatasetUMAsync(ds.Id, "deleter");

            // Assert
            var fetched = await ctx.DatasetsUM.FindAsync(ds.Id);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task DeleteDatasetUMAsync_ThrowsWhenDatasetNotFound()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetUMAsync(9999, "noone"));
        }

        [Fact]
        public async Task DeleteDatasetUMAsync_ThrowsWhenUsernameMismatch()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "owner", Name = "OwnerDS", Is_Dataset = "N", DatasetId = 2 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act & Assert: different username should not be allowed to delete
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetUMAsync(ds.Id, "intruder"));

            // Ensure dataset still exists
            var stillThere = await ctx.DatasetsUM.FindAsync(ds.Id);
            Assert.NotNull(stillThere);
        }

        [Fact]
        public async Task DeleteDatasetUMAsync_RemovesAssociatedRelations()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "relDel", Name = "WithRelations", Is_Dataset = "N", DatasetId = 3 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            ctx.DatasetEvents.Add(new DatasetEvent { DatasetId = ds.Id, Id_event = 10 });
            ctx.DatasetNews.Add(new DatasetNews { DatasetId = ds.Id, Id_news = 20 });
            await ctx.SaveChangesAsync();

            // Act
            await svc.DeleteDatasetUMAsync(ds.Id, "relDel");

            // Assert: dataset gone
            var fetched = await ctx.DatasetsUM.FindAsync(ds.Id);
            Assert.Null(fetched);

            // Related records for that DatasetId should not remain
            var events = await ctx.DatasetEvents.Where(e => e.DatasetId == ds.Id).ToListAsync();
            var news = await ctx.DatasetNews.Where(n => n.DatasetId == ds.Id).ToListAsync();

            Assert.Empty(events);
            Assert.Empty(news);
        }

        [Fact]
        public async Task DeleteDatasetUMAsync_DeletesOnlyTargetDataset_LeavesOthersIntact()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds1 = new DatasetUM { Username = "u", Name = "KeepMe", Is_Dataset = "N", DatasetId = 4 };
            var ds2 = new DatasetUM { Username = "u", Name = "DeleteMe", Is_Dataset = "N", DatasetId = 5 };
            ctx.DatasetsUM.AddRange(ds1, ds2);
            await ctx.SaveChangesAsync();

            // Act
            await svc.DeleteDatasetUMAsync(ds2.Id, "u");

            // Assert
            var remaining = await ctx.DatasetsUM.OrderBy(d => d.Name).ToListAsync();
            Assert.Single(remaining);
            Assert.Equal("KeepMe", remaining[0].Name);
        }

        [Fact]
        public async Task DeleteDatasetUMAsync_SecondDeleteThrows()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetUM { Username = "repeat", Name = "OneAndOnly", Is_Dataset = "N", DatasetId = 6 };
            ctx.DatasetsUM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act - first delete OK
            await svc.DeleteDatasetUMAsync(ds.Id, "repeat");

            // Act & Assert - second attempt should throw because it no longer exists
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetUMAsync(ds.Id, "repeat"));
        }
    }
}
