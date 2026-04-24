namespace AdminPanel.Services.CookiesUserService;

public interface ICookiesUserService
{
    void SetCookiesToken(string? token);
    string? GetCookiesToken();
}