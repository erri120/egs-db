using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Scraper.Lib.ValueObjects;

namespace Scraper.Lib.Models;

[PublicAPI]
public record CatalogNamespaceEnumerationResult(
    [property:JsonPropertyName("paging")]
    CatalogNamespaceEnumerationResult.PagingDetails Paging,

    [property:JsonPropertyName("elements")]
    IReadOnlyList<CatalogNamespaceEnumerationResult.Element> Elements)
{
    public record PagingDetails(
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("start")] int Start,
        [property: JsonPropertyName("total")] int Total
    );

    public record Element(
        [property: JsonPropertyName("id")] CatalogId CatalogId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("namespace")] CatalogNamespace CatalogNamespace,
        [property: JsonPropertyName("categories")] Categories? Categories,
        [property: JsonPropertyName("keyImages")] IReadOnlyList<KeyImage>? Images,
        [property: JsonPropertyName("creationDate")] DateTime CreationDate,
        [property: JsonPropertyName("lastModifiedDate")] DateTime LastModifiedDate,
        [property: JsonPropertyName("developer")] string? Developer,
        [property: JsonPropertyName("developerId")] string? DeveloperId,
        [property: JsonPropertyName("applicationId")] string? ApplicationId
    )
    {
        public override string ToString() => $"{CatalogId}";

        public virtual bool Equals(Element? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(CatalogId.Value, other.CatalogId.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return CatalogId.Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }
    }

    public record KeyImage(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("md5")] string Md5,
        [property: JsonPropertyName("width")] int Width,
        [property: JsonPropertyName("height")] int Height,
        [property: JsonPropertyName("size")] int Size,
        [property: JsonPropertyName("uploadedDate")] DateTime UploadedDate
    );
}
