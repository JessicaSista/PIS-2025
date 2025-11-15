using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OmniMonitor.Shared.Dtos.AM;

namespace OmniMonitor.Shared.Dtos
{
    public class BundleDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty; // minLength: 1

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty; // minLength: 1

        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }

        [JsonPropertyName("tenantId")]
        public int TenantId { get; set; }

        [JsonPropertyName("assetTypesIds")]
        public List<int>? AssetTypesIds { get; set; }

        [JsonPropertyName("brandsIds")]
        public List<int>? BrandsIds { get; set; }

        [JsonPropertyName("modelsIds")]
        public List<int>? ModelsIds { get; set; }

        [JsonPropertyName("responsiblesIds")]
        public List<int>? ResponsiblesIds { get; set; }

        [JsonPropertyName("providersIds")]
        public List<int>? ProvidersIds { get; set; }

        [JsonPropertyName("statesIds")]
        public List<int>? StatesIds { get; set; }

        [JsonPropertyName("usersIds")]
        public List<string>? UsersIds { get; set; }

        [JsonPropertyName("rolesIds")]
        public List<string>? RolesIds { get; set; }

        [JsonPropertyName("userRoleDtos")]
        public List<UserRoleBundleDto>? UserRoleDtos { get; set; }

        [JsonPropertyName("taskTypeDtos")]
        public List<TaskTypeDto>? TaskTypeDtos { get; set; }

        [JsonPropertyName("usersToNofityTasksDtos")]
        public List<UserNotifyTaskDto>? UsersToNofityTasksDtos { get; set; }

        public override string ToString() => Name;
    }
}
