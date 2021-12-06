﻿using System.Text.Json;
namespace NetDaemon.Client.Common.HomeAssistant.Model;

public record HassServiceEventData
{
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = string.Empty;

    [JsonPropertyName("service")]
    public string Service { get; init; } = string.Empty;

    [JsonPropertyName("service_data")]
    public JsonElement? ServiceData { get; init; }

    public object? Data { get; set; }
}