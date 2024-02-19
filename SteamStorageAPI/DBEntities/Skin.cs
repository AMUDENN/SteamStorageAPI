namespace SteamStorageAPI.DBEntities;

public class Skin
{
    #region Properties

    public int Id { get; set; }

    public int GameId { get; set; }

    public string MarketHashName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string SkinIconUrl { get; set; } = null!;

    public virtual ICollection<Active> Actives { get; set; } = new List<Active>();

    public virtual ICollection<Archive> Archives { get; set; } = new List<Archive>();

    public virtual Game Game { get; set; } = null!;

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<MarkedSkin> MarkedSkins { get; set; } = new List<MarkedSkin>();

    public virtual ICollection<SkinsDynamic> SkinsDynamics { get; set; } = new List<SkinsDynamic>();

    #endregion Properties
}
