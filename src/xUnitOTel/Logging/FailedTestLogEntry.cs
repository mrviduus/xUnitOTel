using System.Text.Json.Serialization;

namespace xUnitOTel.Logging;

public class FailedTestLogEntry
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("exception")]
    public string? Exception { get; set; }
}
