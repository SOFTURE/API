using FastEndpoints;
using Microsoft.Extensions.Primitives;
using SOFTURE.Common.StronglyTypedIdentifiers.API.Parsers;
using SOFTURE.Language.Common;

namespace SOFTURE.Common.StronglyTypedIdentifiers.API.Extensions;

public static class ParserExtensions
{
    private const string ValueParserForMethodName = "ValueParserFor";
    
    public static void RegisterIdentifierParsers<TLanguageAssemblyMarker>(this Config config)
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
                config.RegisterParser(identifierType, nameof(IdentifierParsers.GuidParser));
            }
            else if (valueType == typeof(int) || valueType == typeof(long))
            {
                config.RegisterParser(identifierType, nameof(IdentifierParsers.NumberParser));
            }
        }
    }
    
    private static void RegisterParser(this Config config, Type identifierType, string parserMethodName)
    {
        var method = typeof(IdentifierParsers).GetMethod(parserMethodName)?.MakeGenericMethod(identifierType);
        var delegateType = typeof(Func<,>).MakeGenericType(typeof(StringValues), typeof(ParseResult));
        var parserDelegate = Delegate.CreateDelegate(delegateType, method!);

        var bindingMethod = config.Binding.GetType().GetMethod(ValueParserForMethodName, [delegateType])
            ?.MakeGenericMethod(identifierType);
        
        bindingMethod?.Invoke(config.Binding, [parserDelegate]);
    }
}