using System;
using System.Collections.Generic;
using System.Text.Json;
using OneOf;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Services;

public static class NamespaceScraper
{
    /// <summary>
    /// Finds the namespace mappings inside an HTML text.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static OneOf<IDictionary<CatalogNamespace, UrlSlug>, NamespaceScraperError> GetNamespacesFromHtmlText(string input)
    {
        var span = input.AsSpan();

        if (!span.IndexOfValue("window.__epic_client_state", out var clientStateIndex, out var error))
            return error;

        span = span[clientStateIndex..];

        if (!span.IndexOfValue('{', out var stateStartIndex, out error))
            return error;

        if (!span.IndexOfValue("};", out var stateEndIndex, out error))
            return error;

        span = span.Slice(stateStartIndex, stateEndIndex - stateStartIndex + 1);

        if (!span.IndexOfValue("productInstall", out var productInstallIndex, out error))
            return error;

        span = span[productInstallIndex..];

        if (!span.IndexOfValue('{', out var productInstallStartIndex, out error))
            return error;

        span = span[productInstallStartIndex..];

        if (!span.IndexOfValue("latestValue", out var latestValueIndex, out error))
            return error;

        span = span[latestValueIndex..];

        if (!span.IndexOfValue('{', out var latestValueStartIndex, out error))
            return error;

        if (!span.IndexOfValue('}', out var latestValueEndIndex, out error))
            return error;

        span = span.Slice(latestValueStartIndex, latestValueEndIndex - latestValueStartIndex + 1);

        var result = JsonSerializer.Deserialize<IDictionary<CatalogNamespace, UrlSlug>>(span);
        if (result is null)
            return NamespaceScraperError.From("Unable to deserialize the namespace mappings, result is null!");

        return OneOf<IDictionary<CatalogNamespace, UrlSlug>, NamespaceScraperError>.FromT0(result);
    }

    private static bool IndexOfValue(this ReadOnlySpan<char> span,
        ReadOnlySpan<char> value,
        out int index,
        out NamespaceScraperError error,
        StringComparison comparison = StringComparison.Ordinal)
    {
        error = NamespaceScraperError.From(string.Empty);
        index = span.IndexOf(value, comparison);
        if (index != -1) return true;

        error = NamespaceScraperError.From($"Found no value \"{value.ToString()}\" in document!");
        return false;

    }

    private static bool IndexOfValue(
        this ReadOnlySpan<char> span,
        char value,
        out int index,
        out NamespaceScraperError error)
    {
        error = NamespaceScraperError.From(string.Empty);
        index = span.IndexOf(value);
        if (index != -1) return true;

        error = NamespaceScraperError.From($"Found no value \"{value.ToString()}\" in document!");
        return false;
    }
}
