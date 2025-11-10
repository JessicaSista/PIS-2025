using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace QA.Tests
{
    public class AuthenticationTest
    {
        private ApplicationDbContext CreateDbContext()
        {
            // Configuración real apuntando a la DB de test
            var inMemorySettings = new Dictionary<string, string?> {
            {"ConnectionStrings:DefaultConnection", "Server=localhost;Database=OmniMonitorTest;Trusted_Connection=True;TrustServerCertificate=True;"}
        };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var context = new ApplicationDbContext(configuration);

            // Limpiar o preparar datos de prueba
            context.Users.RemoveRange(context.Users.ToList());
            context.Users.Add(new User
            {
                Username = "testuser",
                Password = "password123"
            });
            context.SaveChanges();

            return context;
        }

        private IOptions<ApiConfig> CreateApiConfig()
        {
            var apiConfig = new ApiConfig
            {
                BaseUrl = new BaseUrlConfig 
                { 
                    UrlIM = "https://fakeapi.com", 
                    UrlUM = "https://fakeumapi.com", 
                    UrlAM = "https://fakeamapi.com", 
                    UrlEM = "https://fakeemapi.com" 
                },
                Credentials = new CredentialsConfig
                {
                    CredentialsIM = new CredentialDetails { Email = "email@test.com", Password = "pass" },
                    CredentialsAM = new CredentialDetails { Email = "emailam@test.com", Password = "passam" }, 
                    CredentialsEM = new CredentialDetails { Email = "emailem@test.com", Password = "passem" },
                    CredentialsUM = new CredentialDetails { Email = "emailum@test.com", Password = "passum" }  
                },
                EndpointsIM = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Login", new Dictionary<string, string> { { "Login", "/login" } } }
                },
                EndpointsUM = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Login", new Dictionary<string, string> { { "Login", "/login" } } }
                },
                EndpointsAM = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Login", new Dictionary<string, string> { { "Login", "/login" } } }
                },
                EndpointsEM = new Dictionary<string, Dictionary<string, string>>
                {
                    { "Login", new Dictionary<string, string> { { "Login", "/login" } } }
                }
            };
            return Options.Create(apiConfig);
        }

        private IHttpClientFactory CreateHttpClientFactoryMock(HttpResponseMessage responseMessage)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            var client = new HttpClient(handlerMock.Object);

            var httpFactoryMock = new Mock<IHttpClientFactory>();
            httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            return httpFactoryMock.Object;
        }
        // Tests sobre Authentication sobre el modulo IM
        [Fact]
        public async Task GetUserTokenIMAsync_ReturnsToken_WhenValidUser()
        {
            // Arrange
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "fake-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };

            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);

            var service = new SondaAuthService(context, httpFactory, apiConfig);

            // Act
            var token = await service.GetUserTokenIMAsync("testuser", "password123");

            // Assert
            Assert.Equal("fake-token", token);

            var userInDb = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("fake-token", userInDb.SondaTokenIM);
        }

        [Fact]
        public async Task GetUserTokenIMAsync_ThrowsAuthenticationException_WhenInvalidPassword() {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenIMAsync("testuser", "wrongpassword"));
        }

        [Fact]
        public async Task GetUserTokenIMAsync_ThrowsAuthenticationException_WhenInvalidUser() {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenIMAsync("wronguser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenIMAsync_RefreshesToken_WhenExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            // Configura usuario con token expirado
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenIM = "old-token";
            user.TokenExpirationIM = DateTime.UtcNow.AddMinutes(-10);
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "new-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenIMAsync("testuser", "password123");
            Assert.Equal("new-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("new-token", updatedUser.SondaTokenIM);
        }

        [Fact]
        public async Task GetUserTokenIMAsync_ThrowsHttpRequestException_WhenApiError()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenIM = null;
            user.TokenExpirationIM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetUserTokenIMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUSerTokenIMAsync_ThrowsInvalidOperationException_WhenNoTokenInResponse()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenIM = null;
            user.TokenExpirationIM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserTokenIMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenIMAsync_Refreshes_WhenTokenExpiresExactlyInFiveMinutes()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenIM = null;
            user.TokenExpirationIM = DateTime.UtcNow.AddMinutes(5);
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "refreshed-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenIMAsync("testuser", "password123");
            Assert.Equal("refreshed-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("refreshed-token", updatedUser.SondaTokenIM);
        }

        [Fact]
        public async Task GetUserTokenIMAsync_ReturnCachedToken_WhenValidAndNotExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenIM = "cached-token";
            user.TokenExpirationIM = DateTime.UtcNow.AddMinutes(10);
            await context.SaveChangesAsync();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenIMAsync("testuser", "password123");
            Assert.Equal("cached-token", token);
        }

        [Fact]
        public async Task GetUserTokenIMAsync_Refreshes_WhenTokenAndExpirationAreNull()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenIM = null;
            user.TokenExpirationIM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "newly-refreshed-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenIMAsync("testuser", "password123");
            Assert.Equal("newly-refreshed-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("newly-refreshed-token", updatedUser.SondaTokenIM);
            Assert.True(updatedUser.TokenExpirationIM > DateTime.UtcNow);
        }
        // Tests sobre Authentication sobre el modulo UM
        [Fact]
        public async Task GetUserTokenUMAsync_ReturnToken_WhenValidUser()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "um-fake-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenUMAsync("testuser", "password123");
            Assert.Equal("um-fake-token", token);
        }

        [Fact]
        public async Task GetUserTokenUMAsync_ThrowsAuthenticationException_WhenInvalidPassword()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenUMAsync("testuser", "wrongpassword"));
        }

        [Fact]
        public async Task GetUserTokenUMAsync_ThrowsAuthenticationException_WhenInvalidUser()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenUMAsync("wronguser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenUMAsync_RefreshesToken_WhenExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenUM = "old-um-token";
            user.TokenExpirationUM = DateTime.UtcNow.AddMinutes(-10);
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "new-um-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenUMAsync("testuser", "password123");
            Assert.Equal("new-um-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("new-um-token", updatedUser.SondaTokenUM);
        }

        [Fact]
        public async Task GetUserTokenUMAsync_ThrowsHttpRequestException_WhenApiError()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenUM = null;
            user.TokenExpirationUM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetUserTokenUMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenUMAsync_ThrowsInvalidOperationException_WhenNoTokenInResponse()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenUM = null;
            user.TokenExpirationUM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserTokenUMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenUMAsync_Refreshes_WhenTokenAndExpirationAreNull()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenUM = null;
            user.TokenExpirationUM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "newly-refreshed-um-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenUMAsync("testuser", "password123");
            Assert.Equal("newly-refreshed-um-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("newly-refreshed-um-token", updatedUser.SondaTokenUM);
            Assert.True(updatedUser.TokenExpirationUM > DateTime.UtcNow);
        }

        [Fact]
        public async Task GetUserTokenUMAsync_ReturnCachedToken_WhenValidAndNotExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenUM = "cached-um-token";
            user.TokenExpirationUM = DateTime.UtcNow.AddMinutes(10);
            await context.SaveChangesAsync();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenUMAsync("testuser", "password123");
            Assert.Equal("cached-um-token", token);
        }

        // Tests sobre Authentication sobre el modulo AM
        [Fact]
        public async Task GetUserTokenAMAsync_ReturnToken_WhenValidUser()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "am-fake-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenAMAsync("testuser", "password123");
            Assert.Equal("am-fake-token", token);
        }

        [Fact]
        public async Task GetUserTokenAMAsync_ThrowsAuthenticationException_WhenInvalidPassword()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenAMAsync("testuser", "wrongpassword"));
        }

        [Fact]
        public async Task GetUserTokenAMAsync_ThrowsAuthenticationException_WhenInvalidUser()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenAMAsync("wronguser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenAMAsync_RefreshesToken_WhenExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenAM = "old-am-token";
            user.TokenExpirationAM = DateTime.UtcNow.AddMinutes(-10);
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "new-am-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenAMAsync("testuser", "password123");
            Assert.Equal("new-am-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("new-am-token", updatedUser.SondaTokenAM);
        }

        [Fact]
        public async Task GetUserTokenAMAsync_ThrowsHttpRequestException_WhenApiError()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");  
            user.SondaTokenAM = null;
            user.TokenExpirationAM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetUserTokenAMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenAMAsync_ThrowsInvalidOperationException_WhenNoTokenInResponse()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenAM = null;
            user.TokenExpirationAM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserTokenAMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenAMAsync_Refreshes_WhenTokenAndExpirationAreNull()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenAM = null;
            user.TokenExpirationAM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "newly-refreshed-am-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenAMAsync("testuser", "password123");
            Assert.Equal("newly-refreshed-am-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("newly-refreshed-am-token", updatedUser.SondaTokenAM);
            Assert.True(updatedUser.TokenExpirationAM > DateTime.UtcNow);
        }

        [Fact]
        public async Task GetUserTokenAMAsyc_ReturnCachedToken_WhenValidAndNotExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenAM = "cached-am-token";
            user.TokenExpirationAM = DateTime.UtcNow.AddMinutes(10);
            await context.SaveChangesAsync();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenAMAsync("testuser", "password123");
            Assert.Equal("cached-am-token", token);
        }

        // Tests sobre Authentication sobre el modulo EM

        [Fact]
        public async Task GetUserTokenEMAsync_ReturnToken_WhenValidUser()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "em-fake-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenEMAsync("testuser", "password123");
            Assert.Equal("em-fake-token", token);
        }

        [Fact]
        public async Task GetUserTokenEMAsync_ThrowsAuthenticationException_WhenInvalidPassword()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenEMAsync("testuser", "wrongpassword"));
        }

        [Fact]
        public async Task GetUserTokenEMAsync_ThrowsAuthenticationException_WhenInvalidUser()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<AuthenticationException>(() => service.GetUserTokenEMAsync("wronguser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenEMAsync_RefreshesToken_WhenExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenEM = "old-em-token";
            user.TokenExpirationEM = DateTime.UtcNow.AddMinutes(-10);
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "new-em-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenEMAsync("testuser", "password123");
            Assert.Equal("new-em-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("new-em-token", updatedUser.SondaTokenEM);
        }

        [Fact]
        public async Task GetUserTokenEMAsync_ThrowsHttpRequestException_WhenApiError()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenEM = null;
            user.TokenExpirationEM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetUserTokenEMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenEMAsync_ThrowsInvalidOperationException_WhenNoTokenInResponse()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenEM = null;
            user.TokenExpirationEM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserTokenEMAsync("testuser", "password123"));
        }

        [Fact]
        public async Task GetUserTokenEMAsync_Refreshes_WhenTokenAndExpirationAreNull()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenEM = null;
            user.TokenExpirationEM = null;
            await context.SaveChangesAsync();
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new SondaLoginResponse
                    {
                        Token = "newly-refreshed-em-token",
                        Expiration = DateTime.UtcNow.AddHours(1)
                    })
                )
            };
            var httpFactory = CreateHttpClientFactoryMock(fakeResponse);
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenEMAsync("testuser", "password123");
            Assert.Equal("newly-refreshed-em-token", token);
            var updatedUser = await context.Users.FirstAsync(u => u.Username == "testuser");
            Assert.Equal("newly-refreshed-em-token", updatedUser.SondaTokenEM);
            Assert.True(updatedUser.TokenExpirationEM > DateTime.UtcNow);
        }

        [Fact]
        public async Task GetUserTokenEMAsync_ReturnCachedToken_WhenValidAndNotExpired()
        {
            var context = CreateDbContext();
            var apiConfig = CreateApiConfig();
            var user = await context.Users.FirstAsync(u => u.Username == "testuser");
            user.SondaTokenEM = "cached-em-token";
            user.TokenExpirationEM = DateTime.UtcNow.AddMinutes(10);
            await context.SaveChangesAsync();
            var httpFactory = CreateHttpClientFactoryMock(new HttpResponseMessage(HttpStatusCode.OK));
            var service = new SondaAuthService(context, httpFactory, apiConfig);
            var token = await service.GetUserTokenEMAsync("testuser", "password123");
            Assert.Equal("cached-em-token", token);
        }


        public class SondaLoginResponse
        {
            public string Token { get; set; }
            public DateTime Expiration { get; set; }
        }
    }
}

