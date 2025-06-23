using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Proxy.Models;

public record Crossing
{
    public enum SignalState
    {
        Off = 0,
        Walk = 1,
        Clear = 2,
        Stop = 3
    }

    public enum CallState
    {
        None = 0,
        Standard = 1,
        Extended = 2
    }

    public string Id { get; init; } = "";
    public string Description { get; init; } = "";
    public int Phase { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter ))]
    public SignalState Signal { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter ))]
    public CallState Calls { get; init; }
}
