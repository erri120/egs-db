using System;
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

    private readonly ILogger<Scraper> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly HttpMessageHandler _httpMessageHandler;
    private readonly OAuthHelper _oAuthHelper;

    private ScraperState _scraperState;

    public Scraper(
        ILogger<Scraper> logger,
        IFileSystem fileSystem,
        HttpMessageHandler httpMessageHandler,
        ScraperState scraperState)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _httpMessageHandler = httpMessageHandler;

        _oAuthHelper = new OAuthHelper(
            httpMessageHandler,
            scraperState.OAuthClientId,
            scraperState.OAuthClientSecret
        );

        _scraperState = scraperState;
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

        try
        {
            var stream = fileSystem.File.Open(
                StateFileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

            await using (stream.ConfigureAwait(false))
            {
                var state = await JsonSerializer.DeserializeAsync<ScraperState>(
                    stream,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (state is null)
                {
                    logger.LogError("Unable to deserialize file \"{FileName}\", result is null", StateFileName);
                    return null;
                }

                logger.LogInformation("Imported state from file \"{FileName}\"", StateFileName);
                return state;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception while importing state from file \"{FileName}\"", StateFileName);
            return null;
        }
    }

    public async ValueTask ExportState(CancellationToken cancellationToken)
    {
        try
        {
            var stream = _fileSystem.File.Open(
                StateFileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read
            );

            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer
                    .SerializeAsync(stream, _scraperState, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Exported state to file \"{FileName}\"", StateFileName);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while exporting state to file \"{FileName}\"", StateFileName);
        }
    }
}
