namespace SecureBank.Application.Exceptions;

public sealed class ConcurrencyException(string message) : Exception(message);
