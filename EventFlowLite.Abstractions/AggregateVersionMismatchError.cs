namespace EventFlowLite.Abstractions;

public class AggregateVersionMismatchError : Exception
{
    public AggregateVersionMismatchError(int expected, int actual)
        : base($"expected aggregate's version to be {expected}, got {actual} instead")
    {
    }
}