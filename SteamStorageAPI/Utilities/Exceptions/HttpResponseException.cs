namespace SteamStorageAPI.Utilities.Exceptions;

public class HttpResponseException : Exception
{
    #region Properties

    public int StatusCode { get; }

    #endregion Properties

    #region Constructor

    public HttpResponseException(
        int statusCode = StatusCodes.Status400BadRequest,
        string? message = null) : base(message)
    {
        StatusCode = statusCode;
    }

    #endregion Constructor
}