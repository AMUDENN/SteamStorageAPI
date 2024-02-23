using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SteamStorageAPI.DBEntities;

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
        modelBuilder.Entity<Active>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Actives__3214EC27A0B6A2E3");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BuyDate).HasColumnType("datetime");
            entity.Property(e => e.BuyPrice).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.GoalPrice).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");

            entity.HasOne(d => d.Group).WithMany(p => p.Actives)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Actives__GroupID__5535A963");

            entity.HasOne(d => d.Skin).WithMany(p => p.Actives)
                .HasForeignKey(d => d.SkinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Actives__SkinID__5EBF139D");
        });

        modelBuilder.Entity<ActiveGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ActiveGr__3214EC27D1172601");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Colour).HasMaxLength(8);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.GoalSum).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.ActiveGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ActiveGro__UserI__534D60F1");
        });

        modelBuilder.Entity<ActiveGroupsDynamic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ActiveGr__3214EC272CD4BB2C");

            entity.ToTable("ActiveGroupsDynamic");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DateUpdate).HasColumnType("datetime");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.Sum).HasColumnType("decimal(14, 2)");

            entity.HasOne(d => d.Group).WithMany(p => p.ActiveGroupsDynamics)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ActiveGro__Group__5441852A");
        });

        modelBuilder.Entity<Archive>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Archive__3214EC274C88A91A");

            entity.ToTable("Archive");

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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Archive__GroupID__5FB337D6");

            entity.HasOne(d => d.Skin).WithMany(p => p.Archives)
                .HasForeignKey(d => d.SkinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Archive__SkinID__60A75C0F");
        });

        modelBuilder.Entity<ArchiveGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ArchiveG__3214EC27F80691B1");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Colour).HasMaxLength(8);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.ArchiveGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ArchiveGr__UserI__5CD6CB2B");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Currenci__3214EC273C93DBA6");

            entity.HasIndex(e => e.SteamCurrencyId, "UQ__Currenci__E1F42BF7926A89FE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Mark).HasMaxLength(10);
            entity.Property(e => e.SteamCurrencyId).HasColumnName("SteamCurrencyID");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<CurrencyDynamic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Currency__3214EC27B3D14A7F");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
            entity.Property(e => e.DateUpdate).HasColumnType("datetime");

            entity.HasOne(d => d.Currency).WithMany(p => p.CurrencyDynamics)
                .HasForeignKey(d => d.CurrencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CurrencyD__Curre__72C60C4A");
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Games__3214EC2789919467");

            entity.HasIndex(e => e.SteamGameId, "UQ__Games__20E24016110F4353").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SteamGameId).HasColumnName("SteamGameID");
            entity.Property(e => e.Title).HasMaxLength(300);
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Inventor__3214EC278790377C");

            entity.ToTable("Inventory");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Skin).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.SkinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__SkinI__5812160E");

            entity.HasOne(d => d.User).WithMany(p => p.Inventories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__UserI__5AEE82B9");
        });

        modelBuilder.Entity<MarkedSkin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MarkedSk__3214EC2798FE4252");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Skin).WithMany(p => p.MarkedSkins)
                .HasForeignKey(d => d.SkinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MarkedSki__SkinI__59FA5E80");

            entity.HasOne(d => d.User).WithMany(p => p.MarkedSkins)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MarkedSki__UserI__5CD6CB2B");
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Pages__3214EC2744DC59BE");

            entity.HasIndex(e => e.Title, "UQ__Pages__2CB664DCFBABA136").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC27C69DA33E");

            entity.HasIndex(e => e.Title, "UQ__Roles__2CB664DC7330A5BE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<Skin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Skins__3214EC27352FBBA1");

            entity.HasIndex(e => e.MarketHashName, "UQ__Skins__B593E7DA563B2E74").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GameId).HasColumnName("GameID");
            entity.Property(e => e.MarketHashName).HasMaxLength(300);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.Game).WithMany(p => p.Skins)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Skins__GameID__5535A963");
        });

        modelBuilder.Entity<SkinsDynamic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SkinsDyn__3214EC2762527D0C");

            entity.ToTable("SkinsDynamic");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.DateUpdate).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.SkinId).HasColumnName("SkinID");

            entity.HasOne(d => d.Skin).WithMany(p => p.SkinsDynamics)
                .HasForeignKey(d => d.SkinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SkinsDyna__SkinI__5629CD9C");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC2727F435B9");

            entity.HasIndex(e => e.SteamId, "UQ__Users__6F4E9AD04FAC4669").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
            entity.Property(e => e.DateRegistration).HasColumnType("datetime");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.StartPageId).HasColumnName("StartPageID");
            entity.Property(e => e.SteamId).HasColumnName("SteamID");
            entity.Property(e => e.GoalSum).HasColumnType("decimal(14, 2)");

            entity.HasOne(d => d.Currency).WithMany(p => p.Users)
                .HasForeignKey(d => d.CurrencyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__CurrencyI__5FB337D6");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleID__60A75C0F");

            entity.HasOne(d => d.StartPage).WithMany(p => p.Users)
                .HasForeignKey(d => d.StartPageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__StartPage__0D7A0286");
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
        {
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
    }

    #endregion Methods
}
