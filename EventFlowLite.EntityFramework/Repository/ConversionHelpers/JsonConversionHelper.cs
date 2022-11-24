using System.Linq.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace EventFlowLite.EntityFramework.Repository.ConversionHelpers;

public static class JsonConversionHelper<TProvider>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly JsonSerializerSettings EntityConversionJsonSettings = new()
    {
        Formatting = Formatting.None,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters = new List<JsonConverter>
        {
            new StringEnumConverter()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    public static Expression<Func<TProvider, string>> ConvertToProviderExpression => property =>
        JsonConvert.SerializeObject(property, EntityConversionJsonSettings);

    public static Expression<Func<string, TProvider>> ConvertFromProviderExpression => provider =>
        JsonConvert.DeserializeObject<TProvider>(provider, EntityConversionJsonSettings);
}