namespace SteamStorageAPI.Domain.Entities;

public class ActiveGroupsDynamic
{
    #region Properties

    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public DateTime DateUpdate { get; private set; }
    public decimal Sum { get; private set; }

    #endregion

    #region Constructors

    private ActiveGroupsDynamic() { }

    public ActiveGroupsDynamic(int groupId, decimal sum, DateTime dateUpdate)
    {
        GroupId = groupId;
        Sum = sum;
        DateUpdate = dateUpdate;
    }

    #endregion
}
