namespace LoginWebApp.Models;

public class ErrorViewModel
{
    #region Properties

    public string? RequestId { get; init; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    #endregion Properties
}