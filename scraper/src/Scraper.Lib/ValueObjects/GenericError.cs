using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Vogen;

namespace Scraper.Lib.ValueObjects;

[PublicAPI]
[ValueObject<string>(conversions:Conversions.None)]
[SuppressMessage("Usage", "AddValidationMethod:Value Objects can have validation", Justification = "Not needed for this type.")]
public readonly partial struct GenericError { }
