using System.Collections.Generic;
using System.Runtime.InteropServices;
using OneOf;
using Scraper.Lib.ValueObjects;
using Vogen;

using ParseResult = OneOf.OneOf<Scraper.Cli.OAuthLoginOptions, Scraper.Cli.ScrapNamespacesOptions, Scraper.Cli.RefreshOAuthTokenOptions, Scraper.Cli.CliOptionsParserError>;

namespace Scraper.Cli;

[StructLayout(LayoutKind.Auto)]
public record struct OAuthLoginOptions(
    AuthorizationCode AuthorizationCode,
    OAuthClientId ClientId,
    OAuthClientSecret ClientSecret);

public record struct ScrapNamespacesOptions;

public record struct RefreshOAuthTokenOptions;

[ValueObject<string>(conversions: Conversions.None)]
public readonly partial struct CliOptionsParserError { }

public static class CliOptionsParser
{
    public static ParseResult ParseArguments(string[] args)
    {
        if (args.Length < 1) return CliOptionsParserError.From("Not enough arguments!");

        var command = args[0];
        return command switch
        {
            "oauth" => ParseDoOAuthLogin(args).Match<ParseResult>(x => x, x => x),
            "refresh" => ParseRefreshOAuthToken(args).Match<ParseResult>(x => x, x => x),
            "namespaces" => ParseScrapNamespaces(args).Match<ParseResult>(x => x, x => x),
            _ => CliOptionsParserError.From("Unknown command"),
        };
    }

    private static OneOf<OAuthLoginOptions, CliOptionsParserError> ParseDoOAuthLogin(string[] args)
    {
        string? authorizationCode = null;
        string? clientId = null;
        string? clientSecret = null;

        for (var i = 1; i < args.Length; i++)
        {
            if (authorizationCode is null)
            {
                var res = GetNamedValue(args, i, "--authorization_code", out i);
                if (res.IsT1) return res.AsT1;
                if (res.TryPickT0(out authorizationCode, out _) && authorizationCode is not null) continue;
            }

            if (clientId is null)
            {
                var res = GetNamedValue(args, i, "--client_id", out i);
                if (res.IsT1) return res.AsT1;
                if (res.TryPickT0(out clientId, out _) && clientId is not null) continue;
            }

            if (clientSecret is null)
            {
                var res = GetNamedValue(args, i, "--client_secret", out i);
                if (res.IsT1) return res.AsT1;
                if (res.TryPickT0(out clientSecret, out _) && clientSecret is not null) continue;
            }
        }

        if (authorizationCode is null)
            return CliOptionsParserError.From("Missing required argument: \"--authorization_code\"");
        if (clientId is null)
            return CliOptionsParserError.From("Missing required argument: \"--client_id\"");
        if (clientSecret is null)
            return CliOptionsParserError.From("Missing required argument: \"--client_secret\"");

        return new OAuthLoginOptions(
            AuthorizationCode.From(authorizationCode),
            OAuthClientId.From(clientId),
            OAuthClientSecret.From(clientSecret)
        );
    }

    private static OneOf<ScrapNamespacesOptions, CliOptionsParserError> ParseScrapNamespaces(string[] args)
    {
        if (args.Length != 1)
            return CliOptionsParserError.From("Too many arguments!");
        return new ScrapNamespacesOptions();
    }

    private static OneOf<RefreshOAuthTokenOptions, CliOptionsParserError> ParseRefreshOAuthToken(string[] args)
    {
        if (args.Length != 1)
            return CliOptionsParserError.From("Too many arguments!");
        return new RefreshOAuthTokenOptions();
    }

    private static OneOf<string?, CliOptionsParserError> GetNamedValue(IReadOnlyList<string> args, int current, string name, out int next)
    {
        next = current;

        var currentValue = args[next++];
        if (!string.Equals(currentValue, name, System.StringComparison.OrdinalIgnoreCase))
            return null;

        if (args.Count <= next)
            return CliOptionsParserError.From($"Missing value for argument \"{name}\"");

        var nextValue = args[next++];
        return nextValue;
    }
}
