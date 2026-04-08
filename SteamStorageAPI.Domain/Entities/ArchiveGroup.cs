using SteamStorageAPI.Domain.Exceptions;
using SteamStorageAPI.Domain.Constants;

namespace SteamStorageAPI.Domain.Entities;

public class ArchiveGroup
{
    #region Properties

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Colour { get; private set; } = GroupConstants.BaseArchiveGroupColour;
    public DateTime DateCreation { get; private set; }

    #endregion

    #region Constructors

    private ArchiveGroup() { }

    public ArchiveGroup(int userId, string title, string? description, string? colour)
    {
        UserId = userId;
        Title = title;
        Description = description;
        Colour = colour ?? GroupConstants.BaseArchiveGroupColour;
        DateCreation = DateTime.UtcNow;
    }

    #endregion

    #region Methods

    public void Update(string title, string? description, string? colour)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainValidationException("Название группы не может быть пустым.");
        Title = title;
        Description = description;
        Colour = colour ?? GroupConstants.BaseArchiveGroupColour;
    }

    public static double CalculateChange(decimal buySum, decimal soldSum)
    {
        if (buySum == 0) return 0;
        return (double)((soldSum - buySum) / buySum);
    }

    #endregion
}
