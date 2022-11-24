using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventFlowLite.EntityFramework.Repository.ConversionHelpers;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TProperty> HasJsonConversion<TProperty>(this PropertyBuilder<TProperty> builder)
    {
        return builder
            .HasConversion(
                JsonConversionHelper<TProperty>.ConvertToProviderExpression,
                JsonConversionHelper<TProperty>.ConvertFromProviderExpression);
    }
}