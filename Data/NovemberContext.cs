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
    }
}
