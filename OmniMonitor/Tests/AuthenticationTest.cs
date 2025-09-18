using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Context;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Xunit;

namespace Tests
{
    public class AuthenticationTest
    {
        // Helper para crear un DbContext en Memoria para pruebas 
        private ApplicationDbContext CreateInMemoryDb(string dbName) {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }
        // Helper para crear un HttpClientFactory que devuelve respuestas simuladas 
        private IHttpClientFactory CreateHttpClientFactory(HttpResponseMessage fakeResponse) {
            var handlerMock = new Mock<HttpMessageHandler>();
            // Configura el mock para devolver la respuesta simulada
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(fakeResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                       .Returns(httpClient);

            return factoryMock.Object;
        }
        // Test GetUserTokenAsync: Lanza error si el usuario no existe 
        [Fact]
        public async Task GetUserTokenAsync_ShouldThrow_WhenUserDoesNotExist() {
            using var dbContext = CreateInMemoryDb("Db_UserNotExist");
            var factory = new Mock<IHttpClientFactory>().Object;
            var service = new SondaAuthService(dbContext, factory);
            // No agregamos usuario en la DB
            await Assert.ThrowsAsync<AuthenticationException>(() =>
                service.GetUserTokenAsync("ghost", "1234"));
        }
        // Test GetUserTokenAsync: Lanza error si la contraseña es incorrecta  
        [Fact]
        public async Task GetUserTokenAsync_ShouldThrow_WhenPasswordIsIncorrect() {
            using var dbContext = CreateInMemoryDb("Db_WrongPassword");
            // Agrega usuario de prueba
            var user = new User
            {
                Username = "test",
                Password = "correctpassword"
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            var factory = new Mock<IHttpClientFactory>().Object;
            var service = new SondaAuthService(dbContext, factory);
            // Contraseña incorrecta
            await Assert.ThrowsAsync<AuthenticationException>(() =>
                service.GetUserTokenAsync("test", "wrongpassword"));
        }
        // Test GetUserTokenAsync: Retorna token cacheado si es valido y no expirado
        [Fact]
        public async Task GetUserTokenAsync_ShouldReturnCachedToken_WhenValidAndNotExpired() {
            using var dbContext = CreateInMemoryDb("Db_CachedToken");
            // Agrega usuario de prueba con token valido
            var user = new User {
                Username = "test",
                Password = "1234",
                SondaToken = "CACHED_TOKEN",
                TokenExpiration = DateTime.UtcNow.AddMinutes(30)
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            var factory = new Mock<IHttpClientFactory>().Object;
            var service = new SondaAuthService(dbContext, factory);
            var token = await service.GetUserTokenAsync("test", "1234");
            Assert.Equal("CACHED_TOKEN", token);
        }
        // Test RefreshAndStoreTokenAsync: Maneja error de API
        [Fact]
        public async Task RefreshAndStoreTokenAsync_ShouldThrow_WhenApiReturnsError() {
            using var dbContext = CreateInMemoryDb("Db_ApiError");
            // Agrega usuario de prueba
            var user = new User { 
                Username = "test", 
                Password = "1234"
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            // Simula respuesta HTTP de error
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var factory = CreateHttpClientFactory(fakeResponse);
            var service = new SondaAuthService(dbContext, factory);
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.GetUserTokenAsync("test", "1234"));
        }
        // Test RefreshAndStoreTokenAsync: Maneja respuesta sin token
        [Fact]
        public async Task RefreshAndStoreTokenAsync_ShouldThrow_WhenApiReturnsNoToken() {
            using var dbContext = CreateInMemoryDb("Db_NoToken");
            // Agrega usario de prueba
            var user = new User { 
                Username = "test", 
                Password = "1234" 
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            // Simula respuesta HTTP sin token
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { }))
            };
            var factory = CreateHttpClientFactory(fakeResponse);
            var service = new SondaAuthService(dbContext, factory);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetUserTokenAsync("test", "1234"));
        }

        // Test GetUserTokenAsync: Refresca token si esta expirado
        [Fact]
        public async Task GetUserTokenAsync_ShouldRefreshToken_WhenTokenExpired() {
            using var dbContext = CreateInMemoryDb("Db_ExpiredToken");
            // Agrega usuario de prueba con token expirado
            var user = new User {
                Username = "test",
                Password = "1234",
                SondaToken = "OLD_TOKEN",
                TokenExpiration = DateTime.UtcNow.AddMinutes(-1)
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            // Simula respuesta HTTP de Sonda
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{ ""token"": ""NEW_TOKEN"", ""expiration"": ""2099-12-31T23:59:59Z"" }")
            };
            var factory = CreateHttpClientFactory(fakeResponse);
            var service = new SondaAuthService(dbContext, factory);
            var token = await service.GetUserTokenAsync("test", "1234");
            Assert.Equal("NEW_TOKEN", token);
            var updatedUser = await dbContext.Users.FirstAsync();
            Assert.Equal("NEW_TOKEN", updatedUser.SondaToken);
        }

        // Test RefreshAndStoreTokenAsync: Guarda token en DB
        [Fact]
        public async Task RefreshAndStoreTokenAsync_ShouldSaveTokenInDb() {
            // Configura DbContext en memoria
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            using var dbContext = new ApplicationDbContext(options);
            // Agrega usuario de prueba
            var user = new User {
                Username = "test",
                Password = "1234"
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            // Simular respuesta HTTP de Sonda
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{ ""token"": ""FAKE_TOKEN"", ""expiration"": ""2099-12-31T23:59:59Z"" }")
            };
            // Crea mock de HttpClientFactory para devolver respuesta simulada
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(fakeResponse);
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            // Configura el mock para devolver el HttpClient simulado
            factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>()))
                       .Returns(httpClient);
            var service = new SondaAuthService(dbContext, factoryMock.Object);
            var token = await service.GetUserTokenAsync("test", "1234");
            Assert.Equal("FAKE_TOKEN", token);
            var updatedUser = await dbContext.Users.FirstAsync();
            Assert.Equal("FAKE_TOKEN", updatedUser.SondaToken);
        }
        // Test GetUserTokenAsync: Verifica que se llama al endpoint correcto
        [Fact]
        public async Task GetUserTokenAsync_ShouldCallCorrectEndpoint() {
            using var dbContext = CreateInMemoryDb("Db_EndpointCheck");
            // Agrega usuario de prueba
            var user = new User { 
                Username = "test", 
                Password = "1234", 
                SondaToken = null, 
                TokenExpiration = null 
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            HttpRequestMessage? capturedRequest = null;
            var handlerMock = new Mock<HttpMessageHandler>();
            // Captura el request enviado
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(@"{ ""token"": ""TOKEN"", ""expiration"": ""2099-12-31T23:59:59Z"" }")
                });
            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var service = new SondaAuthService(dbContext, factoryMock.Object);
            await service.GetUserTokenAsync("test", "1234");
            Assert.NotNull(capturedRequest);
            Assert.Equal("https://sondasmartplatform.com/internal/IoTMonitor/api/Account/Login", capturedRequest.RequestUri.ToString());
        }

        // Test GetUserTokenAsync: Verifica que las credenciales se envian correctamente en el body
        [Fact]
        public async Task GetUserTokenAsync_ShouldSendCorrectCredentialsInBody()
        {
            using var dbContext = CreateInMemoryDb("Db_BodyCheck");
            // Agrega usuario de prueba
            var user = new User { 
                Username = "test", 
                Password = "1234", 
                SondaToken = null, 
                TokenExpiration = null 
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            HttpRequestMessage? capturedRequest = null;
            // Captura el request enviado
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(@"{ ""token"": ""TOKEN"", ""expiration"": ""2099-12-31T23:59:59Z"" }")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var service = new SondaAuthService(dbContext, factoryMock.Object);
            await service.GetUserTokenAsync("test", "1234");
            Assert.NotNull(capturedRequest);
            var body = await capturedRequest.Content.ReadAsStringAsync();
            Assert.Contains(@"""email"":""pis@pis.com""", body.Replace(" ", "").Replace("\n", ""));
            Assert.Contains(@"""password"":""PIS.sonda2025""", body.Replace(" ", "").Replace("\n", ""));
        }

        // Test GetUserTokenAsync: Maneja correctamente la expiracion en UTC
        [Fact]
        public async Task GetUserTokenAsync_ShouldHandleUtcExpirationCorrectly() {
            using var dbContext = CreateInMemoryDb("Db_Timezone");
            var user = new User { 
                Username = "test", 
                Password = "1234", 
                SondaToken = null, 
                TokenExpiration = null 
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            // Simula una expiración en UTC muy lejana
            var expirationUtc = DateTime.UtcNow.AddHours(2);
            var expirationString = expirationUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            // Simula respuesta HTTP de Sonda
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent($@"{{ ""token"": ""TOKEN"", ""expiration"": ""{expirationString}"" }}")
            };
            var factory = CreateHttpClientFactory(fakeResponse);
            var service = new SondaAuthService(dbContext, factory);
            await service.GetUserTokenAsync("test", "1234");
            var updatedUser = await dbContext.Users.FirstAsync();
            // Debe estar cerca de la fecha UTC simulada (no local)
            Assert.True(updatedUser.TokenExpiration.HasValue);
            Assert.InRange(updatedUser.TokenExpiration.Value, expirationUtc.AddMinutes(-1), expirationUtc.AddMinutes(1));
        }
        // Test GetUserTokenAsync: Verifica que es thread-safe
        [Fact]
        public async Task GetUserTokenAsync_ShouldBeThreadSafe() {
            using var dbContext = CreateInMemoryDb("Db_Concurrent");
            // Agrega usuario de prueba
            var user = new User { 
                Username = "test", 
                Password = "1234", 
                SondaToken = null, 
                TokenExpiration = null 
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            // Simula respuesta HTTP de Sonda
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(@"{ ""token"": ""TOKEN"", ""expiration"": ""2099-12-31T23:59:59Z"" }")
            };
            var factory = CreateHttpClientFactory(fakeResponse);
            var service = new SondaAuthService(dbContext, factory);
            // Lanza múltiples llamadas concurrentes
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => service.GetUserTokenAsync("test", "1234"))
                .ToArray();
            //  Espera todas las tareas
            await Task.WhenAll(tasks);
            var updatedUser = await dbContext.Users.FirstAsync();
            Assert.Equal("TOKEN", updatedUser.SondaToken);
            Assert.All(tasks, t => Assert.Equal("TOKEN", t.Result));
        }

        // Test GetUserTokenAsync: Refresca token si es null o expiracion es null
        [Fact]
        public async Task GetUserTokenAsync_ShouldRefresh_WhenTokenAndExpirationAreNull() {
            using var dbContext = CreateInMemoryDb("Db_NullFields");
            // Agrega usuario de prueba con token y expiracion null
            var user = new User { 
                Username = "test", 
                Password = "1234", 
                SondaToken = null, 
                TokenExpiration = null 
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            // Simula respuesta HTTP de Sonda
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(@"{ ""token"": ""TOKEN"", ""expiration"": ""2099-12-31T23:59:59Z"" }")
            };
            var factory = CreateHttpClientFactory(fakeResponse);
            var service = new SondaAuthService(dbContext, factory);
            var token = await service.GetUserTokenAsync("test", "1234");
            Assert.Equal("TOKEN", token);
            var updatedUser = await dbContext.Users.FirstAsync();
            Assert.Equal("TOKEN", updatedUser.SondaToken);
            Assert.True(updatedUser.TokenExpiration > DateTime.UtcNow);
        }
    }
}
