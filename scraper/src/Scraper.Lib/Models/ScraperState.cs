using JetBrains.Annotations;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Models;

[PublicAPI]
public record ScraperState
{
    public string OutputFolder { get; set; } = "scraper-output";
    public OAuthResponse? LastOAuthResponse { get; set; }
    public required OAuthClientId OAuthClientId { get; set; }
    public required OAuthClientSecret OAuthClientSecret { get; set; }
}
