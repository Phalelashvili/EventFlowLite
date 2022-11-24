namespace EventFlowLite.Abstractions.Specification;

public interface ISpecification<in TEntity>
{
    bool IsSatisfiedBy(TEntity entity);

    IEnumerable<string> WhyIsNotSatisfiedBy(TEntity entity);
}