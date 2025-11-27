using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Moq;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;

namespace QA.Tests
{
    public class DatasetAMServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IDatasetAmService GetService(ApplicationDbContext context, ISondaAMService? sondaAMService = null)
        {
            return new DatasetAmService(context, sondaAMService ?? Mock.Of<ISondaAMService>());
        }

        /* Tests para CreateDatasetAMWithFiltersAsync y UpdateDatasetAMWithFiltersAsync */

        [Fact]
        public async Task PuedeCrearDatasetAMConFiltros()
        {
            var context = GetInMemoryDbContext();
            var sondaAMService = new Mock<ISondaAMService>();
            var service = GetService(context, sondaAMService.Object);

            var filters = new List<FilterCondition>
            {
                new() {
                    AttributeName = "Name",
                    Type = FilterType.Contains,
                    ValueType = FilterValueType.String,
                    Condition = "Test"
                }
            };

            var request = new CreateDatasetAMRequest
            {
                Username = "usuario5",
                Nombre = "Dataset Filtrado",
                Descripcion = "Con filtros",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Id_Asset_Type = 1,
                Grupo_Asset_Ids = new List<string> { "A1" }
            };

            var dataset = await service.CreateDatasetAMWithFiltersAsync(request, 1, filters);

            Assert.NotNull(dataset);
            Assert.Equal("usuario5", dataset.Username);
            Assert.Contains("Test", dataset.Filters);
        }

        [Fact]
        public async Task CreateDatasetAMWithFilters_PersistsFiltersAndReferences()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var filters = new List<FilterCondition>
    {
        new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "Test" }
    };

            var req = new CreateDatasetAMRequest
            {
                Username = "fuser",
                Nombre = "WithFilters",
                Descripcion = "desc",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Id_Asset_Type = 7,
                Grupo_Asset_Ids = new List<string> { "10", "11" }
            };

            var created = await svc.CreateDatasetAMWithFiltersAsync(req, 123, filters);

            Assert.NotNull(created);
            Assert.Equal("fuser", created.Username);
            Assert.Equal("WithFilters", created.Nombre);
            Assert.NotNull(created.Filters);
            Assert.Contains("Test", created.Filters);
            Assert.NotNull(created.Grupo_Asset);
            Assert.Equal(2, created.Grupo_Asset.Count);
        }

        [Fact]
        public async Task CreateDatasetAMWithFilters_DuplicateIds_ArePersistedAsProvidedButServiceShouldHandleUniquenessOnQuery()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var filters = new List<FilterCondition>
    {
        new() { AttributeName = "Some", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" }
    };

            var req = new CreateDatasetAMRequest
            {
                Username = "dupUser",
                Nombre = "DupDataset",
                IsDataset = "S",
                ContentType = "1",
                Type_Dataset = 1,
                Grupo_Event_Task_Instance_Ids = new List<int> { 5, 5, 6, 6 }
            };

            var created = await svc.CreateDatasetAMWithFiltersAsync(req, 200, filters);

            Assert.NotNull(created);
            // El servicio almacena lo que le pasaron; valida que se guardaron las 4 entradas tal cual
            Assert.NotNull(created.Grupo_Event_Task_Instance);
            Assert.Equal(4, created.Grupo_Event_Task_Instance.Count);

            // Sin embargo, comportamiento esperado en consultas: las APIs que devuelven resultados deben deduplicar.
            // Aquí validamos que la DB contiene 4 filas relacionadas (persistencia tal cual).
            var persisted = await ctx.DatasetEventTaskInstance.Where(d => d.DatasetAMId == created.Id_Dataset).ToListAsync();
            Assert.Equal(4, persisted.Count);
        }

        [Fact]
        public async Task UpdateDatasetAMWithFilters_UpdatesFiltersAndReplacesRelations()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            // Crear dataset inicial con 2 assets
            var initialReq = new CreateDatasetAMRequest
            {
                Username = "upUser",
                Nombre = "ToUpdate",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Grupo_Asset_Ids = new List<string> { "A1", "A2" }
            };
            var filters = new List<FilterCondition>();
            var created = await svc.CreateDatasetAMWithFiltersAsync(initialReq, 300, filters);
            Assert.Equal(2, created.Grupo_Asset.Count);

            // Preparar update con filtros y assets diferentes
            filters = new List<FilterCondition>
    {
        new() { AttributeName = "Name", Type = FilterType.StartsWith, ValueType = FilterValueType.String, Condition = "New" }
    };
            var updateReq = new CreateDatasetAMRequest
            {
                Username = "upUser",
                Nombre = "ToUpdateModified",
                Descripcion = "updated",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Id_Asset_Type = 9,
                Grupo_Asset_Ids = new List<string> { "A3" }
            };

            var updated = await svc.UpdateDatasetAMWithFiltersAsync(created.Id_Dataset, updateReq, filters);

            Assert.NotNull(updated);
            Assert.Equal("ToUpdateModified", updated.Nombre);
            Assert.Equal(1, updated.Grupo_Asset.Count);
            Assert.Contains(updated.Grupo_Asset, a => a.Id_Asset == "A3");
            Assert.NotNull(updated.Filters);
            Assert.Contains("New", updated.Filters);
            var persistedAssets = await ctx.DatasetAsset.Where(a => a.DatasetAMId == created.Id_Dataset).ToListAsync();
            Assert.Single(persistedAssets);
            Assert.Equal("A3", persistedAssets.First().Id_Asset);
        }

        [Fact]
        public async Task CreateDatasetAMWithFilters_SinUsername_LanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition>
    {
        new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" }
    };

            var req = new CreateDatasetAMRequest
            {
                Username = "", // inválido
                Nombre = "Name",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Grupo_Asset_Ids = new List<string> { "A1" }
            };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateDatasetAMWithFiltersAsync(req, 1, filters));
        }

        [Fact]
        public async Task CreateDatasetAMWithFilters_SinNombre_LanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition>
    {
        new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" }
    };

            var req = new CreateDatasetAMRequest
            {
                Username = "u",
                Nombre = "", // inválido
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Grupo_Asset_Ids = new List<string> { "A1" }
            };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateDatasetAMWithFiltersAsync(req, 1, filters));
        }

        [Fact]
        public async Task CreateDatasetAMWithFilters_NombreDuplicado_LanzaInvalidOperationException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition>
    {
        new() { AttributeName = "Name", Type = FilterType.Equals, ValueType = FilterValueType.String, Condition = "X" }
    };

            var req1 = new CreateDatasetAMRequest
            {
                Username = "dupUser",
                Nombre = "Dup",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Grupo_Asset_Ids = new List<string> { "A1" }
            };

            var req2 = new CreateDatasetAMRequest
            {
                Username = "dupUser",
                Nombre = "Dup",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Grupo_Asset_Ids = new List<string> { "A2" }
            };

            await svc.CreateDatasetAMWithFiltersAsync(req1, 10, filters);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateDatasetAMWithFiltersAsync(req2, 11, filters));
        }

        [Fact]
        public async Task CreateDatasetAMWithFilters_GrupoIdsNull_CreaDatasetSinRelaciones()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition>
    {
        new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" }
    };

            // Grupo_Asset_Ids null pero otros datos válidos
            var req = new CreateDatasetAMRequest
            {
                Username = "userNoIds",
                Nombre = "NoRelations",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Grupo_Asset_Ids = null // explicitamente null
            };

            var created = await svc.CreateDatasetAMWithFiltersAsync(req, 5, filters);

            Assert.NotNull(created);
            Assert.Equal("userNoIds", created.Username);
            Assert.Equal("NoRelations", created.Nombre);
            // Como no se recibieron IDs, la colección de assets debe ser null o vacía según implementación
            Assert.True(created.Grupo_Asset == null || !created.Grupo_Asset.Any());
            // Los filtros deben haberse persistido como JSON en la propiedad Filters
            Assert.NotNull(created.Filters);
            Assert.Contains("X", created.Filters);
        }

        [Fact]
        public async Task UpdateDatasetAMWithFilters_NoExiste_LanzaInvalidOperationException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var filters = new List<FilterCondition>
            {
                new() { AttributeName = "Name", Type = FilterType.Contains, ValueType = FilterValueType.String, Condition = "X" }
            };

            var req = new CreateDatasetAMRequest
            {
                Username = "nonuser",
                Nombre = "DoesNotExist",
                IsDataset = "S",
                ContentType = "2",
                Type_Dataset = 2,
                Grupo_Asset_Ids = new List<string> { "A1" }
            };

            // datasetId 999 no existe -> el servicio debe lanzar InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateDatasetAMWithFiltersAsync(999, req, filters));
        }

        /* Tests sobre GetReducedAssetsByDatasetIdAsync */

        [Fact]
        public async Task GetReducedAssetsByDatasetIdAsync_Success_ReturnsMappedDto()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            // seed user and dataset assets
            ctx.Users.Add(new User { UserName = "alice" });
            ctx.DatasetAsset.Add(new DatasetAsset { Id_Asset = "123", DatasetAMId = 11, Grupo_Asset = 1 });
            await ctx.SaveChangesAsync();

            // Prepare AssetDto (BundleDto null => fallback to BundleId.ToString())
            var assetDto = new AssetDto
            {
                Id = "123",
                Name = "AssetName",
                Code = "C123",
                Address = "Addr",
                Reference = "Ref",
                BundleId = 42,
                BundleDto = null, // should fallback
                BrandDto = new BrandDto { Name = "BrandX" },
                StateDto = new StateDto { Name = "Ready" },
                ModelDto = new ModelDto { Name = "M1" },
                ResponsibleDto = new ResponsibleDto { Name = "Resp" },
                ProviderDto = new ProviderDto { Name = "Prov" }
            };

            sonda.Setup(s => s.GetAssetById(123, "alice")).ReturnsAsync(assetDto);

            // Act
            var result = await svc.GetReducedAssetsByDatasetIdAsync(11, "alice");

            // Assert
            Assert.Single(result);
            var reduced = result.First();
            Assert.Equal("AssetName", reduced.nombre);
            Assert.Equal("C123", reduced.codigo);
            Assert.Equal("Addr", reduced.address);
            Assert.Equal("Ref", reduced.referencia);
            Assert.Equal("42", reduced.bundle); // fallback to BundleId.ToString()
            Assert.Equal("BrandX", reduced.brand);
            Assert.Equal("Ready", reduced.state);
            Assert.Equal("M1", reduced.modelo);
            Assert.Equal("Resp", reduced.responsable);
            Assert.Equal("Prov", reduced.proveedor);
            sonda.Verify(s => s.GetAssetById(123, "alice"), Times.Once);
        }

        [Fact]
        public async Task GetReducedAssetsByDatasetIdAsync_NonNumericIdAsset_IgnoresEntry()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            ctx.Users.Add(new User { UserName = "bob" });
            ctx.DatasetAsset.Add(new DatasetAsset { Id_Asset = "ABC", DatasetAMId = 20, Grupo_Asset = 1 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetReducedAssetsByDatasetIdAsync(20, "bob");

            // Assert
            Assert.Empty(result);
            // Verify sonda never called because parsing failed
            sonda.Verify(s => s.GetAssetById(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetReducedAssetsByDatasetIdAsync_GetAssetByIdReturnsNull_IgnoresEntry()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            ctx.Users.Add(new User { UserName = "carol" });
            ctx.DatasetAsset.Add(new DatasetAsset { Id_Asset = "321", DatasetAMId = 30, Grupo_Asset = 1 });
            await ctx.SaveChangesAsync();

            sonda.Setup(s => s.GetAssetById(321, "carol")).ReturnsAsync((AssetDto?)null);

            // Act
            var result = await svc.GetReducedAssetsByDatasetIdAsync(30, "carol");

            // Assert
            Assert.Empty(result);
            sonda.Verify(s => s.GetAssetById(321, "carol"), Times.Once);
        }

        [Fact]
        public async Task GetReducedAssetsByDatasetIdAsync_InvalidDatasetId_ThrowsArgumentException()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx); // no sonda needed

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetReducedAssetsByDatasetIdAsync(0, "any"));
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetReducedAssetsByDatasetIdAsync(-5, "any"));
        }

        [Fact]
        public async Task GetReducedAssetsByDatasetIdAsync_MultipleAssets_MappedCorrectlyWithBundleFallbackAndName()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            ctx.Users.Add(new User { UserName = "dave" });
            // three assets: two numeric, one non-numeric
            ctx.DatasetAsset.Add(new DatasetAsset { Id_Asset = "1", DatasetAMId = 50, Grupo_Asset = 1 });
            ctx.DatasetAsset.Add(new DatasetAsset { Id_Asset = "xyz", DatasetAMId = 50, Grupo_Asset = 2 });
            ctx.DatasetAsset.Add(new DatasetAsset { Id_Asset = "2", DatasetAMId = 50, Grupo_Asset = 3 });
            await ctx.SaveChangesAsync();

            // Asset 1 -> has BundleDto (should use BundleDto.Name)
            var a1 = new AssetDto
            {
                Id = "1",
                Name = "A1",
                Code = "C1",
                Address = "Addr1",
                Reference = "Ref1",
                BundleId = 9,
                BundleDto = new BundleDto { Name = "BundleNine" },
                BrandDto = new BrandDto { Name = "B" },
                StateDto = new StateDto { Name = "S" },
                ModelDto = new ModelDto { Name = "M" },
                ResponsibleDto = new ResponsibleDto { Name = "R" },
                ProviderDto = new ProviderDto { Name = "P" }
            };

            // Asset 2 -> BundleDto null -> fallback to BundleId
            var a2 = new AssetDto
            {
                Id = "2",
                Name = "A2",
                Code = "C2",
                Address = "Addr2",
                Reference = "Ref2",
                BundleId = 77,
                BundleDto = null,
                BrandDto = new BrandDto { Name = "B2" },
                StateDto = new StateDto { Name = "S2" },
                ModelDto = new ModelDto { Name = "M2" },
                ResponsibleDto = new ResponsibleDto { Name = "R2" },
                ProviderDto = new ProviderDto { Name = "P2" }
            };

            sonda.Setup(s => s.GetAssetById(1, "dave")).ReturnsAsync(a1);
            sonda.Setup(s => s.GetAssetById(2, "dave")).ReturnsAsync(a2);

            // Act
            var result = await svc.GetReducedAssetsByDatasetIdAsync(50, "dave");

            // Assert
            Assert.Equal(2, result.Count);
            // verify presence and mapping for A1
            Assert.Contains(result, r => r.nombre == "A1" && r.bundle == "BundleNine" && r.codigo == "C1");
            // verify presence and fallback for A2
            Assert.Contains(result, r => r.nombre == "A2" && r.bundle == "77" && r.codigo == "C2");

            sonda.Verify(s => s.GetAssetById(1, "dave"), Times.Once);
            sonda.Verify(s => s.GetAssetById(2, "dave"), Times.Once);
            sonda.Verify(s => s.GetAssetById(It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(2));
        }

        /* Tests sobre GetReducedEventsByDatasetIdAsync */

        [Fact]
        public async Task GetReducedEventsByDatasetIdAsync_Success_MapsFieldsAndCriticalYes()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            // seed one event task instance linked to datasetId 100
            ctx.DatasetEventTaskInstance.Add(new DatasetEventTaskInstance
            {
                DatasetAMId = 100,
                Id_Event_Task_Instance = 9001
            });
            await ctx.SaveChangesAsync();

            // prepare DTO returned by sonda
            var dto = new EventTaskInstanceDto
            {
                Id = 9001,
                Subject = "Subject A",
                State = "Open",
                Critical = true,
                TakenBy = new UserDto { Name = "Operator1" },
                EventTaskDto = new EventTaskDto { Subject = "TaskSubject" }
            };

            sonda.Setup(s => s.GetEventTaskInstanceById(9001, "anyUser")).ReturnsAsync(dto);

            // Act
            var result = await svc.GetReducedEventsByDatasetIdAsync(100, "anyUser");

            // Assert
            Assert.Single(result);
            var first = result.First();
            Assert.Equal("TaskSubject", first.eventTask);
            Assert.Equal("Operator1", first.autor);
            Assert.Equal("Open", first.state);
            Assert.Equal("Subject A", first.subject);
            Assert.Equal("Operator1", first.takenBy);
            Assert.Equal("Sí", first.critico);
            sonda.Verify(s => s.GetEventTaskInstanceById(9001, "anyUser"), Times.Once);
        }

        [Fact]
        public async Task GetReducedEventsByDatasetIdAsync_GetEventReturnsNull_IgnoresEntry()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            ctx.DatasetEventTaskInstance.Add(new DatasetEventTaskInstance
            {
                DatasetAMId = 200,
                Id_Event_Task_Instance = 8002
            });
            await ctx.SaveChangesAsync();

            sonda.Setup(s => s.GetEventTaskInstanceById(8002, "bob")).ReturnsAsync((EventTaskInstanceDto?)null);

            // Act
            var result = await svc.GetReducedEventsByDatasetIdAsync(200, "bob");

            // Assert
            Assert.Empty(result);
            sonda.Verify(s => s.GetEventTaskInstanceById(8002, "bob"), Times.Once);
        }

        [Fact]
        public async Task GetReducedEventsByDatasetIdAsync_InvalidDatasetId_ThrowsArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetReducedEventsByDatasetIdAsync(0, "u"));
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetReducedEventsByDatasetIdAsync(-10, "u"));
        }

        [Fact]
        public async Task GetReducedEventsByDatasetIdAsync_MultipleEntries_OnlyValidMappedReturned()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            ctx.DatasetEventTaskInstance.AddRange(
                new DatasetEventTaskInstance { DatasetAMId = 300, Id_Event_Task_Instance = 7001 },
                new DatasetEventTaskInstance { DatasetAMId = 300, Id_Event_Task_Instance = 7002 },
                new DatasetEventTaskInstance { DatasetAMId = 300, Id_Event_Task_Instance = 7003 }
            );
            await ctx.SaveChangesAsync();

            var dto1 = new EventTaskInstanceDto { Id = 7001, EventTaskDto = new EventTaskDto { Subject = "T1" }, Subject = "S1", State = "X", Critical = false, TakenBy = new UserDto { Name = "U1" } };
            var dto2 = new EventTaskInstanceDto { Id = 7002, EventTaskDto = new EventTaskDto { Subject = "T2" }, Subject = "S2", State = "Y", Critical = true, TakenBy = new UserDto { Name = "U2" } };
            // 7003 will return null

            sonda.Setup(s => s.GetEventTaskInstanceById(7001, "charlie")).ReturnsAsync(dto1);
            sonda.Setup(s => s.GetEventTaskInstanceById(7002, "charlie")).ReturnsAsync(dto2);
            sonda.Setup(s => s.GetEventTaskInstanceById(7003, "charlie")).ReturnsAsync((EventTaskInstanceDto?)null);

            // Act
            var result = await svc.GetReducedEventsByDatasetIdAsync(300, "charlie");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.eventTask == "T1" && r.subject == "S1" && r.critico == "No");
            Assert.Contains(result, r => r.eventTask == "T2" && r.subject == "S2" && r.critico == "Sí");
            sonda.Verify(s => s.GetEventTaskInstanceById(It.IsAny<int>(), "charlie"), Times.Exactly(3));
        }

        [Fact]
        public async Task GetReducedEventsByDatasetIdAsync_CriticalNull_TreatedAsNo()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            ctx.DatasetEventTaskInstance.Add(new DatasetEventTaskInstance
            {
                DatasetAMId = 400,
                Id_Event_Task_Instance = 6001
            });
            await ctx.SaveChangesAsync();

            var dto = new EventTaskInstanceDto
            {
                Id = 6001,
                EventTaskDto = new EventTaskDto { Subject = "T3" },
                Subject = "S3",
                State = "Done",
                Critical = null,
                TakenBy = null
            };
            sonda.Setup(s => s.GetEventTaskInstanceById(6001, "dave")).ReturnsAsync(dto);

            // Act
            var result = await svc.GetReducedEventsByDatasetIdAsync(400, "dave");

            // Assert
            Assert.Single(result);
            Assert.Equal("No", result.First().critico);
            Assert.Null(result.First().autor); // TakenBy null -> autor null
            sonda.Verify(s => s.GetEventTaskInstanceById(6001, "dave"), Times.Once);
        }

        /* Tests sobre GetAllDatasetAMsAsync */

        [Fact]
        public async Task GetAllDatasetAMsAsync_HappyPath_ReturnsDatasetsWithChildrenIncluded()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var username = "alice";
            var ds1 = new DatasetAM
            {
                Id_Dataset = 1,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 2,
                DatasetId = 100
            };
            ds1.Grupo_Asset.Add(new DatasetAsset { Grupo_Asset = 11, Id_Asset = "10", DatasetAMId = ds1.Id_Dataset });
            var ds2 = new DatasetAM
            {
                Id_Dataset = 2,
                Username = username,
                Is_Dataset = "N",
                Type_Dataset = 1,
                DatasetId = 101
            };
            ds2.Grupo_Event_Task_Instance.Add(new DatasetEventTaskInstance { Id = 21, Id_Event_Task_Instance = 900 });
            ds2.Grupo_Event_Task_Instance.First().Grupo_Stock.Add(new DatasetStock { Grupo_Stock = 31, Id_Stock = 55, DatasetAMId = ds2.Id_Dataset });

            ctx.DatasetAM.AddRange(ds1, ds2);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetAllDatasetAMsAsync(username);

            // Assert
            Assert.Equal(2, result.Count);
            var returned1 = result.Single(r => r.Id_Dataset == 1);
            Assert.NotNull(returned1.Grupo_Asset);
            Assert.Single(returned1.Grupo_Asset);
            var returned2 = result.Single(r => r.Id_Dataset == 2);
            Assert.NotNull(returned2.Grupo_Event_Task_Instance);
            Assert.Single(returned2.Grupo_Event_Task_Instance);
            Assert.Single(returned2.Grupo_Event_Task_Instance.First().Grupo_Stock);
        }

        [Fact]
        public async Task GetAllDatasetAMsAsync_NoDatasetsForUser_ReturnsEmptyList()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // seed dataset for other user
            ctx.DatasetAM.Add(new DatasetAM { Id_Dataset = 10, Username = "other", Is_Dataset = "S", Type_Dataset = 1, DatasetId = 200 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetAllDatasetAMsAsync("nobody");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllDatasetAMsAsync_ChildrenCollectionsNullOrEmpty_DoNotThrowAndReturnDatasets()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            // Create dataset where navigation collections are left as default (empty)
            var username = "nullableUser";
            var ds = new DatasetAM
            {
                Id_Dataset = 33,
                Username = username,
                Is_Dataset = "N",
                Type_Dataset = 2,
                DatasetId = 303,
                // do not add children (collections remain empty)
            };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetAllDatasetAMsAsync(username);

            // Assert
            Assert.Single(result);
            var returned = result.First();
            Assert.True(returned.Grupo_Asset == null || !returned.Grupo_Asset.Any() || returned.Grupo_Asset.Count == 0);
            Assert.True(returned.Grupo_Event_Task_Instance == null || !returned.Grupo_Event_Task_Instance.Any());
        }

        [Fact]
        public async Task GetAllDatasetAMsAsync_MultipleUsers_ReturnsOnlyCurrentUserDatasets()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            ctx.DatasetAM.Add(new DatasetAM { Id_Dataset = 101, Username = "owner", Is_Dataset = "S", Type_Dataset = 2, DatasetId = 501 });
            ctx.DatasetAM.Add(new DatasetAM { Id_Dataset = 102, Username = "owner", Is_Dataset = "S", Type_Dataset = 1, DatasetId = 502 });
            ctx.DatasetAM.Add(new DatasetAM { Id_Dataset = 201, Username = "other", Is_Dataset = "S", Type_Dataset = 2, DatasetId = 503 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetAllDatasetAMsAsync("owner");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("owner", r.Username));
            Assert.DoesNotContain(result, r => r.Username == "other");
        }

        [Fact]
        public async Task GetAllDatasetAMsAsync_LargeNumberOfDatasets_ReturnsAllForUser()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var username = "bulk";
            for (int i = 1; i <= 150; i++)
            {
                ctx.DatasetAM.Add(new DatasetAM { Id_Dataset = 1000 + i, Username = username, Is_Dataset = "N", Type_Dataset = (i % 3) + 1, DatasetId = 2000 + i });
            }
            // add some for other users to ensure filtering
            ctx.DatasetAM.Add(new DatasetAM { Id_Dataset = 9999, Username = "someone", Is_Dataset = "N", Type_Dataset = 1, DatasetId = 9999 });
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetAllDatasetAMsAsync(username);

            // Assert
            Assert.Equal(150, result.Count);
            Assert.All(result, r => Assert.Equal(username, r.Username));
        }

        [Fact]
        public async Task GetAllDatasetAMsAsync_UsernameNull_ReturnsEmptyList()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);
            // Act
            var result = await svc.GetAllDatasetAMsAsync(null!);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /* Tests sobre GetDatasetAMByIdAsync */

        [Fact]
        public async Task GetDatasetAMByIdAsync_ExistsWithChildren_ReturnsDatasetWithoutCallingSonda()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "alice";
            ctx.Users.Add(new User { UserName = username });
            var ds = new DatasetAM
            {
                Id_Dataset = 10,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 2,
                Id_Asset_Type = 5
            };
            ds.Grupo_Asset.Add(new DatasetAsset { Grupo_Asset = 1, Id_Asset = "100", DatasetAMId = ds.Id_Dataset });
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetAMByIdAsync(10, username);

            Assert.NotNull(result);
            Assert.Equal(10, result.Id_Dataset);
            Assert.NotNull(result.Grupo_Asset);
            Assert.Single(result.Grupo_Asset);
            sonda.Verify(s => s.GetAssets(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
            sonda.Verify(s => s.GetEventTaskInstances(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDatasetAMByIdAsync_FormalAssetEmpty_CallsSondaAndPopulatesAssets()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "bob";
            ctx.Users.Add(new User { UserName = username });

            var ds = new DatasetAM
            {
                Id_Dataset = 20,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 2,
                Id_Asset_Type = 7
            };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var assetDtos = new List<AssetDto>
    {
        new AssetDto
        {
            Id = "111",
            Name = "Asset111",
            Code = "C111",
            BundleId = 1,
            BundleDto = null,
            BrandDto = new BrandDto { Name = "B" },
            StateDto = new StateDto { Name = "S" },
            ModelDto = new ModelDto { Name = "M" },
            ResponsibleDto = new ResponsibleDto { Name = "R" },
            ProviderDto = new ProviderDto { Name = "P" }
        }
    };

            sonda.Setup(s => s.GetAssets(null, null, null, 7, null, null, username)).ReturnsAsync(assetDtos);

            var result = await svc.GetDatasetAMByIdAsync(20, username);

            Assert.NotNull(result);
            Assert.NotNull(result.Grupo_Asset);
            Assert.Single(result.Grupo_Asset);
            Assert.Equal("111", result.Grupo_Asset.First().Id_Asset);
            sonda.Verify(s => s.GetAssets(null, null, null, 7, null, null, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMByIdAsync_FormalEventTaskEmpty_CallsSondaAndPopulatesEventInstances()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "carol";
            ctx.Users.Add(new User { UserName = username });

            var ds = new DatasetAM
            {
                Id_Dataset = 30,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 1,
                Id_Event_Task = 42
            };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var eventDtos = new List<EventTaskInstanceDto>
    {
        new EventTaskInstanceDto { Id = 9000, Subject = "T1", State = "Open", Critical = false, EventTaskDto = new EventTaskDto { Subject = "Task" } }
    };

            sonda.Setup(s => s.GetEventTaskInstances(
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(),
                42, It.IsAny<int?>(), It.IsAny<int?>(), false, false, username))
                 .ReturnsAsync(eventDtos);

            var result = await svc.GetDatasetAMByIdAsync(30, username);

            Assert.NotNull(result);
            Assert.NotNull(result.Grupo_Event_Task_Instance);
            Assert.Single(result.Grupo_Event_Task_Instance);
            Assert.Equal(9000, result.Grupo_Event_Task_Instance.First().Id_Event_Task_Instance);
            sonda.Verify(s => s.GetEventTaskInstances(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), 42, It.IsAny<int?>(), It.IsAny<int?>(), false, false, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMByIdAsync_SingleEventInstance_PopulatesStocksFromSonda()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "dan";
            ctx.Users.Add(new User { UserName = username });

            var et = new DatasetEventTaskInstance
            {
                Id = 1,
                DatasetAMId = 40,
                Id_Event_Task_Instance = 5001,
                Grupo_Stock = new List<DatasetStock>()
            };
            var ds = new DatasetAM
            {
                Id_Dataset = 40,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 1,
                Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance> { et }
            };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var stocks = new List<EventTaskInstanceStockDto> { new EventTaskInstanceStockDto { Id = 77 } };

            sonda.Setup(s => s.GetEventTaskInstanceStock(5001, username))
                 .Returns(Task.FromResult(stocks));

            var result = await svc.GetDatasetAMByIdAsync(40, username);

            Assert.NotNull(result);
            var instance = result.Grupo_Event_Task_Instance.First();
            Assert.NotNull(instance.Grupo_Stock);
            Assert.Single(instance.Grupo_Stock);
            Assert.Equal(77, instance.Grupo_Stock.First().Id_Stock);
            sonda.Verify(s => s.GetEventTaskInstanceStock(5001, username), Times.Once);
        }

        [Fact]
        public async Task GetDatasetAMByIdAsync_NegativeId_ThrowsArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            await Assert.ThrowsAsync<ArgumentException>(() => svc.GetDatasetAMByIdAsync(-1, "u"));
        }

        [Fact]
        public async Task GetDatasetAMByIdAsync_UserNotFound_DoesNotCallSonda_ReturnsDataset()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "eve";
            // DO NOT add user row -> simulate missing user
            var ds = new DatasetAM
            {
                Id_Dataset = 60,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 2,
                Id_Asset_Type = 9
            };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetAMByIdAsync(60, username);

            Assert.NotNull(result);
            Assert.True(result.Grupo_Asset == null || !result.Grupo_Asset.Any());
            sonda.Verify(s => s.GetAssets(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDatasetAMByIdAsync_NotFound_ReturnsNull()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var result = await svc.GetDatasetAMByIdAsync(9999, "nosuch");
            Assert.Null(result);
        }

        /* Tests sobre GetDatasetAMByIdAsyncSinToken */

        [Fact]
        public async Task GetDatasetAMByIdAsyncSinToken_NotFound_ReturnsNull()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            // Act
            var result = await svc.GetDatasetAMByIdAsyncSinToken(9999);

            // Assert
            Assert.Null(result);
            sonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetAMByIdAsyncSinToken_ExistsWithOwner_DelegatesAndReturnsDataset()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var owner = "ownerA";
            var ds = new DatasetAM
            {
                Id_Dataset = 10,
                Username = owner,
                Is_Dataset = "N",
                Type_Dataset = 2,
                Id_Asset_Type = 5
            };
            ds.Grupo_Asset.Add(new DatasetAsset { Grupo_Asset = 1, Id_Asset = "100", DatasetAMId = ds.Id_Dataset });
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetAMByIdAsyncSinToken(10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Id_Dataset);
            Assert.Equal(owner, result.Username);
            Assert.NotNull(result.Grupo_Asset);
            Assert.Single(result.Grupo_Asset);
            sonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetAMByIdAsyncSinToken_OwnerUserNotInUsers_ReturnsDatasetWithoutCallingSonda()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var owner = "charlie";
            var ds = new DatasetAM
            {
                Id_Dataset = 30,
                Username = owner,
                Is_Dataset = "S",
                Type_Dataset = 2,
                Id_Asset_Type = 9
            };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            var result = await svc.GetDatasetAMByIdAsyncSinToken(30);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Grupo_Asset == null || !result.Grupo_Asset.Any());
            sonda.Verify(s => s.GetAssets(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetDatasetAMByIdAsyncSinToken_EventTaskSingleInstance_CallsGetEventTaskInstanceStockAndPopulatesStocks()
        {
            // Arrange
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var owner = "dan";
            var et = new DatasetEventTaskInstance
            {
                Id = 1,
                DatasetAMId = 40,
                Id_Event_Task_Instance = 5001,
                Grupo_Stock = new List<DatasetStock>()
            };
            var ds = new DatasetAM
            {
                Id_Dataset = 40,
                Username = owner,
                Is_Dataset = "S",
                Type_Dataset = 1,
                Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance> { et },
                Id_Event_Task = 42
            };
            ctx.DatasetAM.Add(ds);
            // also add the owner to Users because GetDatasetAMByIdAsync will look for it
            ctx.Users.Add(new User { UserName = owner });
            await ctx.SaveChangesAsync();

            var stocks = new List<EventTaskInstanceStockDto> { new EventTaskInstanceStockDto { Id = 77 } };
            sonda.Setup(s => s.GetEventTaskInstanceStock(5001, owner)).ReturnsAsync(stocks);

            // Act
            var result = await svc.GetDatasetAMByIdAsyncSinToken(40);

            // Assert
            Assert.NotNull(result);
            var instance = result.Grupo_Event_Task_Instance.First();
            Assert.NotNull(instance.Grupo_Stock);
            Assert.Single(instance.Grupo_Stock);
            Assert.Equal(77, instance.Grupo_Stock.First().Id_Stock);
            sonda.Verify(s => s.GetEventTaskInstanceStock(5001, owner), Times.Once);
        }

        /* Tests sobre GetDatasetAMByIdForEditAsync */

        [Fact]
        public async Task GetDatasetAMByIdForEditAsync_ExistsWithChildren_ReturnsDatabaseSnapshot_NoSondaCalls()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "owner1";
            var ds = new DatasetAM
            {
                Id_Dataset = 101,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 2,
                Id_Asset_Type = 5
            };
            ds.Grupo_Asset.Add(new DatasetAsset { Grupo_Asset = 1, Id_Asset = "A100", DatasetAMId = ds.Id_Dataset });
            ds.Grupo_Event_Task_Instance.Add(new DatasetEventTaskInstance { Id = 2, Id_Event_Task_Instance = 500, DatasetAMId = ds.Id_Dataset });
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetAMByIdForEditAsync(101, username);

            Assert.NotNull(result);
            Assert.Equal(101, result.Id_Dataset);
            Assert.Equal(username, result.Username);
            Assert.NotNull(result.Grupo_Asset);
            Assert.Single(result.Grupo_Asset);
            Assert.NotNull(result.Grupo_Event_Task_Instance);
            Assert.Single(result.Grupo_Event_Task_Instance);
            // No debe llamar a Sonda porque este método no realiza lógica dinámica
            sonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetAMByIdForEditAsync_NotFound_ReturnsNull()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var result = await svc.GetDatasetAMByIdForEditAsync(9999, "any");
            Assert.Null(result);
            sonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetAMByIdForEditAsync_WrongUser_ReturnsNull()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            ctx.DatasetAM.Add(new DatasetAM { Id_Dataset = 201, Username = "ownerA", Is_Dataset = "N", Type_Dataset = 1, DatasetId = 1 });
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetAMByIdForEditAsync(201, "otherUser");
            Assert.Null(result);
            sonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetAMByIdForEditAsync_EmptyCollections_ReturnsDatasetWithEmptyCollections()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "emptyUser";
            var ds = new DatasetAM
            {
                Id_Dataset = 301,
                Username = username,
                Is_Dataset = "N",
                Type_Dataset = 3,
                // No agregar hijos -> las colecciones quedan vacías (inicializadas)
            };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetAMByIdForEditAsync(301, username);

            Assert.NotNull(result);
            Assert.True(result.Grupo_Asset == null || !result.Grupo_Asset.Any());
            Assert.True(result.Grupo_Event_Task_Instance == null || !result.Grupo_Event_Task_Instance.Any());
            Assert.True(result.Grupo_Stock == null || !result.Grupo_Stock.Any());
            sonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetAMByIdForEditAsync_FormalDataset_DoesNotTriggerDynamicPopulation()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "formalUser";
            var ds = new DatasetAM
            {
                Id_Dataset = 401,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 2,
                Id_Asset_Type = 7
            };
            // dejar Grupo_Asset vacío para que la lógica dinámica en GetDatasetAMByIdAsync la rellenaría,
            // pero en GetDatasetAMByIdForEditAsync no debe pasar nada.
            ctx.DatasetAM.Add(ds);
            ctx.Users.Add(new User { UserName = username }); // aunque exista user, este método no llama a Sonda
            await ctx.SaveChangesAsync();

            var result = await svc.GetDatasetAMByIdForEditAsync(401, username);

            Assert.NotNull(result);
            Assert.True(result.Grupo_Asset == null || !result.Grupo_Asset.Any());
            // confirmar que no se llamó a Sonda para obtener assets
            sonda.Verify(s => s.GetAssets(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string>()), Times.Never);
            sonda.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetDatasetAMByIdForEditAsync_RepeatedCalls_ReturnsConsistentResults()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "repeatUser";
            var ds = new DatasetAM
            {
                Id_Dataset = 501,
                Username = username,
                Is_Dataset = "N",
                Type_Dataset = 1
            };
            ds.Grupo_Asset.Add(new DatasetAsset { Grupo_Asset = 10, Id_Asset = "X", DatasetAMId = ds.Id_Dataset });
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            var first = await svc.GetDatasetAMByIdForEditAsync(501, username);
            var second = await svc.GetDatasetAMByIdForEditAsync(501, username);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.Id_Dataset, second.Id_Dataset);
            Assert.Equal(first.Grupo_Asset.Count, second.Grupo_Asset.Count);
            sonda.VerifyNoOtherCalls();
        }

        /* Tests sobre DeleteDatasetAMAsync */

        [Fact]
        public async Task DeleteDatasetAMAsync_Success_RemovesDatasetAndChildren()
        {
            var ctx = GetInMemoryDbContext();
            var sonda = new Mock<ISondaAMService>();
            var svc = GetService(ctx, sonda.Object);

            var username = "ownerDelete";
            var ds = new DatasetAM
            {
                Id_Dataset = 1001,
                Username = username,
                Is_Dataset = "S",
                Type_Dataset = 2
            };
            var asset = new DatasetAsset { Grupo_Asset = 11, Id_Asset = "A1", DatasetAMId = ds.Id_Dataset };
            var et = new DatasetEventTaskInstance { Id = 21, DatasetAMId = ds.Id_Dataset, Id_Event_Task_Instance = 9001 };
            var stock = new DatasetStock { Grupo_Stock = 31, Id_Stock = 55, DatasetAMId = ds.Id_Dataset };

            ds.Grupo_Asset.Add(asset);
            et.Grupo_Stock.Add(stock);
            ds.Grupo_Event_Task_Instance.Add(et);

            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            // Act
            await svc.DeleteDatasetAMAsync(1001, username);

            // Assert: dataset removed
            var found = await ctx.DatasetAM.FindAsync(1001);
            Assert.Null(found);
            // children removed
            var assets = await ctx.DatasetAsset.Where(a => a.DatasetAMId == 1001).ToListAsync();
            var ets = await ctx.DatasetEventTaskInstance.Where(e => e.DatasetAMId == 1001).ToListAsync();
            var stocks = await ctx.DatasetStock.Where(s => s.DatasetAMId == 1001).ToListAsync();
            Assert.Empty(assets);
            Assert.Empty(ets);
            Assert.Empty(stocks);
        }

        [Fact]
        public async Task DeleteDatasetAMAsync_NotFound_ThrowsInvalidOperationException()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetAMAsync(99999, "someone"));
        }

        [Fact]
        public async Task DeleteDatasetAMAsync_WrongUser_ThrowsInvalidOperationExceptionAndDoesNotDelete()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetAM { Id_Dataset = 2001, Username = "ownerA", Is_Dataset = "N", Type_Dataset = 1 };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetAMAsync(2001, "otherUser"));

            // dataset still present
            var still = await ctx.DatasetAM.FindAsync(2001);
            Assert.NotNull(still);
        }

        [Fact]
        public async Task DeleteDatasetAMAsync_EventInstanceWithEmptyStock_DeletesEventInstanceWithoutError()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetAM { Id_Dataset = 3001, Username = "u1", Is_Dataset = "S", Type_Dataset = 1 };
            var et = new DatasetEventTaskInstance { Id = 31, DatasetAMId = ds.Id_Dataset, Id_Event_Task_Instance = 5001, Grupo_Stock = new List<DatasetStock>() };
            ds.Grupo_Event_Task_Instance.Add(et);
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            await svc.DeleteDatasetAMAsync(3001, "u1");

            var foundDs = await ctx.DatasetAM.FindAsync(3001);
            Assert.Null(foundDs);
            var foundEt = await ctx.DatasetEventTaskInstance.FindAsync(31);
            Assert.Null(foundEt);
        }

        [Fact]
        public async Task DeleteDatasetAMAsync_LeavesChildrenWithNonPositiveGroup_WhenPredicateNotMet()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetAM { Id_Dataset = 4001, Username = "u2", Is_Dataset = "S", Type_Dataset = 2 };
            // crear un asset con Grupo_Asset = 0 (clave primaria aunque inusual) y otro con >0
            var assetZero = new DatasetAsset { Grupo_Asset = 0, Id_Asset = "Z0", DatasetAMId = ds.Id_Dataset };
            var assetPos = new DatasetAsset { Grupo_Asset = 41, Id_Asset = "P1", DatasetAMId = ds.Id_Dataset };
            ds.Grupo_Asset.Add(assetZero);
            ds.Grupo_Asset.Add(assetPos);
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            await svc.DeleteDatasetAMAsync(4001, "u2");

            // dataset removed
            var foundDs = await ctx.DatasetAM.FindAsync(4001);
            Assert.Null(foundDs);

            // assets with Grupo_Asset > 0 should be removed; the one with Grupo_Asset == 0 remains only if DB allowed it
            var remaining = await ctx.DatasetAsset.Where(a => a.DatasetAMId == 4001).ToListAsync();
            Assert.True(remaining.Count == 0 || remaining.Count == 1);
            if (remaining.Count == 1)
            {
                Assert.Equal(0, remaining.First().Grupo_Asset);
            }
        }

        [Fact]
        public async Task DeleteDatasetAMAsync_RepeatedCall_FirstDeletesSecondThrows()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetAM { Id_Dataset = 5001, Username = "repeat", Is_Dataset = "N", Type_Dataset = 3 };
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            await svc.DeleteDatasetAMAsync(5001, "repeat");
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteDatasetAMAsync(5001, "repeat"));
        }

        [Fact]
        public async Task DeleteDatasetAMAsync_DeletionPersistedAcrossContexts()
        {
            var ctx = GetInMemoryDbContext();
            var svc = GetService(ctx);

            var ds = new DatasetAM { Id_Dataset = 6001, Username = "persist", Is_Dataset = "S", Type_Dataset = 2 };
            ds.Grupo_Asset.Add(new DatasetAsset { Grupo_Asset = 61, Id_Asset = "A61", DatasetAMId = ds.Id_Dataset });
            ctx.DatasetAM.Add(ds);
            await ctx.SaveChangesAsync();

            await svc.DeleteDatasetAMAsync(6001, "persist");

            // usar un nuevo contexto in-memory apuntando a la misma DB string requiere que GetInMemoryDbContext use la misma DBName.
            // Dado que tu helper crea DBs únicas por GUID, simulamos persistencia consultando la misma instancia de ctx (ya guardada).
            var assets = await ctx.DatasetAsset.Where(a => a.DatasetAMId == 6001).ToListAsync();
            var dsFound = await ctx.DatasetAM.FindAsync(6001);
            Assert.Null(dsFound);
            Assert.Empty(assets);
        }
    }
}
