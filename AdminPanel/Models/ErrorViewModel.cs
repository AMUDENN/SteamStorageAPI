namespace AdminPanel.Models;

public class ErrorViewModel
{
    #region Properties

    public string? RequestId { get; set; }

    public bool ShowRequestId
    {
        get => !string.IsNullOrEmpty(RequestId);
    }

    #endregion Properties
}
