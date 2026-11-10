using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Taurus.Core.Users.Attributes;
using Taurus.Infrastructure.Authentication.Opaque;
using OPAQUE.Net;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Attributes;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;
using Taurus.Infrastructure.Provisioning;
using Taurus.Infrastructure.Schemas;

namespace Taurus.Tests.tests;

public class SchemaTests
{
    private const string ConnectionString = "Data Source=:memory:";
    private const string TenantId = "tenant";
    private const string AttributeName = "department";
    private const string Value = "engineering";
    private const string Origin = "https://app.localhost";
    private const string SecretEnvironment = "TAURUS_TEST_BOOTSTRAP_SECRET";
    private const string Password = "bootstrap-test-password";
    private const string ReplacementPassword = "different-test-password";
    private const string StaleFingerprint = "stale";

    [Fact]
    public async Task BootstrapIsIdempotentAndSchemaApprovalIsExplicit()
    {
        await using var db = new SqliteDatabase(
            new DbContextOptionsBuilder<SqliteDatabase>()
                .UseSqlite(ConnectionString).Options);
        var tenant = new Tenant
        {
            Id = TenantId,
            Issuer = Origin,
            ApplicationOrigins = [Origin],
            RootPassword = new SecretReference { Environment = SecretEnvironment },
            Attributes = [new AttributeDefinition { Name = AttributeName }]
        };
        var settings = new Settings { NoRateLimits = true, Tenants = [tenant] };
        Assert.True(new OpaqueServer().CreateSetup(out var setup));

        var opaque = new OpaqueService(new OpaqueOptions
        {
            Setup = new SecretReference { Value = setup }
        }, TimeProvider.System);
        var bootstrap = new Bootstrapper(db, settings, opaque,
            new UserLogRepo(db, NullLogger<UserLogRepo>.Instance));
        var schema = new SchemaManager(db, settings,
            new UserLogRepo(db, NullLogger<UserLogRepo>.Instance));

        Environment.SetEnvironmentVariable(SecretEnvironment, Password);

        try
        {
            await db.Database.OpenConnectionAsync();
            await db.Database.EnsureCreatedAsync();
            await bootstrap.RunAsync(default);

            var originalRecord = (await db.Credentials.SingleAsync()).Material;

            Environment.SetEnvironmentVariable(SecretEnvironment, ReplacementPassword);
            await bootstrap.RunAsync(default);

            Assert.Single(await db.Users.ToListAsync());
            Assert.Equal(originalRecord, (await db.Credentials.SingleAsync()).Material);

            var user = await db.Users.SingleAsync();

            db.Attributes.Add(new UserAttributeEntity
            {
                TenantId = TenantId,
                UserId = user.Id,
                Name = AttributeName,
                Value = Value
            });
            await db.SaveChangesAsync();
            tenant.Attributes.Clear();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                schema.CheckAsync(default));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                schema.ApproveAsync(StaleFingerprint, default));

            var fingerprint = await schema.PlanAsync(default);

            await schema.ApproveAsync(fingerprint, default);
            await schema.CheckAsync(default);

            Assert.Empty(await db.Attributes.AsNoTracking().ToListAsync());
        }
        finally
        {
            Environment.SetEnvironmentVariable(SecretEnvironment, null);
        }
    }
}
