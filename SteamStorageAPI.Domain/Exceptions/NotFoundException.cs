namespace SteamStorageAPI.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id={id} not found.") { }

    public NotFoundException(string message)
        : base(message) { }
}
