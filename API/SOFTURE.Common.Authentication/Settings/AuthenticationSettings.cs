namespace SOFTURE.Common.Authentication.Settings;

public sealed class AuthenticationSettings
{
#if NET8_0_OR_GREATER
    public required string JwtSecret { get; init; }
    public required string ValidAudience { get; init; }
    public required string ValidIssuer { get; init; }
#endif

#if NET6_0
    public string JwtSecret { get; init; } = null!;
    public string ValidAudience { get; init; } = null!;
    public string ValidIssuer { get; init; } = null!;
#endif
}