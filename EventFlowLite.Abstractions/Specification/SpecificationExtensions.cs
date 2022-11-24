using EventFlowLite.Abstractions.Extensions;

namespace EventFlowLite.Abstractions.Specification;

public static class SpecificationExtensions
{
    public static void ThrowIfSatisfied<T>(this ISpecification<T> specification, T obj)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        if (specification.IsSatisfiedBy(obj))
            throw DomainError.With("Specification '{0}' should not be satisfied",
                specification.GetType().PrettyPrint());
    }

    public static void ThrowIfNotSatisfied<T>(this ISpecification<T> specification, T obj)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        var reason = specification.WhyIsNotSatisfiedBy(obj).ToList();
        if (reason.Any())
            throw DomainError.With(
                $"'{specification.GetType().Name}' is not satisfied because of {string.Join(" and ", reason)}");
    }
}

public static class SpecificationOperatorExtensions
{
    public static ISpecification<TEntity> And<TEntity>(
        this ISpecification<TEntity> specificationOne,
        ISpecification<TEntity> specificationTwo)
    {
        return new AndSpecification<TEntity>(
            specificationOne, specificationTwo);
    }

    public static ISpecification<TEntity> Or<TEntity>(
        this ISpecification<TEntity> specificationOne,
        ISpecification<TEntity> specificationTwo)
    {
        return new OrSpecification<TEntity>(
            specificationOne, specificationTwo);
    }

    public static ISpecification<TEntity> Not<TEntity>(
        this ISpecification<TEntity> specification)
    {
        return new NotSpecification<TEntity>(specification);
    }
}