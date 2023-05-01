using Microsoft.Extensions.Logging;

namespace Scraper.Lib;

public static partial class Logging
{
    [LoggerMessage(
        EventId = 1,
        EventName = $"{nameof(GetCatalogNamespaceItems)}",
        Level = LogLevel.Debug,
        Message = "Sending request to \"{url}\" with the following values: " +
                  "start={start}, " +
                  "count={count}, " +
                  "countryCode={countryCode}, " +
                  "locale={locale}, " +
                  "includeDLCDetails={includeDLCDetails}, " +
                  "includeMainGameDetails={includeMainGameDetails}")]
    public static partial void GetCatalogNamespaceItems(
        this ILogger logger,
        string url,
        int start,
        int count,
        string countryCode,
        string locale,
        bool includeDLCDetails,
        bool includeMainGameDetails
    );
}
