using Microsoft.EntityFrameworkCore;
using VictorNovember.Data.Entities;

namespace VictorNovember.Data;

public class NovemberContext : DbContext
{
    public NovemberContext(DbContextOptions<NovemberContext> options)
        : base(options)
    {
        
    }

    public DbSet<Server> Servers => Set<Server>();
    public DbSet<Memorial> Memorials => Set<Memorial>();
    public DbSet<SearchUsage> SearchUsages => Set<SearchUsage>();
    public DbSet<HoneypotConfig> HoneypotConfigs => Set<HoneypotConfig>();
    public DbSet<HoneypotHit> HoneypotHits => Set<HoneypotHit>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Server>(entity =>
        {
            entity.ToTable("Servers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnType("decimal(20,0)")
                  .ValueGeneratedNever();  // make sure to set this for future tables as well
            entity.Property(e => e.WelcomeBannerUrl)
                  .HasMaxLength(512);
        });

        modelBuilder.Entity<Memorial>(entity =>
        {
            entity.ToTable("Memorials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id);
            entity.Property(e => e.PersonName)
                  .HasMaxLength(256)
                  .IsRequired();
            entity.Property(e => e.Message)
                  .HasMaxLength(2000)
                  .IsRequired();
            entity.Property(e => e.RecipientUserId)
                  .HasColumnType("decimal(20,0)");
            entity.Property(e => e.CronExpression)
                  .HasMaxLength(100)
                  .IsRequired();
            entity.Property(e => e.Date)
                  .IsRequired();
        });

        modelBuilder.Entity<SearchUsage>()
        .HasIndex(x => x.MonthKey)
        .IsUnique();

        modelBuilder.Entity<HoneypotConfig>(entity =>
        {
            entity.ToTable("HoneypotConfigs");
            entity.HasKey(e => e.GuildId);
            entity.Property(e => e.GuildId).HasColumnType("decimal(20,0)").ValueGeneratedNever();
            entity.Property(e => e.ChannelId).HasColumnType("decimal(20,0)");
            entity.Property(e => e.ModLogChannelId).HasColumnType("decimal(20,0)");
            entity.Property(e => e.WarningMessageId).HasColumnType("decimal(20,0)");
            entity.Property(e => e.CounterMessageId).HasColumnType("decimal(20,0)");
            entity.Property(e => e.ConfiguredByUserId).HasColumnType("decimal(20,0)");
        });

        modelBuilder.Entity<HoneypotHit>(entity =>
        {
            entity.ToTable("HoneypotHits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GuildId).HasColumnType("decimal(20,0)");
            entity.Property(e => e.UserId).HasColumnType("decimal(20,0)");
            entity.Property(e => e.Username).HasMaxLength(256).IsRequired();
            entity.Property(e => e.MessageContent).HasMaxLength(2000);
        });
    }
}
