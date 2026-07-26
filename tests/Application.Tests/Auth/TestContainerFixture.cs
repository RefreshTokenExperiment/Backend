using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Application.Tests.Auth;

[SetUpFixture]
public sealed class TestContainerFixture
{
    public static PostgreSqlContainer Container { get; private set; }
    public static DbContextOptions<AppDbContext> Options { get; private set; }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        Container = new PostgreSqlBuilder("postgres:18").Build();
        await Container.StartAsync();

        Options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Container.GetConnectionString()).Options;
        var context = new AppDbContext(Options);
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown()
    {
        await Container.StopAsync();
        await Container.DisposeAsync();
    }
}