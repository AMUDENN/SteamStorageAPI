using SteamStorageAPI.Domain.Exceptions;
using SteamStorageAPI.Domain.Constants;

namespace SteamStorageAPI.Domain.Entities;

public class User
{
    #region Properties

    public int Id { get; private set; }
    public long SteamId { get; private set; }
    public int RoleId { get; private set; }
    public int StartPageId { get; private set; }
    public int CurrencyId { get; private set; }
    public DateTime DateRegistration { get; private set; }
    public DateTime? DateUpdate { get; private set; }
    public decimal? GoalSum { get; private set; }
    public string? Username { get; private set; }
    public string? IconUrl { get; private set; }
    public string? IconUrlMedium { get; private set; }
    public string? IconUrlFull { get; private set; }

    #endregion

    #region Constructors

    private User() { }
    
    public User(long steamId, int roleId)
    {
        SteamId = steamId;
        RoleId = roleId;
        StartPageId = UserConstants.BaseStartPageId;
        CurrencyId = CurrencyConstants.BaseCurrencyId;
        DateRegistration = DateTime.UtcNow;
    }

    #endregion

    #region Methods
    
    public void UpdateSteamProfile(
        string? username,
        string? iconUrl,
        string? iconUrlMedium,
        string? iconUrlFull)
    {
        Username = username?.Trim();
        IconUrl = iconUrl;
        IconUrlMedium = iconUrlMedium;
        IconUrlFull = iconUrlFull;
        DateUpdate = DateTime.UtcNow;
    }

    public void SetGoalSum(decimal? goalSum)
    {
        if (goalSum is < 0)
            throw new DomainValidationException("Финансовая цель не может быть отрицательной.");
        GoalSum = goalSum;
    }

    public void SetCurrency(int currencyId)
    {
        if (currencyId <= 0)
            throw new DomainValidationException("Некорректный идентификатор валюты.");
        CurrencyId = currencyId;
    }

    public void SetStartPage(int pageId)
    {
        if (pageId <= 0)
            throw new DomainValidationException("Некорректный идентификатор страницы.");
        StartPageId = pageId;
    }

    public void SetRole(int roleId)
    {
        if (roleId <= 0)
            throw new DomainValidationException("Некорректный идентификатор роли.");
        RoleId = roleId;
    }
    
    public double CalculateGoalCompletion(decimal currentSum)
    {
        if (GoalSum is null or 0)
            return 1;
        return (double)(currentSum / GoalSum.Value);
    }

    #endregion
}
