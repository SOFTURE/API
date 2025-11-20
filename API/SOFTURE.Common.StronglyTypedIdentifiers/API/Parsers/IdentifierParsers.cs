using FastEndpoints;
using Microsoft.Extensions.Primitives;
using SOFTURE.Language.Common;

namespace SOFTURE.Common.StronglyTypedIdentifiers.API.Parsers;

public static class IdentifierParsers
{
    public static ParseResult GuidParser<TIdentifier>(StringValues input)
        where TIdentifier : IIdentifier
    {
        var success = Guid.TryParse(input?.ToString(), out var result);
        
        var identifier = (TIdentifier)Activator.CreateInstance(typeof(TIdentifier), result)!;
        
        return new ParseResult(success, identifier);
    }
    
    public static ParseResult NumberParser<TIdentifier>(StringValues input)
        where TIdentifier : IIdentifier
    {
        var success = int.TryParse(input?.ToString(), out var result);
        
        var identifier = (TIdentifier)Activator.CreateInstance(typeof(TIdentifier), result)!;
        
        return new ParseResult(success, identifier);
    }
}