namespace NetDaemon.Client.Internal.HomeAssistant.Commands;
public record HassMessage : HassMessageBase
{
    [JsonPropertyName("event")]
    // [JsonConverter(typeof(HassEventConverter))]
    public HassEvent? Event { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    // public object? Result { get; set; }

    [JsonPropertyName("result")]
    public JsonElement? ResultElement { get; init; }

    [JsonPropertyName("success")]
    public bool? Success { get; init; }

    [JsonPropertyName("error")]
    public HassError? Error { get; init; }
}