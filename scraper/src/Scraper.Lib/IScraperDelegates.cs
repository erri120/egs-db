using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Scraper.Lib;

[PublicAPI]
public interface IScraperDelegates
{
    Task<string> RenderHtmlPage(string url, CancellationToken cancellationToken);
}
