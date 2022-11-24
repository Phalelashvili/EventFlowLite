using System.Reflection;

namespace EventFlowLite.EntityFramework.Extensions;

public static class GenericInheritanceUtil
{
    public static bool IsAssignableToGenericInterface(this Type type, Type targetInterfaceType)
    {
        if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == targetInterfaceType)
            return true;
            
        var implementedInterfaces = type.GetTypeInfo().ImplementedInterfaces;
        if (implementedInterfaces.Any(it => 
                it.GetTypeInfo().IsGenericType && it.GetGenericTypeDefinition() == targetInterfaceType))
            return true;
            
        var baseType = type.GetTypeInfo().BaseType;
        return baseType is not null && IsAssignableToGenericInterface(baseType, targetInterfaceType);
    }
}