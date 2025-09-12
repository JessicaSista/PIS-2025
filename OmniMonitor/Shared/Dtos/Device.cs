using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// This file defines the C# representation of the JSON data from the Sonda API.

public class Device
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("source")]
    public Source? Source { get; set; }

    [JsonPropertyName("lastDataReceived")]
    public DateTime? LastDataReceived { get; set; }
}

public class Source
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
