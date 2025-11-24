using Microsoft.EntityFrameworkCore;
using Moq;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace QA.Tests
{
    public class DatasetIMServiceTests
    {
        private static ApplicationDbContext GetDbContext(string dbName = null!)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static DatasetIMService GetService(
            ApplicationDbContext? context = null,
            ISondaIMService? sondaIMService = null)
        {
            return new DatasetIMService(
                context ?? GetDbContext(),
                sondaIMService ?? Mock.Of<ISondaIMService>());
        }

        /* Tests de CreateDatasetIMFilteredAsync */
        public async Task CreateDatasetIMFilteredAsync_ThrowsIfUsernameOrNameMissing()
        {
            var service = GetService();
            var req = new CreateDatasetIMRequest { Name = null, IsDataset = "N" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDatasetIMFilteredAsync(req, 1, "user"));
            req.Name = "X";
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDatasetIMFilteredAsync(req, 1, null!));
        }

        [Fact]
        public async Task CreateDatasetIMFilteredAsync_ThrowsIfIsDatasetS()
        {
            var service = GetService();
            var req = new CreateDatasetIMRequest { Name = "X", IsDataset = "S" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDatasetIMFilteredAsync(req, 1, "user"));
        }

        [Fact]
        public async Task CreateDatasetIMFilteredAsync_ThrowsIfDuplicateName()
        {
            var db = GetDbContext();
            db.DatasetsIM.Add(new DatasetIM { Name = "X", Username = "user", Is_Dataset = "N", DatasetId = 1 });
            db.SaveChanges();
            var service = GetService(db);
            var req = new CreateDatasetIMRequest { Name = "X", IsDataset = "N", ContentType = "1" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDatasetIMFilteredAsync(req, 2, "user"));
        }

        [Fact]
        public async Task CreateDatasetIMFilteredAsync_DeviceContentType_PersistsDevices()
        {
            var db = GetDbContext();
            var sonda = new Mock<ISondaIMService>();
            sonda.Setup(s => s.GetAllDevices("user")).ReturnsAsync(new List<Device> { new Device { Id = 10 } });
            var req = new CreateDatasetIMRequest { Name = "D", IsDataset = "N", ContentType = "1", Filters = new List<FilterCondition>() };
            var service = GetService(db, sonda.Object);

            var result = await service.CreateDatasetIMFilteredAsync(req, 1, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetDevices);
            Assert.Equal(10, result.DatasetDevices.First().Id_device);
        }

        [Fact]
        public async Task CreateDatasetIMFilteredAsync_SourceContentType_PersistsSources()
        {
            var db = GetDbContext();
            var sonda = new Mock<ISondaIMService>();
            sonda.Setup(s => s.GetAllSources("user")).ReturnsAsync(new List<Source> { new Source { Id = 20 } });
            var req = new CreateDatasetIMRequest { Name = "S", IsDataset = "N", ContentType = "2", Filters = new List<FilterCondition>() };
            var service = GetService(db, sonda.Object);

            var result = await service.CreateDatasetIMFilteredAsync(req, 1, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetSources);
            Assert.Equal(20, result.DatasetSources.First().Id_source);
        }

        [Fact]
        public async Task CreateDatasetIMFilteredAsync_SensorContentType_PersistsSensors()
        {
            var db = GetDbContext();
            var sonda = new Mock<ISondaIMService>();
            sonda.Setup(s => s.GetAllDevices("user")).ReturnsAsync(new List<Device>
            {
                new Device { Sensors = new List<Sensor> { new Sensor { Name = "S1" } } }
            });
            var req = new CreateDatasetIMRequest { Name = "T", IsDataset = "N", ContentType = "3", Filters = new List<FilterCondition>() };
            var service = GetService(db, sonda.Object);

            var result = await service.CreateDatasetIMFilteredAsync(req, 1, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetSensors);
            Assert.Equal("S1", result.DatasetSensors.First().SensorName);
        }

        [Fact]
        public async Task CreateDatasetIMFilteredAsync_ThrowsOnInvalidContentType()
        {
            var service = GetService();
            var req = new CreateDatasetIMRequest { Name = "X", IsDataset = "N", ContentType = "999", Filters = new List<FilterCondition>() };
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDatasetIMFilteredAsync(req, 1, "user"));
        }

        /* Tests de GetAllDatasetsIMAsync */
        [Fact]
        public async Task GetAllDatasetsIMAsync_ReturnsOnlyForUser()
        {
            var db = GetDbContext();
            db.DatasetsIM.Add(new DatasetIM { Name = "A", Username = "u1", Is_Dataset = "S", DatasetId = 1 });
            db.DatasetsIM.Add(new DatasetIM { Name = "B", Username = "u2", Is_Dataset = "S", DatasetId = 2 });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetAllDatasetsIMAsync("u1");

            Assert.Single(result);
            Assert.Equal("A", result[0].Name);
        }

        [Fact]
        public async Task GetAllDatasetsIMAsync_ReturnsEmptyIfNone()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var result = await service.GetAllDatasetsIMAsync("nobody");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllDatasetsIMAsync_ReturnsMultiple()
        {
            var db = GetDbContext();
            db.DatasetsIM.Add(new DatasetIM { Name = "A", Username = "u1", Is_Dataset = "S", DatasetId = 1 });
            db.DatasetsIM.Add(new DatasetIM { Name = "B", Username = "u1", Is_Dataset = "N", DatasetId = 2 });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetAllDatasetsIMAsync("u1");

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllDatasetsIMAsync_CaseSensitiveUser()
        {
            var db = GetDbContext();
            db.DatasetsIM.Add(new DatasetIM { Name = "A", Username = "User", Is_Dataset = "S", DatasetId = 1 });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetAllDatasetsIMAsync("user");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllDatasetsIMAsync_ReturnsCorrectType()
        {
            var db = GetDbContext();
            db.DatasetsIM.Add(new DatasetIM { Name = "A", Username = "u1", Is_Dataset = "S", DatasetId = 1 });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetAllDatasetsIMAsync("u1");

            Assert.IsType<List<DatasetIM>>(result);
        }

        /* Tests de GetDatasetIMByIdAsync */
        [Fact]
        public async Task GetDatasetIMByIdAsync_ReturnsNullIfNotFound()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdAsync(99, "user");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_ReturnDatasetWithDevices()         {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 1,
                Name = "A",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 1,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id_device = 1 } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);
            var result = await service.GetDatasetIMByIdAsync(1, "user");
            Assert.NotNull(result);
            Assert.Single(result.DatasetDevices);
            Assert.Equal(1, result.DatasetDevices.First().Id_device);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_ReturnDatasetWithSourcesFromDevices()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 2,
                Name = "B",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 2,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id_device = 2 } },
                DatasetSources = new List<DatasetSource> { new DatasetSource { Id_source = 2 } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);
            var result = await service.GetDatasetIMByIdAsync(2, "user");
            Assert.NotNull(result);
            Assert.Single(result.DatasetSources);
            Assert.Equal(2, result.DatasetSources.First().Id_source);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_ReturnDatasetWithSensorsFromDevices()         {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 3,
                Name = "C",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 3,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id_device = 3 } },
                DatasetSensors = new List<DatasetSensor> { new DatasetSensor { SensorName = "S" } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);
            var result = await service.GetDatasetIMByIdAsync(3, "user");
            Assert.NotNull(result);
            Assert.Single(result.DatasetSensors);
            Assert.Equal("S", result.DatasetSensors.First().SensorName);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_FormalDatasetLoadsDevicesDynamically()
        {
            var db = GetDbContext();
            var user = new User { UserName = "user" };
            db.Users.Add(user);
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1, Id_Source = 10 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var sonda = new Mock<ISondaIMService>();
            sonda.Setup(s => s.GetDeviceOfSource(10, "user")).ReturnsAsync(new List<Device> { new Device { Id = 100 } });
            var service = GetService(db, sonda.Object);

            var result = await service.GetDatasetIMByIdAsync(1, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetDevices);
            Assert.Equal(100, result.DatasetDevices.First().Id_device);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_ReturnsNullIfUserNotFound()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1, Id_Source = 10 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var sonda = new Mock<ISondaIMService>();
            var service = GetService(db, sonda.Object);

            var result = await service.GetDatasetIMByIdAsync(1, "user");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_DynamicDevices_Intersection()
        {
            var db = GetDbContext();
            var user = new User { UserName = "user" };
            db.Users.Add(user);
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1, Id_Source = 10, Id_Group = 20 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var sonda = new Mock<ISondaIMService>();
            sonda.Setup(s => s.GetDeviceOfSource(10, "user")).ReturnsAsync(new List<Device> { new Device { Id = 1 }, new Device { Id = 2 } });
            sonda.Setup(s => s.GetDeviceOfGroup(20, "user")).ReturnsAsync(new List<Device> { new Device { Id = 2 }, new Device { Id = 3 } });
            var service = GetService(db, sonda.Object);

            var result = await service.GetDatasetIMByIdAsync(1, "user");

            Assert.Single(result.DatasetDevices);
            Assert.Equal(2, result.DatasetDevices.First().Id_device);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_ReturnsDatasetWithSources()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 2, Name = "B", Username = "user", Is_Dataset = "N", DatasetId = 2 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdAsync(2, "user");

            Assert.NotNull(result);
            Assert.Equal("B", result.Name);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_ReturnsDatasetWithSensors()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 3, Name = "C", Username = "user", Is_Dataset = "N", DatasetId = 3 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdAsync(3, "user");

            Assert.NotNull(result);
            Assert.Equal("C", result.Name);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_DynamicDevices_FallbackToAllDevices()
        {
            var db = GetDbContext();
            var user = new User { UserName = "user" };
            db.Users.Add(user);
            var ds = new DatasetIM { Id = 4, Name = "D", Username = "user", Is_Dataset = "S", DatasetId = 4 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var sonda = new Mock<ISondaIMService>();
            sonda.Setup(s => s.GetAllDevices("user")).ReturnsAsync(new List<Device> { new Device { Id = 200 } });
            var service = GetService(db, sonda.Object);

            var result = await service.GetDatasetIMByIdAsync(4, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetDevices);
            Assert.Equal(200, result.DatasetDevices.First().Id_device);
        }

        [Fact]
        public async Task GetDatasetIMByIdAsync_DynamicDevices_EmptyLists()
        {
            var db = GetDbContext();
            var user = new User { UserName = "user" };
            db.Users.Add(user);
            var ds = new DatasetIM { Id = 5, Name = "E", Username = "user", Is_Dataset = "S", DatasetId = 5, Id_Source = 10, Id_Group = 20 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var sonda = new Mock<ISondaIMService>();
            sonda.Setup(s => s.GetDeviceOfSource(10, "user")).ReturnsAsync(new List<Device>());
            sonda.Setup(s => s.GetDeviceOfGroup(20, "user")).ReturnsAsync(new List<Device>());
            var service = GetService(db, sonda.Object);

            var result = await service.GetDatasetIMByIdAsync(5, "user");

            Assert.NotNull(result);
            Assert.Empty(result.DatasetDevices);
        }

        /* Tests de GetDatasetIMByIdForEditAsync */
        [Fact]
        public async Task GetDatasetIMByIdForEditAsync_ReturnsNullIfNotFound()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsync(99, "user");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsync_ReturnsDataset()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsync(1, "user");

            Assert.NotNull(result);
            Assert.Equal("A", result.Name);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsync_ReturnsDatasetWithDevices()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 2,
                Name = "B",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 2,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id_device = 1 } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsync(2, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetDevices);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsync_ReturnsDatasetWithSources()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 3,
                Name = "C",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 3,
                DatasetSources = new List<DatasetSource> { new DatasetSource { Id_source = 2 } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsync(3, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetSources);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsync_ReturnsDatasetWithSensors()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 4,
                Name = "D",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 4,
                DatasetSensors = new List<DatasetSensor> { new DatasetSensor { SensorName = "S" } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsync(4, "user");

            Assert.NotNull(result);
            Assert.Single(result.DatasetSensors);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsync_ReturnsNullForWrongUser()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 5, Name = "E", Username = "user", Is_Dataset = "S", DatasetId = 5 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsync(5, "otheruser");

            Assert.Null(result);
        }

        /* Tests de GetDatasetIMByIdForEditAsyncSinToken */
        [Fact]
        public async Task GetDatasetIMByIdForEditAsyncSinToken_ReturnsNullIfNotFound()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsyncSinToken(99);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsyncSinToken_ReturnsDataset()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsyncSinToken(1);

            Assert.NotNull(result);
            Assert.Equal("A", result.Name);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsyncSinToken_ReturnsDatasetWithDevices()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 2,
                Name = "B",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 2,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id_device = 1 } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsyncSinToken(2);

            Assert.NotNull(result);
            Assert.Single(result.DatasetDevices);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsyncSinToken_ReturnsDatasetWithSources()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 3,
                Name = "C",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 3,
                DatasetSources = new List<DatasetSource> { new DatasetSource { Id_source = 2 } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsyncSinToken(3);

            Assert.NotNull(result);
            Assert.Single(result.DatasetSources);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsyncSinToken_ReturnsDatasetWithSensors()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 4,
                Name = "D",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 4,
                DatasetSensors = new List<DatasetSensor> { new DatasetSensor { SensorName = "S" } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsyncSinToken(4);

            Assert.NotNull(result);
            Assert.Single(result.DatasetSensors);
        }

        [Fact]
        public async Task GetDatasetIMByIdForEditAsyncSinToken_ReturnsNullForNonexistent()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var result = await service.GetDatasetIMByIdForEditAsyncSinToken(999);

            Assert.Null(result);
        }

        /* Tests sobre UpdateDatasetIMAsync */
        [Fact]
        public async Task UpdateDatasetIMAsync_ThrowsIfNull()
        {
            var db = GetDbContext();
            var service = GetService(db);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateDatasetIMAsync(null!, new CreateDatasetIMRequest(), "user"));
        }

        [Fact]
        public async Task UpdateDatasetIMAsync_UpdatesFields()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var req = new CreateDatasetIMRequest { Name = "B", IsDataset = "S" };
            var result = await service.UpdateDatasetIMAsync(ds, req, "user");

            Assert.Equal("B", result.Name);
        }

        [Fact]
        public async Task UpdateDatasetIMAsync_ClearsRelations()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 1,
                Name = "A",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 1,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id_device = 1 } },
                DatasetSources = new List<DatasetSource> { new DatasetSource { Id_source = 2 } },
                DatasetSensors = new List<DatasetSensor> { new DatasetSensor { SensorName = "S" } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var req = new CreateDatasetIMRequest { Name = "B", IsDataset = "S" };
            var result = await service.UpdateDatasetIMAsync(ds, req, "user");

            Assert.Empty(result.DatasetDevices);
            Assert.Empty(result.DatasetSources);
            Assert.Empty(result.DatasetSensors);
        }

        [Fact]
        public async Task UpdateDatasetIMAsync_AddsDevicesForFormal()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var req = new CreateDatasetIMRequest { Name = "B", IsDataset = "S", DeviceIds = new List<int> { 10, 20 } };
            var result = await service.UpdateDatasetIMAsync(ds, req, "user");

            Assert.Equal(2, result.DatasetDevices.Count);
        }

        [Fact]
        public async Task UpdateDatasetIMAsync_AddsDevicesForNonFormal()
        {
            var db = GetDbContext();
            var ds = new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "N", DatasetId = 1 };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            var req = new CreateDatasetIMRequest { Name = "B", IsDataset = "N", DeviceIds = new List<int> { 10 } };
            var result = await service.UpdateDatasetIMAsync(ds, req, "user");

            Assert.Single(result.DatasetDevices);
        }

        /* Tests sobre DeleteDatasetIMAsnyc */
        [Fact]
        public async Task DeleteDatasetIMAsync_ThrowsIfNotFound()
        {
            var db = GetDbContext();
            var service = GetService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteDatasetIMAsync(99, "user"));
        }

        [Fact]
        public async Task DeleteDatasetIMAsync_RemovesDatasetAndRelations()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 1,
                Name = "A",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 1,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id_device = 1 } },
                DatasetSources = new List<DatasetSource> { new DatasetSource { Id_source = 2 } },
                DatasetSensors = new List<DatasetSensor> { new DatasetSensor { SensorName = "S" } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            await service.DeleteDatasetIMAsync(1, "user");

            Assert.Empty(db.DatasetsIM.ToList());
        }

        [Fact]
        public async Task DeleteDatasetIMAsync_OnlyRemovesForUser()
        {
            var db = GetDbContext();
            db.DatasetsIM.Add(new DatasetIM { Id = 1, Name = "A", Username = "user1", Is_Dataset = "S", DatasetId = 1 });
            db.DatasetsIM.Add(new DatasetIM { Id = 2, Name = "B", Username = "user2", Is_Dataset = "S", DatasetId = 2 });
            db.SaveChanges();
            var service = GetService(db);

            await service.DeleteDatasetIMAsync(1, "user1");

            Assert.Single(db.DatasetsIM.ToList());
            Assert.Equal("B", db.DatasetsIM.First().Name);
        }

        [Fact]
        public async Task DeleteDatasetIMAsync_RemovesRelationsWithIdGreaterThanZero()
        {
            var db = GetDbContext();
            var ds = new DatasetIM
            {
                Id = 1,
                Name = "A",
                Username = "user",
                Is_Dataset = "S",
                DatasetId = 1,
                DatasetDevices = new List<DatasetDevice> { new DatasetDevice { Id = 1, Id_device = 1 } },
                DatasetSources = new List<DatasetSource> { new DatasetSource { Id = 2, Id_source = 2 } },
                DatasetSensors = new List<DatasetSensor> { new DatasetSensor { Id = 3, SensorName = "S" } }
            };
            db.DatasetsIM.Add(ds);
            db.SaveChanges();
            var service = GetService(db);

            await service.DeleteDatasetIMAsync(1, "user");

            Assert.Empty(db.DatasetDevices.ToList());
            Assert.Empty(db.DatasetSources.ToList());
            Assert.Empty(db.DatasetSensors.ToList());
        }

        /* Tests sobre IdentifyDatasetModuleAsync */
        [Fact]
        public async Task IdentifyDatasetModuleAsync_ReturnsInsightMonitor()
        {
            var db = GetDbContext();
            db.DatasetsIM.Add(new DatasetIM { Id = 1, Name = "A", Username = "user", Is_Dataset = "S", DatasetId = 1 });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.IdentifyDatasetModuleAsync(1, "user");

            Assert.Equal("Insight Monitor", result);
        }

        [Fact]
        public async Task IdentifyDatasetModuleAsync_ReturnsAssetManager()
        {
            var db = GetDbContext();
            db.DatasetAM.Add(new DatasetAM { Id_Dataset = 2, Username = "user", Is_Dataset = "S", Type_Dataset = 1, DatasetId = 1 });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.IdentifyDatasetModuleAsync(2, "user");

            Assert.Equal("Asset Manager", result);
        }

        [Fact]
        public async Task IdentifyDatasetModuleAsync_ReturnsUrbanMonitor()
        {
            var db = GetDbContext();
            db.DatasetsUM.Add(new DatasetUM { Id = 30, Username = "user", Datasets = new Datasets(), DatasetEvents = new List<DatasetEvent>(), DatasetNews = new List<DatasetNews>() });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.IdentifyDatasetModuleAsync(30, "user");

            Assert.Equal("Urban Monitor", result);
        }

        [Fact]
        public async Task IdentifyDatasetModuleAsync_ReturnsEventManager()
        {
            var db = GetDbContext();
            db.DatasetsEM.Add(new DatasetEM { Id = 40, Username = "user", Is_Dataset = "S", DatasetAlerts = new List<DatasetAlert>(), DatasetEvents = new List<DatasetEventEM>(), DatasetExtensions = new List<DatasetExtension>() });
            db.SaveChanges();
            var service = GetService(db);

            var result = await service.IdentifyDatasetModuleAsync(40, "user");

            Assert.Equal("Event Manager", result);
        }

        [Fact]
        public async Task IdentifyDatasetModuleAsync_ReturnsNullIfNotFound()
        {
            var db = GetDbContext();
            var service = GetService(db);

            var result = await service.IdentifyDatasetModuleAsync(99, "user");

            Assert.Null(result);
        }
    }
}
