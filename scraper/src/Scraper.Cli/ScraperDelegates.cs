using System;
using System.Globalization;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scraper.Lib;

namespace Scraper.Cli;

public class ScraperDelegates : IScraperDelegates
{
    private readonly ILogger<ScraperDelegates> _logger;
    private readonly IFileSystem _fileSystem;

    public ScraperDelegates(ILogger<ScraperDelegates> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task<string> RenderHtmlPage(string url, CancellationToken cancellationToken)
    {
        var path = _fileSystem.Path.GetFullPath($"{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}.html");

        _logger.LogInformation("Opening page \"{Url}\"", url);
        _logger.LogInformation("Download the HTML for this page and save it to \"{Path}\"", path);

        await OpenUrl(url, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Press enter after you've downloaded the page:");
        Console.ReadKey();

        if (!_fileSystem.File.Exists(path))
        {
            _logger.LogError("File does not exist at \"{Path}\"", path);
            throw new Exception($"Unable to download HTML file {path}");
        }

        var text = await _fileSystem.File
            .ReadAllTextAsync(path, Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);

        return text;
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
}
