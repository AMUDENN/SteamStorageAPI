using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SteamStorageAPI.Models.DBEntities;

public partial class SteamStorageContext : DbContext
{
    #region Properties

    public virtual DbSet<Active> Actives { get; set; }

    public virtual DbSet<ActiveGroup> ActiveGroups { get; set; }

    public virtual DbSet<ActiveGroupsDynamic> ActiveGroupsDynamics { get; set; }

    public virtual DbSet<Archive> Archives { get; set; }

    public virtual DbSet<ArchiveGroup> ArchiveGroups { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<CurrencyDynamic> CurrencyDynamics { get; set; }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<MarkedSkin> MarkedSkins { get; set; }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Skin> Skins { get; set; }

    public virtual DbSet<SkinsDynamic> SkinsDynamics { get; set; }

    public virtual DbSet<User> Users { get; set; }

    #endregion Properties

    #region Constructor

    public SteamStorageContext(DbContextOptions<SteamStorageContext> options)
        : base(options)
    {
        SaveChangesFailed += SaveChangesFailedHandler;
    }

    #endregion Constructor

    #region Methods

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Active>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Actives");

            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.SkinId);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BuyDate).HasColumnType("datetime");
            entity.Property(e => e.BuyPrice).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.GoalPrice).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");

            entity.HasOne(d => d.Group).WithMany(p => p.Actives)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_Actives_ActiveGroups");

            entity.HasOne(d => d.Skin).WithMany(p => p.Actives)
                .HasForeignKey(d => d.SkinId)
                .HasConstraintName("FK_Actives_Skins");
        });

        modelBuilder.Entity<ActiveGroup>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_ActiveGroups");

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Colour).HasMaxLength(8);
            entity.Property(e => e.DateCreation).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.GoalSum).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.ActiveGroups)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ActiveGroups_Users");
        });

        modelBuilder.Entity<ActiveGroupsDynamic>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_ActiveGroupsDynamic");

            entity.ToTable("ActiveGroupsDynamic");

            entity.HasIndex(e => new
            {
                e.GroupId,
                e.DateUpdate
            });

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DateUpdate).HasColumnType("datetime");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.Sum).HasColumnType("decimal(14, 2)");

            entity.HasOne(d => d.Group).WithMany(p => p.ActiveGroupsDynamics)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_ActiveGroupsDynamic_ActiveGroups");
        });

        modelBuilder.Entity<Archive>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Archive");

            entity.ToTable("Archive");

            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.SkinId);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BuyDate).HasColumnType("datetime");
            entity.Property(e => e.BuyPrice).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");
            entity.Property(e => e.SoldDate).HasColumnType("datetime");
            entity.Property(e => e.SoldPrice).HasColumnType("decimal(14, 2)");

            entity.HasOne(d => d.Group).WithMany(p => p.Archives)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_Archive_ArchiveGroups");

            entity.HasOne(d => d.Skin).WithMany(p => p.Archives)
                .HasForeignKey(d => d.SkinId)
                .HasConstraintName("FK_Archive_Skins");
        });

        modelBuilder.Entity<ArchiveGroup>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_ArchiveGroups");

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Colour).HasMaxLength(8);
            entity.Property(e => e.DateCreation).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.ArchiveGroups)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ArchiveGroups_Users");
        });

        modelBuilder.Entity<Currency>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Currencies");

            entity.HasIndex(e => e.SteamCurrencyId, "UQ_Currencies_SteamCurrencyID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CultureInfo).HasMaxLength(10);
            entity.Property(e => e.Mark).HasMaxLength(10);
            entity.Property(e => e.SteamCurrencyId).HasColumnName("SteamCurrencyID");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.IsBase).HasDefaultValue(false);
        });

        modelBuilder.Entity<CurrencyDynamic>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_CurrencyDynamics");

            entity.HasIndex(e => new
            {
                e.CurrencyId,
                e.DateUpdate
            });

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
            entity.Property(e => e.DateUpdate).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(14, 4)");
            ;

            entity.HasOne(d => d.Currency).WithMany(p => p.CurrencyDynamics)
                .HasForeignKey(d => d.CurrencyId)
                .HasConstraintName("FK_CurrencyDynamics_Currencies");
        });

        modelBuilder.Entity<Game>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Games");

            entity.HasIndex(e => e.SteamGameId, "UQ_Games_SteamGameID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SteamGameId).HasColumnName("SteamGameID");
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.IsBase).HasDefaultValue(false);
        });

        modelBuilder.Entity<Inventory>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Inventory");

            entity.ToTable("Inventory");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SkinId);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Skin).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.SkinId)
                .HasConstraintName("FK_Inventory_Skins");

            entity.HasOne(d => d.User).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Inventory_Users");
        });

        modelBuilder.Entity<MarkedSkin>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_MarkedSkins");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SkinId);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Skin).WithMany(p => p.MarkedSkins)
                .HasForeignKey(d => d.SkinId)
                .HasConstraintName("FK_MarkedSkins_Skins");

            entity.HasOne(d => d.User).WithMany(p => p.MarkedSkins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_MarkedSkins_Users");
        });

        modelBuilder.Entity<Page>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Pages");

            entity.HasIndex(e => e.Title, "UQ_Pages_Title").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Roles");

            entity.HasIndex(e => e.Title, "UQ_Roles_Title").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Skin>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Skins");

            entity.HasIndex(e => e.MarketHashName, "UQ_Skins_MarketHashName").IsUnique();
            entity.HasIndex(e => e.GameId);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CurrentPrice).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.GameId).HasColumnName("GameID");
            entity.Property(e => e.MarketHashName).HasMaxLength(300);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.Game).WithMany(p => p.Skins)
                .HasForeignKey(d => d.GameId)
                .HasConstraintName("FK_Skins_Games");
        });

        modelBuilder.Entity<SkinsDynamic>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_SkinsDynamic");

            entity.ToTable("SkinsDynamic", tb => tb.HasTrigger("UpdateSkinsCurrentPrice"));

            entity.HasIndex(e => new
            {
                e.SkinId,
                e.DateUpdate
            });

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DateUpdate).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");

            entity.HasOne(d => d.Skin).WithMany(p => p.SkinsDynamics)
                .HasForeignKey(d => d.SkinId)
                .HasConstraintName("FK_SkinsDynamic_Skins");
        });

        modelBuilder.Entity<User>(entity => {
            entity.HasKey(e => e.Id).HasName("PK_Users");

            entity.HasIndex(e => e.SteamId, "UQ_Users_SteamID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
            entity.Property(e => e.DateRegistration).HasColumnType("datetime");
            entity.Property(e => e.DateUpdate).HasColumnType("datetime");
            entity.Property(e => e.GoalSum).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.StartPageId).HasColumnName("StartPageID");
            entity.Property(e => e.SteamId).HasColumnName("SteamID");
            entity.Property(e => e.Username)
                .HasMaxLength(32)
                .IsFixedLength();

            entity.HasOne(d => d.Currency).WithMany(p => p.Users)
                .HasForeignKey(d => d.CurrencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Currencies");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");

            entity.HasOne(d => d.StartPage).WithMany(p => p.Users)
                .HasForeignKey(d => d.StartPageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Pages");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    private void SaveChangesFailedHandler(object? sender, SaveChangesFailedEventArgs e)
    {
        UndoChanges();
    }

    private void UndoChanges()
    {
        foreach (EntityEntry entry in ChangeTracker.Entries())
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.State = EntityState.Unchanged;
                    break;
                case EntityState.Deleted:
                    entry.Reload();
                    break;
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
            }
    }

    #endregion Methods
}