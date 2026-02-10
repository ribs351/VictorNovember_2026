using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VictorNovember.Services;

namespace VictorNovember.Data;

public sealed class NovemberContextFactory : IDesignTimeDbContextFactory<NovemberContext>
{
    public NovemberContext CreateDbContext(string[] args)
    {
        var configuration = ConfigurationProviderService.Build();

        var options = new DbContextOptionsBuilder<NovemberContext>()
            .UseSqlServer(configuration["ConnectionStrings:NovemberDb"])
            .Options;

        return new NovemberContext(options);
    }
}
