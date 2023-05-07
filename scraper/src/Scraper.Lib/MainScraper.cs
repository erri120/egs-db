using System;
using System.Collections.Generic;
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
public class MainScraper
{
    public const string StateFileName = "scraper.state.json";
    public const string NamespacesFileName = "namespaces.json";

    private readonly ILogger<MainScraper> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly HttpMessageHandler _httpMessageHandler;
    private readonly IScraperDelegates _scraperDelegates;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly OAuthHelper _oAuthHelper;

    private ScraperState _scraperState;

    public MainScraper(
        ILogger<MainScraper> logger,
        IFileSystem fileSystem,
        HttpMessageHandler httpMessageHandler,
        IScraperDelegates scraperDelegates,
        JsonSerializerOptions jsonSerializerOptions,
        ScraperState scraperState)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _httpMessageHandler = httpMessageHandler;
        _scraperDelegates = scraperDelegates;
        _jsonSerializerOptions = jsonSerializerOptions;

        _oAuthHelper = new OAuthHelper(
            httpMessageHandler,
            scraperState.OAuthClientId,
            scraperState.OAuthClientSecret
        );

        _scraperState = scraperState;
    }

    public async ValueTask ScrapNamespaces(CancellationToken cancellationToken)
    {
        const string defaultUrl = "https://store.epicgames.com/en-US/p/fortnite";

        var html = await _scraperDelegates
            .RenderHtmlPage(defaultUrl, cancellationToken)
            .ConfigureAwait(false);

        var res = NamespaceScraper.GetNamespacesFromHtmlText(html);
        if (!res.TryPickT0(out var mappings, out var namespaceError))
        {
            _logger.LogError("{Error}", namespaceError.Value);
            return;
        }

        var outputPath = _fileSystem.Path.Combine(
            _scraperState.OutputFolder,
            NamespacesFileName
        );

        GenericError? writeError;
        if (_fileSystem.File.Exists(outputPath))
        {
            var existingMappingsResult = await _fileSystem
                .ReadFromJsonAsync<IDictionary<CatalogNamespace, UrlSlug>>(outputPath, options: _jsonSerializerOptions, logger: _logger, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!existingMappingsResult.TryPickT0(out var existingMappings, out var genericError))
            {
                _logger.LogError("Error reading existing mappings: {Error}", genericError.Value);
                return;
            }

            // NOTE(erri120): existing mappings are always overwritten.
            foreach (var kv in mappings)
            {
                existingMappings[kv.Key] = kv.Value;
            }

            writeError = await _fileSystem.WriteToJsonAsync(
                    existingMappings,
                    outputPath,
                    options: _jsonSerializerOptions,
                    logger: _logger,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            writeError = await _fileSystem.WriteToJsonAsync(
                    mappings,
                    outputPath,
                    options: _jsonSerializerOptions,
                    logger: _logger,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        if (writeError.HasValue)
        {
            _logger.LogError("{Error}", writeError.Value);
        }
    }

    public async ValueTask<OAuthToken> GetOrRefreshToken(CancellationToken cancellationToken)
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
        await ExportState(_logger, _fileSystem, _jsonSerializerOptions, _scraperState, cancellationToken).ConfigureAwait(false);
        return oAuthResponse.AccessToken;
    }

    public static async ValueTask<ScraperState?> ImportState(
        ILogger<MainScraper> logger,
        IFileSystem fileSystem,
        JsonSerializerOptions jsonSerializerOptions,
        CancellationToken cancellationToken)
    {
        if (!fileSystem.File.Exists(StateFileName))
        {
            logger.LogDebug("File \"{FileName}\" does not exist, skipping import", StateFileName);
            return null;
        }

        var res = await fileSystem
            .ReadFromJsonAsync<ScraperState>(StateFileName, options: jsonSerializerOptions, logger: logger, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (res.TryPickT0(out var state, out var error))
        {
            logger.LogInformation("Imported state from file \"{FileName}\"", StateFileName);
            return state;
        }

        logger.LogError("{Error}", error.Value);
        return null;
    }

    public static async ValueTask ExportState(
        ILogger<MainScraper> logger,
        IFileSystem fileSystem,
        JsonSerializerOptions jsonSerializerOptions,
        ScraperState scraperState,
        CancellationToken cancellationToken)
    {
        var error = await fileSystem
            .WriteToJsonAsync(scraperState, StateFileName, options: jsonSerializerOptions, logger: logger, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (error is null)
        {
            logger.LogInformation("Exported state to file \"{FileName}\"", StateFileName);
        }
        else
        {
            logger.LogError("{Error}", error.Value.Value);
        }
    }
}
