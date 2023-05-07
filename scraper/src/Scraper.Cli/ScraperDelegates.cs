using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Scraper.Lib;

namespace Scraper.Cli;

public class ScraperDelegates : IScraperDelegates
{
    public async Task<string> RenderHtmlPage(string url, CancellationToken cancellationToken)
    {
        await OpenUrl(url, cancellationToken).ConfigureAwait(false);
        throw new NotImplementedException();
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
