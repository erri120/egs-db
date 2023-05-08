using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scraper.Lib;
using Scraper.Lib.Services;

namespace Scraper.Cli;

public static class Program
{
    private static readonly CancellationTokenSource CancellationTokenSource = new();

    public static async Task Main(string[] args)
    {
        PosixSignalRegistration.Create(PosixSignal.SIGINT, PosixSignalHandler);
        PosixSignalRegistration.Create(PosixSignal.SIGHUP, PosixSignalHandler);
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, PosixSignalHandler);
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, PosixSignalHandler);

        var host = new HostBuilder()
            .ConfigureServices(serviceCollection => serviceCollection
                .AddSingleton<RateLimiter>(new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                {
                    // TODO: experiment with different values
                    Window = TimeSpan.FromSeconds(2),
                    AutoReplenishment = true,
                    PermitLimit = 1,
                    QueueLimit = 1,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                }))
                .AddSingleton(new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = false,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                })
                .AddSingleton<HttpMessageHandler>(new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromSeconds(10),
                })
                .AddSingleton<IFileSystem, FileSystem>()
                .AddSingleton<IScraperDelegates, ScraperDelegates>()
                .AddSingleton<ApiWrapper>()
                .AddSingleton<Runner>()
                .AddLogging(logging => logging
                    .ClearProviders()
                    .AddJsonConsole()
                    .SetMinimumLevel(LogLevel.Debug)
                )
            )
            .Build();

        var runner = host.Services.GetRequiredService<Runner>();
        await runner.Run(args, CancellationTokenSource.Token).ConfigureAwait(false);
    }

    private static void PosixSignalHandler(PosixSignalContext context)
    {
        context.Cancel = true;

        if (!CancellationTokenSource.IsCancellationRequested)
            CancellationTokenSource.Cancel();
    }
}
