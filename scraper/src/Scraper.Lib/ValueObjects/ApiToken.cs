using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Vogen;

namespace Scraper.Lib.ValueObjects;

/// <summary>
/// Represents an api token.
/// </summary>
[PublicAPI]
[ValueObject<string>(conversions: Conversions.SystemTextJson)]
[SuppressMessage(
    "Usage",
    "AddValidationMethod:Value Objects can have validation",
    Justification = "Validation is not required for this type.")]
public readonly partial struct ApiToken { }
