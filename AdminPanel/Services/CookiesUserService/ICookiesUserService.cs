namespace AdminPanel.Services.CookiesUserService;

public interface ICookiesUserService
{
    void SetCookiesToken(string? token, CancellationToken cancellationToken = default);
    string? GetCookiesToken(CancellationToken cancellationToken = default);
}