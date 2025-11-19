using System.Text.Json;
using System.Text.Json.Serialization;
using SOFTURE.Language.Common;

namespace SOFTURE.Common.StronglyTypedIdentifiers.API.Converters;

public class StronglyTypedIdJsonConverter<TId, TValue> : JsonConverter<TId>
    where TId : IIdentifier
    where TValue : notnull
{
    private readonly Type _identifierBaseGenericType = typeof(IdentifierBase<>);
    
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
        if (value is null)
            throw new JsonException($"Cannot deserialize null value to {typeof(TId).Name}");
            
        return (TId)Activator.CreateInstance(typeToConvert, value)!;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        var baseType = typeof(TId).BaseType;
        if (baseType?.IsGenericType != true || baseType.GetGenericTypeDefinition() != _identifierBaseGenericType)
            throw new InvalidOperationException($"Type '{typeof(TId).Name}' does not inherit from IdentifierBase<T>");
            
        var valueType = baseType.GetGenericArguments()[0];
        var propertyInfo = typeof(TId).GetProperties().FirstOrDefault(p => p.PropertyType == valueType && p.CanRead)
            ?? throw new InvalidOperationException($"Type '{typeof(TId).Name}' does not have a readable property of type '{valueType.Name}'");
            
        var underlyingValue = propertyInfo.GetValue(value);
        
        JsonSerializer.Serialize(writer, underlyingValue, options);
    }
}