using OmniMonitor.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QA.Tests
{
    public class ApiConfigParseTests
    {
        [Fact]
        public void ApiConfig_Deserialize_FromJson_WorksCorrectly()
        {
            
            var json = File.ReadAllText("ApiConfig.json");

            var config = JsonSerializer.Deserialize<ApiConfig>(json);

            Assert.NotNull(config);
            Assert.Equal("https://sondasmartplatform.com/internal/IoTMonitor", config.BaseUrl);
            Assert.Equal("pis@pis.com", config.Credentials.Email);
            Assert.Equal("PIS.sonda2025", config.Credentials.Password);
            Assert.Equal("/api/Action", config.Endpoints["Action"]["GetAll"]);
            Assert.Equal("/api/User/users", config.Endpoints["User"]["Users"]);
        }
    }
}

