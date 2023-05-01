using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Abstractions;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetEscapades.EnumGenerators;
using OneOf;
using Scraper.Lib;
using Scraper.Lib.Models;
using Scraper.Lib.Services;
using Scraper.Lib.ValueObjects;
using Spectre.Console;

namespace Scraper.Cli;

public class GuidedRunner
{
    private OAuthToken _oAuthToken;

    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;
    private readonly HttpMessageHandler _httpMessageHandler;

    public GuidedRunner(IAnsiConsole console, IFileSystem fileSystem)
    {
        _console = console;
        _fileSystem = fileSystem;

        _httpMessageHandler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(5),
        };
    }

    private async ValueTask LoadSettings(CancellationToken cancellationToken)
    {
        const string filePath = "egs-db-scraper.json";
        if (!_fileSystem.File.Exists(filePath)) return;

        var stream = _fileSystem.File.OpenRead(filePath);
        await using (stream.ConfigureAwait(false))
        {
            var scraperSettings = await JsonSerializer
                .DeserializeAsync<ScraperSettings>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (scraperSettings is null) return;
            _console.WriteLine("Read settings from file");

            _oAuthToken = scraperSettings.OAuthToken;
        }
    }

    public async ValueTask Start(CancellationToken cancellationToken)
    {
        await LoadSettings(cancellationToken).ConfigureAwait(false);

        var startChoicePrompt = new SelectionPrompt<StartChoice>()
            .AddChoices(StartChoiceExtensions.GetValues())
            .UseConverter(x => x.ToStringFast());

        var startChoice = await startChoicePrompt
            .ShowAsync(_console, cancellationToken)
            .ConfigureAwait(false);

        if (startChoice == StartChoice.GetOAuthToken)
        {
            var res = await GetOAuthToken(cancellationToken).ConfigureAwait(false);
            if (!res.TryPickT0(out var oAuthResponse, out var oAuthError))
            {
                _console.MarkupLine($"[red]{oAuthError}[/]");
                return;
            }

            // TODO: should probably save more
            _oAuthToken = oAuthResponse.AccessToken;
        }
        else if (startChoice == StartChoice.GetNamespaces)
        {
            var res = await GetNamespaces(cancellationToken).ConfigureAwait(false);
            if (!res) return;
        }

        _console.WriteLine(startChoice.ToStringFast());
    }

    private async ValueTask<bool> GetNamespaces(CancellationToken cancellationToken)
    {
        const string fileName = "input.html";
        const string url = "https://store.epicgames.com/en-US/p/fortnite";
        await OpenUrl(url, cancellationToken).ConfigureAwait(false);

        var _ = _console.Prompt(new ConfirmationPrompt($"Download the site as HTML to \"{fileName}\""));
        if (!_fileSystem.File.Exists(fileName))
        {
            _console.Markup("[red]File does not exist![/]");
            return false;
        }

        var html = await _fileSystem.File.ReadAllTextAsync(fileName, cancellationToken).ConfigureAwait(false);
        var res = NamespaceScraper.GetNamespacesFromHtmlText(html);
        if (!res.TryPickT0(out var mappings, out var scraperError))
        {
            _console.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[red]{scraperError.Value}[/]");
            return false;
        }

        _console.WriteLine($"Mappings: {mappings.Count}");
        await mappings
            .CreateOrUpdateDump(_fileSystem.FileInfo.New("namespaces.json"), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    private async ValueTask<OneOf<OAuthResponse, OAuthError>> GetOAuthToken(CancellationToken cancellationToken)
    {
        var oauthHelper = new OAuthHelper(_httpMessageHandler);

        await OpenUrl(oauthHelper.OAuthUrl, cancellationToken).ConfigureAwait(false);

        var authorizationCode = AuthorizationCode.From(AnsiConsole.Prompt(new TextPrompt<string>("Input the \"authorizationCode\"")));
        var res = await oauthHelper.GetOAuthTokenAsync(authorizationCode, cancellationToken).ConfigureAwait(false);
        return res;
    }

    private static async ValueTask OpenUrl(string url, CancellationToken cancellationToken)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = CliWrap.Cli
                .Wrap("xdg-open")
                .WithArguments(url);
            await command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var command = CliWrap.Cli
                .Wrap("cmd.exe")
                .WithArguments($@"/c start """" ""{url}""");
            await command.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }

    [EnumExtensions]
    public enum StartChoice
    {
        [Display(Name = "Get OAuth Token")]
        GetOAuthToken,

        [Display(Name = "Scrape Namespaces")]
        GetNamespaces,
    }
}
