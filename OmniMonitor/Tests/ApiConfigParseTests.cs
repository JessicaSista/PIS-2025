using OmniMonitor.Server.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace QA.Tests
{
    public class ApiConfigParseTests
    {
        [Fact]
        public void ApiConfig_Deserialize_FromJson_WorksCorrectly()
        {
            var json = File.ReadAllText("ApiConfig.json");
            ApiConfig config = JsonSerializer.Deserialize<ApiConfig>(json);

            Assert.NotNull(config);
            Assert.NotNull(config.BaseUrl);
            Assert.NotNull(config.Credentials);

            // Prueba URLs
            Assert.False(string.IsNullOrWhiteSpace(config.BaseUrl.UrlIM));
            Assert.False(string.IsNullOrWhiteSpace(config.BaseUrl.UrlEM));
            Assert.False(string.IsNullOrWhiteSpace(config.BaseUrl.UrlUM));

            // Prueba credenciales IM
            Assert.NotNull(config.Credentials.CredentialsIM);
            Assert.False(string.IsNullOrWhiteSpace(config.Credentials.CredentialsIM.Email));
            Assert.False(string.IsNullOrWhiteSpace(config.Credentials.CredentialsIM.Password));

            // Prueba Endpoints IM
            Assert.NotNull(config.EndpointsIM);
            Assert.True(config.EndpointsIM.ContainsKey("Login"));
            Assert.Equal("/api/Account/Login", config.EndpointsIM["Login"]["Login"]);

            // Prueba EndpointsAM, EndpointsEM, EndpointsUM (aunque estén vacíos)
            Assert.NotNull(config.EndpointsAM);
            Assert.NotNull(config.EndpointsEM);
            Assert.NotNull(config.EndpointsUM);

            // Imprime algunos valores
            Console.WriteLine("UrlIM: " + config.BaseUrl.UrlIM);
            Console.WriteLine("Email IM: " + config.Credentials.CredentialsIM.Email);
            Console.WriteLine("Login Endpoint: " + config.EndpointsIM["Login"]["Login"]);

            // Puedes repetir para otras categorías si tienes datos de ejemplo
        }
    }
}