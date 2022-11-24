namespace EventFlowLite.Abstractions;

public class AggregateDoesNotExistError : Exception
{
    public AggregateDoesNotExistError(Type type, object id) : base(
        $"Aggregate '{type.Name}' with id {id} does not exist")
    {
    }
}