namespace SteamStorageAPI.Utilities.Exceptions;

public class FluentValidatorAutoException(string formatString, params object[] parameters)
    : Exception(string.Format(formatString, parameters));
