namespace SteamStorageAPI.Domain.Exceptions;

public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
}
