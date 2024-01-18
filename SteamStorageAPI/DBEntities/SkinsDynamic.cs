namespace SteamStorageAPI.DBEntities;

public class SkinsDynamic
{
    public int Id { get; set; }

    public int SkinId { get; set; }

    public DateTime DateUpdate { get; set; }

    public decimal Price { get; set; }

    public virtual Skin Skin { get; set; } = null!;
}
