﻿namespace SteamStorageAPI.DBEntities;

public class Archive
{
    #region Properties

    public int Id { get; set; }

    public int GroupId { get; set; }

    public int SkinId { get; set; }

    public string? Description { get; set; }

    public int Count { get; set; }

    public DateTime BuyDate { get; set; }

    public decimal BuyPrice { get; set; }

    public DateTime SoldDate { get; set; }

    public decimal SoldPrice { get; set; }

    public virtual ArchiveGroup Group { get; set; } = null!;

    public virtual Skin Skin { get; set; } = null!;

    #endregion Properties
}
