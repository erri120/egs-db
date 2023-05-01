using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Scraper.Cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var runner = new GuidedRunner(AnsiConsole.Console, new FileSystem());
        // TODO: cancellation token using SIGTERM
        await runner.Start(CancellationToken.None).ConfigureAwait(false);
    }
}
