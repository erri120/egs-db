using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OneOf;
using Scraper.Lib.Models;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Services;

/// <summary>
/// Wrapper for the public Api of the Epic Games Store
/// </summary>
[PublicAPI]
public class ApiWrapper
{
    public const string CatalogFormatUrl = "https://catalog-public-service-prod.ol.epicgames.com/catalog/api/shared/namespace/{0}/items";

    private readonly ILogger<ApiWrapper> _logger;
    private readonly HttpClient _client;
    private readonly RateLimiter _rateLimiter;

    public ApiWrapper(ILogger<ApiWrapper> logger, HttpMessageHandler httpMessageHandler, RateLimiter rateLimiter)
    {
        _logger = logger;
        _client = new HttpClient(httpMessageHandler);
        _rateLimiter = rateLimiter;
    }

    /// <summary>
    /// Asynchronously enumerates all items in a catalog namespace.
    /// </summary>
    public async IAsyncEnumerable<OneOf<CatalogNamespaceEnumerationResult.Element, ApiError>> EnumerateCatalogNamespaceAsync(
        OAuthToken oAuthToken,
        CatalogNamespace catalogNamespace,
        int itemsPerPage = 500,
        string countryCode = "US",
        string locale = "en-US",
        bool includeDLCDetails = true,
        bool includeMainGameDetails = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = string.Format(CultureInfo.InvariantCulture, CatalogFormatUrl, catalogNamespace.Value);

        var total = int.MaxValue;
        var current = 0;
        while (current < total)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var lease = await _rateLimiter
                .AcquireAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var res = await GetCatalogNamespaceItems(
                    url,
                    oAuthToken,
                    current,
                    itemsPerPage,
                    countryCode,
                    locale,
                    includeDLCDetails,
                    includeMainGameDetails,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!res.TryPickT0(out var enumerationResult, out var apiError))
            {
                yield return apiError;
                yield break;
            }

            total = enumerationResult.Paging.Total;
            current += enumerationResult.Paging.Count;

            foreach (var enumerationResultElement in enumerationResult.Elements)
            {
                yield return enumerationResultElement;
            }

            _logger.LogInformation("Status for \"{Namespace}\": {Current}/{Total}", catalogNamespace, current, total);
        }
    }

    internal async ValueTask<OneOf<CatalogNamespaceEnumerationResult, ApiError>> GetCatalogNamespaceItems(
        string url,
        OAuthToken oAuthToken,
        int start,
        int count,
        string countryCode,
        string locale,
        bool includeDLCDetails,
        bool includeMainGameDetails,
        CancellationToken cancellationToken = default)
    {
        _logger.GetCatalogNamespaceItems(
            url,
            start,
            count,
            countryCode,
            locale,
            includeDLCDetails,
            includeMainGameDetails
        );

        try
        {
            var requestUri = QueryString.CreateUriWithQuery(url, new KeyValuePair<string, string?>[]
            {
                new("start", start.ToString(CultureInfo.InvariantCulture)),
                new("count", count.ToString(CultureInfo.InvariantCulture)),
                new("country", countryCode),
                new("locale", locale),
                new("includeDLCDetails", includeDLCDetails ? "true" : "false"),
                new("includeMainGameDetails", includeMainGameDetails ? "true" : "false"),
            });

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", oAuthToken.Value);

            using var responseMessage = await _client
                .SendAsync(requestMessage, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug("Response code for \"{Url}\" is {Code}", url, responseMessage.StatusCode);
            responseMessage.EnsureSuccessStatusCode();

            var result = await responseMessage.Content
                .ReadFromJsonAsync<CatalogNamespaceEnumerationResult>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
                return ApiError.From($"Deserialization of request for \"{url}\" returned null!");
            return result;
        }
        catch (Exception e)
        {
            return ApiError.From($"Exception trying to contact endpoint \"{url}\":\n{e}");
        }
    }
}
