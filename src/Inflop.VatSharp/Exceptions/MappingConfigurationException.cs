namespace Inflop.VatSharp.Exceptions;

/// <summary>
/// Exception thrown when the mapping configuration is invalid,
/// e.g. when a required property is missing or has an incompatible type.
/// </summary>
public sealed class MappingConfigurationException(string message) : Exception(message);