using NJsonSchema;
using NJsonSchema.Generation;
using SOFTURE.Language.Common;

namespace SOFTURE.Common.StronglyTypedIdentifiers.API.SchemaProcessors;

public sealed class StronglyTypedIdSchemaProcessor : ISchemaProcessor
{
    private static readonly Type IdentifierBaseGenericType = typeof(IdentifierBase<>);
    
    private const string UuidFormat = "uuid";
    private const string Int32Format = "int32";
    private const string Int64Format = "int64";

    public void Process(SchemaProcessorContext context)
    {
        var type = context.ContextualType.Type;
        
        if (!IsStronglyTypedId(type))
            return;

        var underlyingType = GetUnderlyingType(type);
        if (underlyingType == null)
            return;

        var schema = CreateSchemaForUnderlyingType(underlyingType);
        context.Schema.Type = schema.Type;
        context.Schema.Format = schema.Format;
        context.Schema.Properties.Clear();
        context.Schema.AllOf.Clear();
        context.Schema.AnyOf.Clear();
        context.Schema.OneOf.Clear();
    }

    private static bool IsStronglyTypedId(Type type)
    {
        if (type.IsInterface || type.IsAbstract)
            return false;

        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var genericTypeDef = baseType.GetGenericTypeDefinition();
                if (genericTypeDef == IdentifierBaseGenericType)
                {
                    return true;
                }
            }
            baseType = baseType.BaseType;
        }
        
        return typeof(IIdentifier).IsAssignableFrom(type);
    }

    private static Type? GetUnderlyingType(Type stronglyTypedIdType)
    {
        var baseType = stronglyTypedIdType.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var args = baseType.GetGenericArguments();
                if (args.Length > 0)
                    return args[0];
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    private static JsonSchema CreateSchemaForUnderlyingType(Type underlyingType)
    {
        if (underlyingType == typeof(Guid))
        {
            return new JsonSchema
            {
                Type = JsonObjectType.String,
                Format = UuidFormat
            };
        }
        
        if (underlyingType == typeof(int))
        {
            return new JsonSchema
            {
                Type = JsonObjectType.Integer,
                Format = Int32Format
            };
        }
        
        if (underlyingType == typeof(long))
        {
            return new JsonSchema
            {
                Type = JsonObjectType.Integer,
                Format = Int64Format
            };
        }

        return new JsonSchema
        {
            Type = JsonObjectType.String
        };
    }
}