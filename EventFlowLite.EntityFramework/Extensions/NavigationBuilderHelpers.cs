using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventFlowLite.EntityFramework.Extensions;

internal static class NavigationBuilderHelpers
{
    public static void BuildPascalCaseNameNavigation<TEntity, TDependentEntity>(
        OwnedNavigationBuilder<TEntity, TDependentEntity> builder, string prefix)
        where TEntity : class
        where TDependentEntity : class
    {
        var ownedObjType =
            builder.GetType().GenericTypeArguments.Last(); // can i get argument by parameter name instead?
        foreach (var propertyInfo in ownedObjType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var columnName = prefix + propertyInfo.Name;
            builder.Property(propertyInfo.Name).HasColumnName(columnName);
        }
    }
}