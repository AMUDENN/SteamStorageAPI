using SteamStorageAPI.Domain.Exceptions;
using SteamStorageAPI.Domain.Constants;

namespace SteamStorageAPI.Domain.Entities;

public class ActiveGroup
{
    #region Properties

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Colour { get; private set; } = GroupConstants.BaseActiveGroupColour;
    public decimal? GoalSum { get; private set; }
    public DateTime DateCreation { get; private set; }

    #endregion

    #region Constructors

    private ActiveGroup() { }

    public ActiveGroup(int userId, string title, string? description, string? colour, decimal? goalSum)
    {
        UserId = userId;
        Title = title;
        Description = description;
        Colour = colour ?? GroupConstants.BaseActiveGroupColour;
        GoalSum = goalSum;
        DateCreation = DateTime.UtcNow;
    }

    #endregion

    #region Methods

    public void Update(string title, string? description, string? colour, decimal? goalSum)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("Название группы не может быть пустым.");
        Title = title;
        Description = description;
        Colour = colour ?? GroupConstants.BaseActiveGroupColour;
        GoalSum = goalSum;
    }
    
    public double? CalculateGoalCompletion(decimal currentSum)
    {
        if (GoalSum is null or 0)
            return null;
        return (double)(currentSum / GoalSum.Value);
    }

    public static double CalculateChange(decimal buySum, decimal currentSum)
    {
        if (buySum == 0) return 0;
        return (double)((currentSum - buySum) / buySum);
    }

    #endregion
}
