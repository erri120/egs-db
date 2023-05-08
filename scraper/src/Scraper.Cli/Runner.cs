using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Scraper.Lib;
using Scraper.Lib.Models;
using Scraper.Lib.Services;

namespace Scraper.Cli;

public class Runner
{
    private readonly ILogger<Runner> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IScraperDelegates _scraperDelegates;
    private readonly ApiWrapper _apiWrapper;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly HttpMessageHandler _httpMessageHandler;

    public Runner(
        ILogger<Runner> logger,
        ILoggerFactory loggerFactory,
        IFileSystem fileSystem,
        IScraperDelegates scraperDelegates,
        ApiWrapper apiWrapper,
        HttpMessageHandler httpMessageHandler,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _fileSystem = fileSystem;
        _scraperDelegates = scraperDelegates;
        _apiWrapper = apiWrapper;
        _httpMessageHandler = httpMessageHandler;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task Run(string[] args, CancellationToken cancellationToken)
    {
        var argsParseRes = CliOptionsParser.ParseArguments(args);
        var task = argsParseRes.Match(
            oAuthLoginOptions => DoOAuthLogin(oAuthLoginOptions, cancellationToken),
            scrapNamespacesOptions => ScrapNamespaces(scrapNamespacesOptions, cancellationToken),
            refreshOAuthTokenOptions => RefreshOAuthToken(refreshOAuthTokenOptions, cancellationToken),
            parseApiOptions => ScrapApi(parseApiOptions, cancellationToken),
            cliOptionsParserError =>
            {
                _logger.LogError("{Error}", cliOptionsParserError);
                return ValueTask.CompletedTask;
            });

        await task.ConfigureAwait(false);
    }

    private async ValueTask<MainScraper?> CreateScraper(CancellationToken cancellationToken)
    {
        var scraperLogger = _loggerFactory.CreateLogger<MainScraper>();

        var importedState = await MainScraper.ImportState(
            scraperLogger,
            _fileSystem,
            _jsonSerializerOptions,
            cancellationToken
        ).ConfigureAwait(false);

        if (importedState is null)
        {
            _logger.LogError("Unable to import state!");
            return null;
        }

        var scraper = new MainScraper(
            scraperLogger,
            _fileSystem,
            _httpMessageHandler,
            _scraperDelegates,
            _apiWrapper,
            _jsonSerializerOptions,
            importedState
        );

        return scraper;
    }

    private async ValueTask ScrapNamespaces(ScrapNamespacesOptions scrapNamespacesOptions, CancellationToken cancellationToken)
    {
        var scraper = await CreateScraper(cancellationToken).ConfigureAwait(false);
        if (scraper is null) return;

        await scraper.ScrapNamespaces(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ScrapApi(ParseApiOptions parseApiOptions, CancellationToken cancellationToken)
    {
        var scraper = await CreateScraper(cancellationToken).ConfigureAwait(false);
        if (scraper is null) return;

        await scraper.ScrapApi(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask RefreshOAuthToken(RefreshOAuthTokenOptions refreshOAuthTokenOptions, CancellationToken cancellationToken)
    {
        var scraper = await CreateScraper(cancellationToken).ConfigureAwait(false);
        if (scraper is null) return;

        try
        {
            await scraper.GetOrRefreshToken(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("OAuth token refreshed");
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async ValueTask DoOAuthLogin(OAuthLoginOptions oAuthLoginOptions, CancellationToken cancellationToken)
    {
        var helper = new OAuthHelper(_httpMessageHandler, oAuthLoginOptions.ClientId, oAuthLoginOptions.ClientSecret);

        var res = await helper
            .GetOAuthTokenAsync(oAuthLoginOptions.AuthorizationCode, cancellationToken)
            .ConfigureAwait(false);

        if (!res.TryPickT0(out var response, out var error))
        {
            _logger.LogError("{Error}", error);
            return;
        }

        var state = new ScraperState
        {
            OAuthClientId = oAuthLoginOptions.ClientId,
            OAuthClientSecret = oAuthLoginOptions.ClientSecret,
            LastOAuthResponse = response,
        };

        await MainScraper.ExportState(
            _loggerFactory.CreateLogger<MainScraper>(),
            _fileSystem,
            _jsonSerializerOptions,
            state,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
