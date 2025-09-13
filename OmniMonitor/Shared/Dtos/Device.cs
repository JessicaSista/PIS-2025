using Microsoft.VisualBasic;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// This file defines the C# representation of the JSON data from the Sonda API.

public class Device
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("layerId")]
    public int? LayerId { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("xcoordinate")]
    public int? XCoordinate { get; set; }

    [JsonPropertyName("ycoordinate")]
    public int? YCoordinate { get; set; }

    [JsonPropertyName("sourceId")]
    public int? SourceId { get; set; }

    [JsonPropertyName("source")]
    public Source? Source { get; set; }

    [JsonPropertyName("active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("sectorId")]
    public int? SectorId { get; set; }

    [JsonPropertyName("integrations")]
    public List<Integration>? Integrations { get; set; }

    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    [JsonPropertyName("tenantId")]
    public int? TenantId { get; set; }

    [JsonPropertyName("sensors")]
    public List<Sensor>? Sensors { get; set; }

    [JsonPropertyName("deviceActions")]
    public List<DeviceAction>? DeviceActions { get; set; }

    [JsonPropertyName("groups")]
    public List<DeviceGroup>? Groups { get; set; }

    [JsonPropertyName("lastDataReceived")]
    public DateTime? LastDataReceived { get; set; }

    [JsonPropertyName("sendConfigurations")]
    public List<SendConfiguration>? SendConfigurations { get; set; }
}
