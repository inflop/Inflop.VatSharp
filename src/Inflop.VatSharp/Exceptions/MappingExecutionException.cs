namespace Inflop.VatSharp.Exceptions;

/// <summary>
/// Runtime mapping error on a specific instance.
/// </summary>
public sealed class MappingExecutionException(string message, Exception inner) : Exception(message, inner);