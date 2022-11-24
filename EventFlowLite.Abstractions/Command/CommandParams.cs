using System.Text.Json.Serialization;

namespace EventFlowLite.Abstractions.Command;

public class CommandParams
{
    [JsonConstructor]
    public CommandParams(string commandId, string? correlationId, int? expectedVersion,
        string? originatingIpAddress, string? originatingApplication, Dictionary<string, string>? metadata)
    {
        if (string.IsNullOrEmpty(commandId))
            throw new ArgumentOutOfRangeException(nameof(commandId));
        CommandId = commandId;
        CorrelationId = string.IsNullOrEmpty(correlationId) ? commandId : correlationId;
        ExpectedVersion = expectedVersion;
        OriginatingIpAddress = originatingIpAddress;
        OriginatingApplication = originatingApplication;
        Metadata = metadata;
    }

    public string CommandId { get; }

    public string CorrelationId { get; }

    public int? ExpectedVersion { get; }

    public string? OriginatingIpAddress { get; }
    public string? OriginatingApplication { get; }

    public Dictionary<string, string>? Metadata { get; private set; }

    public CommandParams WithMetadata(string key, string value)
    {
        Metadata ??= new Dictionary<string, string>();
        Metadata.Add(key, value);
        return this;
    }
}