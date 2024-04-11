namespace LoginWebApp.Models;

public class ErrorViewModel
{
    #region Properties

    public string? RequestId { get; init; }

    public bool ShowRequestId
    {
        get => !string.IsNullOrEmpty(RequestId);
    }

    #endregion Properties
}
