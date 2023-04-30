using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Scraper.Lib.Models;

[PublicAPI]
[DebuggerDisplay("{StringRepresentation}")]
[JsonConverter(typeof(CategoriesJsonConverter))]
public class Categories : IEquatable<Categories>, IEnumerable<string>
{
    public readonly string[] Parts;
    public readonly string StringRepresentation;

    public Categories(string[] parts)
    {
        Parts = parts;
        StringRepresentation = parts.Length switch
        {
            > 2 => parts.Aggregate((a, b) => $"{a}/{b}"),
            1 => parts[0],
            _ => string.Empty
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => Parts.GetEnumerator();
    public IEnumerator<string> GetEnumerator() => Parts.OfType<string>().GetEnumerator();

    public bool Equals(Categories? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return StringRepresentation.Equals(other.StringRepresentation, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Categories)obj);
    }

    public override int GetHashCode()
    {
        return StringRepresentation.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => StringRepresentation;
}

[PublicAPI]
public class CategoriesJsonConverter : JsonConverter<Categories>
{
    /// <inheritdoc/>
    public override Categories? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert != typeof(Categories))
            throw new ArgumentException($"This JsonConverter only converts {nameof(Categories)} but found {typeToConvert}", nameof(typeToConvert));

        if (reader.TokenType == JsonTokenType.Null) return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var compactString = reader.GetString();
            if (compactString is null) return null;
            if (compactString.Length == 0)
                return new Categories(Array.Empty<string>());
            return !compactString.Contains('/', StringComparison.Ordinal)
                ? new Categories(new[] { compactString })
                : new Categories(compactString.Split('/'));
        }

        ExpectTokenType(ref reader, JsonTokenType.StartArray);

        reader.Read();

        var parts = new List<string>();

        while (reader.TokenType != JsonTokenType.EndArray)
        {
            ExpectTokenType(ref reader, JsonTokenType.StartObject);

            reader.Read();

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                ExpectTokenType(ref reader, JsonTokenType.PropertyName);
                var propertyName = reader.GetString();
                if (propertyName is null)
                    throw new JsonException("String is null");

                if (!string.Equals(propertyName,
                        "path",
                        options.PropertyNameCaseInsensitive
                            ? StringComparison.OrdinalIgnoreCase
                            : StringComparison.Ordinal))
                {
                    // property name
                    reader.Read();

                    // property value
                    if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                    {
                        reader.Skip();
                        reader.Read();
                    }
                    else
                    {
                        reader.Read();
                    }

                    continue;
                }

                reader.Read();
                ExpectTokenType(ref reader, JsonTokenType.String);

                var path = reader.GetString();
                if (path is null)
                    throw new JsonException("String is null");

                parts.Add(path);

                reader.Read();
            }

            reader.Read();
        }

        return new Categories(parts.ToArray());
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Categories value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.StringRepresentation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpectTokenType(ref Utf8JsonReader reader, JsonTokenType expected)
    {
        if (reader.TokenType != expected)
            throw new JsonException($"Expected {expected} found {reader.TokenType}");
    }
}
