using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bento.Api.Data;

public class BentoDbContextFactory : IDesignTimeDbContextFactory<BentoDbContext>
{
    public BentoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql")
            ?? "Host=localhost;Port=5432;Database=bentodb;Username=bento;Password=your_password";

        var options = new DbContextOptionsBuilder<BentoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BentoDbContext(options);
    }
}
