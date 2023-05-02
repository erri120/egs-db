using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Scraper.Lib.Models;
using Scraper.Lib.Services;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib;

[PublicAPI]
public class Scraper
{
    public const string StateFileName = "scraper.state.json";
    public const string NamespacesFileName = "namespaces.json";

    private readonly ILogger<Scraper> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly HttpMessageHandler _httpMessageHandler;
    private readonly IScraperDelegates _scraperDelegates;
    private readonly OAuthHelper _oAuthHelper;

    private ScraperState _scraperState;

    public Scraper(
        ILogger<Scraper> logger,
        IFileSystem fileSystem,
        HttpMessageHandler httpMessageHandler,
        IScraperDelegates scraperDelegates,
        ScraperState scraperState)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _httpMessageHandler = httpMessageHandler;
        _scraperDelegates = scraperDelegates;

        _oAuthHelper = new OAuthHelper(
            httpMessageHandler,
            scraperState.OAuthClientId,
            scraperState.OAuthClientSecret
        );

        _scraperState = scraperState;
    }

    public async ValueTask<IDictionary<CatalogNamespace, UrlSlug>> ScrapNamespaces(CancellationToken cancellationToken)
    {
        const string defaultUrl = "https://store.epicgames.com/en-US/p/fortnite";

        var html = await _scraperDelegates
            .RenderHtmlPage(defaultUrl, cancellationToken)
            .ConfigureAwait(false);

        var res = NamespaceScraper.GetNamespacesFromHtmlText(html);
        return res.AsT0;
    }

    internal async ValueTask<OAuthToken> GetOrRefreshToken(CancellationToken cancellationToken)
    {
        var lastOAuthResponse = _scraperState.LastOAuthResponse;
        if (lastOAuthResponse is null)
        {
            _logger.LogError("OAuth token is missing!");
            throw new Exception("OAuth token is missing!");
        }

        if (DateTimeOffset.UtcNow < lastOAuthResponse.ExpiresAt)
            return lastOAuthResponse.AccessToken;

        if (DateTimeOffset.UtcNow > lastOAuthResponse.RefreshExpiresAt)
        {
            _logger.LogError("OAuth token and refresh token have expired");
            throw new Exception("OAuth token and refresh token have expired");
        }

        var res = await _oAuthHelper
            .GetOAuthTokenAsync(lastOAuthResponse.RefreshToken, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!res.TryPickT0(out var oAuthResponse, out var oAuthError))
        {
            _logger.LogError("{Error}", oAuthError);
            throw new Exception($"{oAuthError}");
        }

        _scraperState.LastOAuthResponse = oAuthResponse;
        await ExportState(cancellationToken).ConfigureAwait(false);
        return oAuthResponse.AccessToken;
    }

    public static async ValueTask<ScraperState?> ImportState(
        ILogger<Scraper> logger,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        if (!fileSystem.File.Exists(StateFileName))
        {
            logger.LogDebug("File \"{FileName}\" does not exist, skipping import", StateFileName);
            return null;
        }

        var res = await fileSystem
            .ReadFromJsonAsync<ScraperState>(StateFileName, logger: logger, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (res.TryPickT0(out var state, out var error))
        {
            logger.LogInformation("Imported state from file \"{FileName}\"", StateFileName);
            return state;
        }

        logger.LogError("{Error}", error.Value);
        return null;
    }

    public async ValueTask ExportState(CancellationToken cancellationToken)
    {
        var error = await _fileSystem
            .WriteToJsonAsync(_scraperState, StateFileName, logger: _logger, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (error is null)
        {
            _logger.LogInformation("Exported state to file \"{FileName}\"", StateFileName);
        }
        else
        {
            _logger.LogError("{Error}", error.Value.Value);
        }
    }
}
