using System.Text.Json;
using SOFTURE.Common.StronglyTypedIdentifiers.API.Converters;
using SOFTURE.Language.Common;

namespace SOFTURE.Common.StronglyTypedIdentifiers.API.Extensions;

public static class JsonExtensions
{
    public static void RegisterStronglyTypedIdConverters<TLanguageAssemblyMarker>(this JsonSerializerOptions options)
        where TLanguageAssemblyMarker : IAssemblyMarker
    {
        var identifiersAssembly = typeof(TLanguageAssemblyMarker).Assembly;

        var identifierTypes = identifiersAssembly.GetTypes()
            .Where(t => t.IsClass && typeof(IIdentifier).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        foreach (var identifierType in identifierTypes)
        {
            var valueType = identifierType.BaseType?.GetGenericArguments().FirstOrDefault();
            if (valueType == typeof(Guid))
            {
                RegisterConverter(options, identifierType, typeof(Guid));
            }
            else if (valueType == typeof(int))
            {
                RegisterConverter(options, identifierType, typeof(int));
            }
            else if (valueType == typeof(long))
            {
                RegisterConverter(options, identifierType, typeof(long));
            }
        }
    }

    private static void RegisterConverter(JsonSerializerOptions options, Type identifierType, Type valueType)
    {
        var converterType = typeof(StronglyTypedIdJsonConverter<,>).MakeGenericType(identifierType, valueType);
        var converter = Activator.CreateInstance(converterType);
        
        if (converter != null)
        {
            options.Converters.Add((System.Text.Json.Serialization.JsonConverter)converter);
        }
    }
}