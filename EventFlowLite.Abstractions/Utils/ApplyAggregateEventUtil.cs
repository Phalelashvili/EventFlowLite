// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Reflection;
using EventFlowLite.Abstractions.Event;

namespace EventFlowLite.Abstractions.Utils;

public static class ApplyAggregateEventUtil
{
    public static IReadOnlyDictionary<Type, Action<TAggregate, IAggregateEvent<TAggregate, TId>>>
        GetApplyMethods<TAggregate, TId>()
        where TAggregate : class,
        IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var aggregateEventType = typeof(IAggregateEvent<TAggregate, TId>);

        var methods = typeof(TAggregate)
            .GetTypeInfo()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        var applyMethods = methods
            .Where(mi => string.Equals(mi.Name, "Apply", StringComparison.Ordinal) ||
                         mi.Name.EndsWith(".Apply", StringComparison.Ordinal));

        var applyMethodsWithEventParam = applyMethods.Where(mi =>
        {
            var parameters = mi.GetParameters();
            return
                parameters.Length == 1 &&
                aggregateEventType.GetTypeInfo().IsAssignableFrom(parameters[0].ParameterType);
        });

        return applyMethodsWithEventParam.ToDictionary(
            mi => mi.GetParameters()[0].ParameterType,
            mi => ReflectionHelper.CompileMethodInvocation<Action<TAggregate, IAggregateEvent<TAggregate, TId>>>(
                typeof(TAggregate), mi.Name, mi.GetParameters()[0].ParameterType));
    }
}