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
            const string configPath = "ApiConfig.json";
            Assert.True(File.Exists(configPath), $"El archivo {configPath} no existe en el directorio de ejecución.");

            string json = File.ReadAllText(configPath);
            ApiConfig config = null;

            try
            {
                config = JsonSerializer.Deserialize<ApiConfig>(json);
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Error al deserializar el JSON: {ex.Message}");
            }

            Assert.NotNull(config);
            Assert.NotNull(config.BaseUrl);
            Assert.NotNull(config.Credentials);

            // Prueba URLs
            Assert.False(string.IsNullOrWhiteSpace(config.BaseUrl.UrlIM), "UrlIM está vacío o nulo");
            Assert.False(string.IsNullOrWhiteSpace(config.BaseUrl.UrlEM), "UrlEM está vacío o nulo");
            Assert.False(string.IsNullOrWhiteSpace(config.BaseUrl.UrlUM), "UrlUM está vacío o nulo");

            // Prueba credenciales IM
            Assert.NotNull(config.Credentials.CredentialsIM);
            Assert.False(string.IsNullOrWhiteSpace(config.Credentials.CredentialsIM.Email), "Email IM vacío");
            Assert.False(string.IsNullOrWhiteSpace(config.Credentials.CredentialsIM.Password), "Password IM vacío");

            // Prueba Endpoints IM
            Assert.NotNull(config.EndpointsIM);
            Assert.True(config.EndpointsIM.ContainsKey("Login"), "No existe la clave 'Login' en EndpointsIM");
            Assert.Equal("/api/Account/Login", config.EndpointsIM["Login"]["Login"]);

            // Prueba EndpointsAM, EndpointsEM, EndpointsUM (aunque estén vacíos)
            Assert.NotNull(config.EndpointsAM);
            Assert.NotNull(config.EndpointsEM);
            Assert.NotNull(config.EndpointsUM);

            // Imprime algunos valores para depuración
            Console.WriteLine("UrlIM: " + config.BaseUrl.UrlIM);
            Console.WriteLine("Email IM: " + config.Credentials.CredentialsIM.Email);
            Console.WriteLine("Login Endpoint: " + config.EndpointsIM["Login"]["Login"]);
        }
    }
}