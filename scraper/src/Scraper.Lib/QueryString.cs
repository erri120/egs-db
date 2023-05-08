using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using JetBrains.Annotations;

namespace Scraper.Lib;

[PublicAPI]
public static class QueryString
{
    public static Uri CreateUriWithQuery(string url, [InstantHandle(RequireAwait = false)] IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        var queryString = CreateQueryString(parameters);
        return new Uri(url + queryString, UriKind.Absolute);
    }

    public static string CreateQueryString([InstantHandle(RequireAwait = false)] IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        var builder = new StringBuilder();
        var first = true;

        foreach (var kv in parameters)
        {
            builder.Append(first ? '?' : '&');
            first = false;

            builder.Append(UrlEncoder.Default.Encode(kv.Key));
            builder.Append('=');
            if (!string.IsNullOrEmpty(kv.Value))
            {
                builder.Append(UrlEncoder.Default.Encode(kv.Value));
            }
        }

        return builder.ToString();
    }
}
