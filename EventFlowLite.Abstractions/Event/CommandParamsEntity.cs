using EventFlowLite.Abstractions.Command;

namespace EventFlowLite.Abstractions.Event;

public class CommandParamsEntity
{
    public CommandParamsEntity()
    {
    }

    public CommandParamsEntity(string commandId, string correlationId, int? expectedVersion,
        string? originatingIpAddress, string? originatingApplication, Dictionary<string, string>? metadata)
    {
        CommandId = commandId;
        CorrelationId = correlationId;
        ExpectedVersion = expectedVersion;
        OriginatingIpAddress = originatingIpAddress;
        OriginatingApplication = originatingApplication;
        Metadata = metadata;
    }

    public string CommandId { get; private set; }
    public string CorrelationId { get; private set; }

    public int? ExpectedVersion { get; private set; }

    public string? OriginatingIpAddress { get; private set; }
    public string? OriginatingApplication { get; private set; }

    public Dictionary<string, string>? Metadata { get; private set; }

    public static explicit operator CommandParamsEntity(CommandParams commandParams)
    {
        return new CommandParamsEntity(commandParams.CommandId, commandParams.CorrelationId,
            commandParams.ExpectedVersion,
            commandParams.OriginatingIpAddress, commandParams.OriginatingApplication, commandParams.Metadata);
    }
}