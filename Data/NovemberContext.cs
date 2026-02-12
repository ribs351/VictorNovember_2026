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
    }
}
