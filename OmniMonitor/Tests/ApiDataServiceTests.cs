using System;
using System.Collections.Generic;
using System.Reflection;

using Moq;

using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using OmniMonitor.Shared.Dtos.EM;

using Xunit;

namespace QA.Tests
{
    public class ApiDataServiceTests
    {
        public class TestObject
        {
            public int Numero { get; set; }
            public string Texto { get; set; }
            public DateTime Fecha { get; set; }
            public bool Activo { get; set; }
            public string Categoria { get; set; }
            public List<string> Tags { get; set; }
        }

        private static List<TestObject> GetSampleObjects()
        {
            return new List<TestObject>
            {
                new TestObject { Numero = 1, Texto = "Alpha", Fecha = new DateTime(2025, 1, 1), Activo = true, Categoria = "A", Tags = new List<string>{"x","y"} },
                new TestObject { Numero = 2, Texto = "Beta", Fecha = new DateTime(2025, 6, 1), Activo = false, Categoria = "B", Tags = new List<string>{"y","z"} },
                new TestObject { Numero = 3, Texto = "Gamma", Fecha = new DateTime(2025, 12, 1), Activo = true, Categoria = "C", Tags = new List<string>{"z"} }
            };
        }

        /* Tests sobre StaticFilterObjects */

        public static IEnumerable<object[]> FilterTestData()
        {
            // Number
            yield return new object[] { "Numero", FilterType.Equals, 2, FilterValueType.Number, 1 };
            yield return new object[] { "Numero", FilterType.NotEquals, 2, FilterValueType.Number, 2 };
            yield return new object[] { "Numero", FilterType.GreaterThan, 1, FilterValueType.Number, 2 };
            yield return new object[] { "Numero", FilterType.LessThan, 3, FilterValueType.Number, 2 };

            // String
            yield return new object[] { "Texto", FilterType.Equals, "Alpha", FilterValueType.String, 1 };
            yield return new object[] { "Texto", FilterType.NotEquals, "Alpha", FilterValueType.String, 2 };
            yield return new object[] { "Texto", FilterType.Contains, "a", FilterValueType.String, 3 }; 
            yield return new object[] { "Texto", FilterType.StartsWith, "B", FilterValueType.String, 1 };
            yield return new object[] { "Texto", FilterType.EndsWith, "a", FilterValueType.String, 3 };

            // Date
            yield return new object[] { "Fecha", FilterType.Between, new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 6, 30) }, FilterValueType.Date, 2 };
            yield return new object[] { "Fecha", FilterType.Equals, new DateTime(2025, 1, 1), FilterValueType.Date, 1 };

            // Boolean
            yield return new object[] { "Activo", FilterType.Equals, true, FilterValueType.Boolean, 2 };
            yield return new object[] { "Activo", FilterType.NotEquals, true, FilterValueType.Boolean, 1 };

            // Enum (simple)
            yield return new object[] { "Categoria", FilterType.In, new List<object> { "A", "C" }, FilterValueType.Enum, 2 };
            yield return new object[] { "Categoria", FilterType.Equals, "B", FilterValueType.Enum, 1 };
            yield return new object[] { "Categoria", FilterType.NotEquals, "B", FilterValueType.Enum, 2 };

            // Enum (colección)
            yield return new object[] { "Tags", FilterType.In, new List<object> { "x" }, FilterValueType.Enum, 1 };
            yield return new object[] { "Tags", FilterType.In, new List<object> { "y" }, FilterValueType.Enum, 2 };
        }

        [Theory]
        [MemberData(nameof(FilterTestData))]
        public void StaticFilterObjects_CoversAllCases(string attribute, FilterType type, object condition, FilterValueType valueType, int expectedCount)
        {
            var filters = new List<FilterCondition>
            {
                new FilterCondition
                {
                    AttributeName = attribute,
                    Type = type,
                    Condition = condition,
                    ValueType = valueType
                }
            };
            var result = ApiDataService.StaticFilterObjects(GetSampleObjects(), filters);
            Assert.Equal(expectedCount, result.Count);
        }

        /* Tests sobre FilterObjects */
        public static IEnumerable<object[]> FilterObjectsTestData()
        {
            yield return new object[] { "Numero", FilterType.Equals, 1, FilterValueType.Number, 1 };
            yield return new object[] { "Texto", FilterType.Contains, "a", FilterValueType.String, 3 };
            yield return new object[] { "Fecha", FilterType.Between, new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 6, 30) }, FilterValueType.Date, 2 };
            yield return new object[] { "Activo", FilterType.Equals, true, FilterValueType.Boolean, 2 };
            yield return new object[] { "Categoria", FilterType.In, new List<object> { "A", "C" }, FilterValueType.Enum, 2 };
        }

        [Theory]
        [MemberData(nameof(FilterObjectsTestData))]
        public void FilterObjects_CoversMainCases(string attribute, FilterType type, object condition, FilterValueType valueType, int expectedCount)
        {
            var filters = new List<FilterCondition>
            {
                new FilterCondition
                {
                    AttributeName = attribute,
                    Type = type,
                    Condition = condition,
                    ValueType = valueType
                }
            };
            var service = new ApiDataService(null, null, null, null, null, null, null, null, null, null);
            var result = service.FilterObjects(GetSampleObjects(), filters);
            Assert.Equal(expectedCount, result.Count);
        }

        /* Tests sobre MatchesFilter */
        public static IEnumerable<object[]> MatchesFilterTestData()
        {
            yield return new object[] { 2, new FilterCondition { Type = FilterType.Equals, Condition = 2, ValueType = FilterValueType.Number }, true };
            yield return new object[] { 1, new FilterCondition { Type = FilterType.GreaterThan, Condition = 0, ValueType = FilterValueType.Number }, true };
            yield return new object[] { "Alpha", new FilterCondition { Type = FilterType.Contains, Condition = "Al", ValueType = FilterValueType.String }, true };
            yield return new object[] { "Beta", new FilterCondition { Type = FilterType.NotEquals, Condition = "Alpha", ValueType = FilterValueType.String }, true };
            yield return new object[] { new DateTime(2025, 1, 1), new FilterCondition { Type = FilterType.Between, Condition = new object[] { new DateTime(2025, 1, 1), new DateTime(2025, 6, 30) }, ValueType = FilterValueType.Date }, true };
            yield return new object[] { true, new FilterCondition { Type = FilterType.Equals, Condition = true, ValueType = FilterValueType.Boolean }, true };
            yield return new object[] { "A", new FilterCondition { Type = FilterType.In, Condition = new List<object> { "A", "B" }, ValueType = FilterValueType.Enum }, true };
            yield return new object[] { "C", new FilterCondition { Type = FilterType.In, Condition = new List<object> { "A", "B" }, ValueType = FilterValueType.Enum }, false };
        }

        [Theory]
        [MemberData(nameof(MatchesFilterTestData))]
        public void MatchesFilter_CoversMainCases(object value, FilterCondition filter, bool expected)
        {
            var service = new ApiDataService(null, null, null, null, null, null, null, null, null, null);
            var method = typeof(ApiDataService).GetMethod("MatchesFilter", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = (bool)method.Invoke(service, new object[] { value, filter });
            Assert.Equal(expected, result);
        }

        /* Tests sobre ObjectToDictionary */
        [Fact]
        public void ObjectToDictionary_ConvertsAllProperties()
        {
            var obj = new TestObject
            {
                Numero = 5,
                Texto = "Test",
                Fecha = new DateTime(2025, 11, 20),
                Activo = true,
                Categoria = "Z",
                Tags = new List<string> { "a", "b" }
            };
            var service = new ApiDataService(null, null, null, null, null, null, null, null, null, null);
            var method = typeof(ApiDataService).GetMethod("ObjectToDictionary", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (IDictionary<string, object>)method.Invoke(service, new object[] { obj });

            Assert.Equal(5, dict["Numero"]);
            Assert.Equal("Test", dict["Texto"]);
            Assert.Equal(new DateTime(2025, 11, 20), dict["Fecha"]);
            Assert.True((bool)dict["Activo"]);
            Assert.Equal("Z", dict["Categoria"]);
            Assert.Equal(obj.Tags, dict["Tags"]);
        }

        /* Tests de GetDataForOperand */
        [Fact]
        public async Task GetDataForOperand_IM_Device_ReturnsDevices()
        {
            // Arrange
            var datasetIM = new DatasetIM
            {
                DatasetDevices = new List<DatasetDevice>
                {
                    new DatasetDevice { Id_device = 14 }
                }
            };
            var device = new Device { Id = 14, Name = "Device1", Sensors = new List<Sensor>() };

            var mockDatasetService = new Mock<IDatasetService>();
            mockDatasetService.Setup(x => x.GetDatasetIMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(datasetIM);

            var mockSondaIMService = new Mock<ISondaIMService>();
            mockSondaIMService.Setup(x => x.GetDeviceById(14, It.IsAny<string>()))
                .ReturnsAsync(device);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                null,
                mockSondaIMService.Object,
                null,
                mockDatasetService.Object,
                null,
                null,
                null,
                null
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.InsightMonitor,
                EntityName = EntityName.Device,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperand(operand, "user");

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(14, ((Device)list[0]).Id);
        }

        [Fact]
        public async Task GetDataForOperand_UM_News_ReturnsNews()
        {
            // Arrange
            var datasetUM = new DatasetUM
            {
                DatasetNews = new List<DatasetNews>
                {
                    new DatasetNews { Id_news = 5 }
                }
            };
            var news = new News { Id = 5, Title = "Test News" };

            var mockDatasetUMService = new Mock<IDatasetUMService>();
            mockDatasetUMService.Setup(x => x.GetDatasetUMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(datasetUM);

            var mockSondaUMService = new Mock<ISondaUMService>();
            mockSondaUMService.Setup(x => x.GetNewsById(5, It.IsAny<string>()))
                .ReturnsAsync(news);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                null,
                null,
                null,
                null,
                mockDatasetUMService.Object,
                null,
                mockSondaUMService.Object,
                null
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.UrbanMonitor,
                EntityName = EntityName.New,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperand(operand, "user");

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(5, ((News)list[0]).Id);
        }

        [Fact]
        public async Task GetDataForOperand_EM_Alert_ReturnsAlerts()
        {
            // Arrange
            var datasetEM = new DatasetEM
            {
                DatasetAlerts = new List<DatasetAlert>
                {
                    new DatasetAlert { Id_alert = 7 }
                }
            };
            var alert = new AlertDto { AlertName = "Alerta1" };

            var mockDatasetEMService = new Mock<IDatasetEMService>();
            mockDatasetEMService.Setup(x => x.GetDatasetEMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(datasetEM);

            var mockSondaEMService = new Mock<ISondaEMService>();
            mockSondaEMService.Setup(x => x.GetAlertById(7, It.IsAny<string>()))
                .ReturnsAsync(alert);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                mockSondaEMService.Object,
                null,
                null,
                null,
                null,
                mockDatasetEMService.Object,
                null,
                null
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.EventManager,
                EntityName = EntityName.Alert,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperand(operand, "user");

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal("Alerta1", ((AlertDto)list[0]).AlertName);
        }

        [Fact]
        public async Task GetDataForOperand_AM_Stock_ReturnsStocks()
        {
            // Arrange
            var datasetAM = new DatasetAM
            {
                Grupo_Stock = new List<DatasetStock>
                {
                    new DatasetStock { Id_Stock = 3 }
                }
            };
            var stock = new StockDto { Id = 3, Name = "Stock1" };

            var mockDatasetAMService = new Mock<IDatasetAmService>();
            mockDatasetAMService.Setup(x => x.GetDatasetAMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(datasetAM);

            var mockSondaAMService = new Mock<ISondaAMService>();
            mockSondaAMService.Setup(x => x.GetStockById(3, It.IsAny<string>()))
                .ReturnsAsync(stock);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                null,
                null,
                mockSondaAMService.Object,
                null,
                null,
                null,
                null,
                mockDatasetAMService.Object
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.AssetManager,
                EntityName = EntityName.Stock,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperand(operand, "user");

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(3, ((StockDto)list[0]).Id);
        }

        [Fact]
        public async Task GetDataForOperand_DatasetNotFound_ReturnsEmpty()
        {
            var mockDatasetService = new Mock<IDatasetService>();
            mockDatasetService.Setup(x => x.GetDatasetIMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((DatasetIM)null);

            var mockSondaIMService = new Mock<ISondaIMService>();

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null, null, mockSondaIMService.Object, null,
                mockDatasetService.Object, null, null, null, null);

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.InsightMonitor,
                EntityName = EntityName.Device,
                DatasetId = 1
            };

            var result = await service.GetDataForOperand(operand, "user");
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDataForOperand_EntityNotSupported_ThrowsNotSupported()
        {
            var datasetIM = new DatasetIM();
            var mockDatasetService = new Mock<IDatasetService>();
            mockDatasetService.Setup(x => x.GetDatasetIMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(datasetIM);

            var mockSondaIMService = new Mock<ISondaIMService>();
            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null, null, mockSondaIMService.Object, null,
                mockDatasetService.Object, null, null, null, null);

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.InsightMonitor,
                EntityName = (EntityName)999, 
                DatasetId = 1
            };

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.GetDataForOperand(operand, "user"));
        }

        [Fact]
        public async Task GetDataForOperand_ModuleNotSupported_ThrowsNotSupported()
        {
            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null, null, null, null, null, null, null, null, null);

            var operand = new JoinOperand
            {
                ModuleType = (ModuleType)999, 
                EntityName = EntityName.Device,
                DatasetId = 1
            };

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.GetDataForOperand(operand, "user"));
        }

        /* Tests sobre GetDataForOperandSinToken */

        [Fact]
        public async Task GetDataForOperandSinToken_IM_Device_ReturnsDevices()
        {
            // Arrange
            var datasetIM = new DatasetIM
            {
                Username = "visitante",
                DatasetDevices = new List<DatasetDevice>
                {
                    new DatasetDevice { Id_device = 13 }
                }
            };
            var device = new Device { Id = 13, Name = "Device1", Sensors = new List<Sensor>() };

            var mockDatasetService = new Mock<IDatasetService>();
            mockDatasetService.Setup(x => x.GetDatasetIMByIdForEditAsyncSinToken(It.IsAny<int>()))
                .ReturnsAsync(datasetIM);
            mockDatasetService.Setup(x => x.GetDatasetIMByIdAsync(It.IsAny<int>(), "visitante"))
                .ReturnsAsync(datasetIM);

            var mockSondaIMService = new Mock<ISondaIMService>();
            mockSondaIMService.Setup(x => x.GetDeviceById(13, It.IsAny<string>()))
                .ReturnsAsync(device);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                null,
                mockSondaIMService.Object,
                null,
                mockDatasetService.Object,
                null,
                null,
                null,
                null
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.InsightMonitor,
                EntityName = EntityName.Device,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperandSinToken(operand);

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(13, ((Device)list[0]).Id);
        }

        [Fact]
        public async Task GetDataForOperandSinToken_UM_News_ReturnsNews()
        {
            // Arrange
            var datasetUM = new DatasetUM
            {
                Username = "visitante",
                DatasetNews = new List<DatasetNews>
                {
                    new DatasetNews { Id_news = 12 }
                }
            };
            var news = new News { Id = 12, Title = "Test News" };

            var mockDatasetUMService = new Mock<IDatasetUMService>();
            mockDatasetUMService.Setup(x => x.GetDatasetUMByIdAsyncSinToken(It.IsAny<int>()))
                .ReturnsAsync(datasetUM);

            var mockSondaUMService = new Mock<ISondaUMService>();
            mockSondaUMService.Setup(x => x.GetNewsById(12, It.IsAny<string>()))
                .ReturnsAsync(news);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                null,
                null,
                null,
                null,
                mockDatasetUMService.Object,
                null,
                mockSondaUMService.Object,
                null
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.UrbanMonitor,
                EntityName = EntityName.New,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperandSinToken(operand);

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(12, ((News)list[0]).Id);
        }

        [Fact]
        public async Task GetDataForOperandSinToken_EM_Alert_ReturnsAlerts()
        {
            // Arrange
            var datasetEM = new DatasetEM
            {
                Username = "visitante",
                DatasetAlerts = new List<DatasetAlert>
                {
                    new DatasetAlert { Id_alert = 11 }
                }
            };
            var alert = new AlertDto { AlertName = "Alerta1" };

            var mockDatasetEMService = new Mock<IDatasetEMService>();
            mockDatasetEMService.Setup(x => x.GetDatasetEMByIdAsyncSinToken(It.IsAny<int>()))
                .ReturnsAsync(datasetEM);

            var mockSondaEMService = new Mock<ISondaEMService>();
            mockSondaEMService.Setup(x => x.GetAlertById(11, It.IsAny<string>()))
                .ReturnsAsync(alert);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                mockSondaEMService.Object,
                null,
                null,
                null,
                null,
                mockDatasetEMService.Object,
                null,
                null
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.EventManager,
                EntityName = EntityName.Alert,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperandSinToken(operand);

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal("Alerta1", ((AlertDto)list[0]).AlertName);
        }

        [Fact]
        public async Task GetDataForOperandSinToken_AM_Stock_ReturnsStocks()
        {
            // Arrange
            var datasetAM = new DatasetAM
            {
                Username = "visitante",
                Grupo_Stock = new List<DatasetStock>
                {
                    new DatasetStock { Id_Stock = 10 }
                }
            };
            var stock = new StockDto { Id = 10, Name = "Stock1" };

            var mockDatasetAMService = new Mock<IDatasetAmService>();
            mockDatasetAMService.Setup(x => x.GetDatasetAMByIdAsyncSinToken(It.IsAny<int>()))
                .ReturnsAsync(datasetAM);

            var mockSondaAMService = new Mock<ISondaAMService>();
            mockSondaAMService.Setup(x => x.GetStockById(10, It.IsAny<string>()))
                .ReturnsAsync(stock);

            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null,
                null,
                null,
                mockSondaAMService.Object,
                null,
                null,
                null,
                null,
                mockDatasetAMService.Object
            );

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.AssetManager,
                EntityName = EntityName.Stock,
                DatasetId = 1
            };

            // Act
            var result = await service.GetDataForOperandSinToken(operand);

            // Assert
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(10, ((StockDto)list[0]).Id);
        }

        [Fact]
        public async Task GetDataForOperandSinToken_DatasetNotFound_ReturnsEmpty()
        {
            var mockDatasetService = new Mock<IDatasetService>();
            mockDatasetService.Setup(x => x.GetDatasetIMByIdForEditAsyncSinToken(It.IsAny<int>()))
                .ReturnsAsync((DatasetIM)null);

            var mockSondaIMService = new Mock<ISondaIMService>();
            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null, null, mockSondaIMService.Object, null,
                mockDatasetService.Object, null, null, null, null);

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.InsightMonitor,
                EntityName = EntityName.Device,
                DatasetId = 1
            };

            var result = await service.GetDataForOperandSinToken(operand);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDataForOperandSinToken_EntityNotSupported_ThrowsNotSupported()
        {
            var datasetIM = new DatasetIM { Username = "visitante" };
            var mockDatasetService = new Mock<IDatasetService>();
            mockDatasetService.Setup(x => x.GetDatasetIMByIdForEditAsyncSinToken(It.IsAny<int>()))
                .ReturnsAsync(datasetIM);
            mockDatasetService.Setup(x => x.GetDatasetIMByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(datasetIM);

            var mockSondaIMService = new Mock<ISondaIMService>();
            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null, null, mockSondaIMService.Object, null,
                mockDatasetService.Object, null, null, null, null);

            var operand = new JoinOperand
            {
                ModuleType = ModuleType.InsightMonitor,
                EntityName = (EntityName)999, // Valor no soportado
                DatasetId = 1
            };

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.GetDataForOperandSinToken(operand));
        }

        [Fact]
        public async Task GetDataForOperandSinToken_ModuleNotSupported_ThrowsNotSupported()
        {
            var service = new ApiDataService(
                new Mock<IHttpClientFactory>().Object,
                null, null, null, null, null, null, null, null, null);

            var operand = new JoinOperand
            {
                ModuleType = (ModuleType)999, // Módulo no soportado
                EntityName = EntityName.Device,
                DatasetId = 1
            };

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.GetDataForOperandSinToken(operand));
        }
    }
}
