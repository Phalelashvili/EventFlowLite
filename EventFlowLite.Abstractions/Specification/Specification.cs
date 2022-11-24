namespace EventFlowLite.Abstractions.Specification;

public abstract class Specification<T> : ISpecification<T>
{
    public bool IsSatisfiedBy(T obj)
    {
        return IsNotSatisfiedBy(obj) is false;
    }

    public bool IsNotSatisfiedBy(T obj)
    {
        return IsNotSatisfiedBecause(obj).Any();
    }

    public IEnumerable<string> WhyIsNotSatisfiedBy(T obj)
    {
        return IsNotSatisfiedBecause(obj);
    }

    protected abstract IEnumerable<string> IsNotSatisfiedBecause(T obj);
}