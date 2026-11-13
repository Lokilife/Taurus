using FluentValidation;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Application.Behaviors;
using Taurus.Application.Features.Administration.Authorize;
using Taurus.Application.Features.Authentication;
using Taurus.Application.Features.Sessions;
using Taurus.Core.Administration;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Infrastructure.Authentication.Keys;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Infrastructure.Persistence.Administration;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Authentication;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;
using Taurus.Infrastructure.Provisioning;
using Taurus.Server.Security;
using Taurus.Server.Transport.Grpc.Administration;

namespace Taurus.Tests.tests;

public sealed class AdministrationTests
{
    private const string ConnectionString = "Data Source=:memory:";
    private const string TenantId = "tenant";
    private const string OtherTenantId = "other";
    private const string ApplicationOrigin = "https://app.localhost";
    private const string AdminOrigin = "https://admin.localhost";
    private const string OtherOrigin = "https://other.localhost";
    private const string OtherApplicationOrigin = "https://other-app.localhost";
    private const string Password = "bootstrap-test-password";
    private const string OtherUsername = "ordinary";
    private const string RootToken = "root-test-session";
    private const string OtherToken = "ordinary-test-session";
    private const string RejectAuditInsert =
        "CREATE TRIGGER reject_audit BEFORE INSERT ON UserLogs "
        + "BEGIN SELECT RAISE(ABORT, 'test audit failure'); END";
    private const int LifetimeDays = 1;

    [Fact]
    public async Task BootstrapBindingIsStableAndCannotFollowAnotherUsername()
    {
        await using var db = await CreateAsync();
        var settings = Settings();
        var bootstrap = Bootstrap(db, settings);

        await bootstrap.RunAsync(default);

        var root = await db.TenantRoots.SingleAsync();
        var original = (await db.Credentials.SingleAsync()).Material;

        await bootstrap.RunAsync(default);

        Assert.Equal(root.UserId, (await db.TenantRoots.SingleAsync()).UserId);
        Assert.Equal(original, (await db.Credentials.SingleAsync()).Material);
        Assert.Single(await db.UserLogs.Where(
            x => x.Action == AuditAction.BootstrapRootBound).ToListAsync());

        var ordinary = User(OtherUsername);

        db.Users.Add(ordinary);
        await db.SaveChangesAsync();
        settings.Tenants[default(int)].RootUsername = OtherUsername;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bootstrap.RunAsync(default));
        Assert.Equal(root.UserId, (await db.TenantRoots.SingleAsync()).UserId);
        Assert.NotEqual(ordinary.Id, root.UserId);
    }

    [Fact]
    public async Task LegacyBindingPreservesDisabledStatusAndCredentials()
    {
        await using var db = await CreateAsync();
        var bootstrap = Bootstrap(db, Settings());

        await bootstrap.RunAsync(default);

        var user = await db.Users.SingleAsync();
        var original = (await db.Credentials.SingleAsync()).Material;

        user.Status = UserStatus.Disabled;
        await db.SaveChangesAsync();
        await db.TenantRoots.ExecuteDeleteAsync();
        db.ChangeTracker.Clear();
        await bootstrap.RunAsync(default);

        Assert.Equal(user.Id, (await db.TenantRoots.SingleAsync()).UserId);
        Assert.Equal(UserStatus.Disabled, (await db.Users.SingleAsync()).Status);
        Assert.Equal(original, (await db.Credentials.SingleAsync()).Material);
    }

    [Fact]
    public async Task MissingOrAmbiguousLegacyEvidenceCannotGrantRoot()
    {
        await using var db = await CreateAsync();
        var settings = Settings();
        var user = User(settings.Tenants[default(int)].RootUsername);
        var bootstrap = Bootstrap(db, settings);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bootstrap.RunAsync(default));
        Assert.Empty(await db.TenantRoots.ToListAsync());

        db.UserLogs.Add(BootstrapAudit(user.Id));
        db.UserLogs.Add(BootstrapAudit(Guid.NewGuid()));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bootstrap.RunAsync(default));
        Assert.Empty(await db.TenantRoots.ToListAsync());

        settings.Tenants[default(int)].RootUsername = OtherUsername;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bootstrap.RunAsync(default));
        Assert.Single(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task BindingRequiresAuditPersistence()
    {
        await using var db = await CreateAsync();

        await db.Database.ExecuteSqlRawAsync(RejectAuditInsert);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Bootstrap(db, Settings()).RunAsync(default));
        Assert.Empty(await db.TenantRoots.AsNoTracking().ToListAsync());
        Assert.Empty(await db.Users.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AuthorizationRequiresDedicatedOriginAndCurrentRootSession()
    {
        await using var db = await CreateAsync();
        var settings = Settings();

        await Bootstrap(db, settings).RunAsync(default);

        var root = await db.Users.SingleAsync();
        var ordinary = User(OtherUsername);

        db.Users.Add(ordinary);
        db.Sessions.Add(Session(root.Id, RootToken));
        db.Sessions.Add(Session(ordinary.Id, OtherToken));
        await db.SaveChangesAsync();

        using var services = Services(db);
        var boundary = new AdministrationAuthorization(settings,
            services.GetRequiredService<ISender>());

        Assert.Equal(root.Id, (await boundary.RequireAsync(
            Http(AdminOrigin, RootToken), default)).UserId);
        await Denied(boundary, ApplicationOrigin, RootToken, StatusCode.PermissionDenied);
        await Denied(boundary, OtherOrigin, RootToken, StatusCode.PermissionDenied);
        await Denied(boundary, AdminOrigin, string.Empty, StatusCode.Unauthenticated);
        await Denied(boundary, AdminOrigin, OtherToken, StatusCode.PermissionDenied);

        var binding = await db.TenantRoots.SingleAsync();

        db.TenantRoots.Remove(binding);
        await db.SaveChangesAsync();

        await Denied(boundary, AdminOrigin, RootToken, StatusCode.PermissionDenied);

        db.TenantRoots.Add(binding);
        root.Status = UserStatus.Disabled;
        await db.SaveChangesAsync();

        await Denied(boundary, AdminOrigin, RootToken, StatusCode.Unauthenticated);

        root.Status = UserStatus.Enabled;
        await db.Sessions.ExecuteUpdateAsync(update =>
            update.SetProperty(x => x.Revoked, true));
        await db.SaveChangesAsync();

        await Denied(boundary, AdminOrigin, RootToken, StatusCode.Unauthenticated);
        Assert.False(await new AdministrationRepo(db).IsRootAsync(
            OtherTenantId, root.Id, default));
    }

    [Fact]
    public async Task RootIdentityAndSessionsCannotCrossTenants()
    {
        await using var db = await CreateAsync();
        var settings = Settings();

        settings.Tenants.Add(new Tenant
        {
            Id = OtherTenantId,
            Issuer = OtherApplicationOrigin,
            ApplicationOrigins = [OtherApplicationOrigin],
            AdminOrigin = OtherOrigin,
            RootPassword = new SecretReference { Value = Password }
        });
        settings.Validate();
        await Bootstrap(db, settings).RunAsync(default);

        var rootId = (await db.TenantRoots.SingleAsync(
            x => x.TenantId == TenantId)).UserId;
        var ordinary = User(OtherUsername);
        var session = Session(rootId, OtherToken);

        ordinary.Id = rootId;
        ordinary.TenantId = OtherTenantId;
        session.TenantId = OtherTenantId;
        session.Origin = OtherOrigin;
        db.Users.Add(ordinary);
        db.Sessions.Add(Session(rootId, RootToken));
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        using var services = Services(db);
        var boundary = new AdministrationAuthorization(settings,
            services.GetRequiredService<ISender>());

        Assert.Equal(rootId, (await boundary.RequireAsync(
            Http(AdminOrigin, RootToken), default)).UserId);
        await Denied(boundary, OtherOrigin, RootToken, StatusCode.Unauthenticated);
        await Denied(boundary, OtherOrigin, OtherToken, StatusCode.PermissionDenied);
    }

    private static async Task Denied(
        AdministrationAuthorization boundary,
        string origin,
        string token,
        StatusCode code)
    {
        var error = await Assert.ThrowsAsync<RpcException>(() =>
            boundary.RequireAsync(Http(origin, token), default));

        Assert.Equal(code, error.StatusCode);
    }

    private static async Task<SqliteDatabase> CreateAsync()
    {
        var db = new SqliteDatabase(new DbContextOptionsBuilder<SqliteDatabase>()
            .UseSqlite(ConnectionString).Options);

        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        return db;
    }

    private static Settings Settings()
    {
        var settings = new Settings
        {
            NoRateLimits = true,
            Database = new SecretReference { Value = ConnectionString },
            Keys = new()
            {
                Csrf = new SecretReference { Value = KeyBootstrap.GenerateCsrfKey() }
            },
            Opaque = new()
            {
                Setup = new SecretReference { Value = KeyBootstrap.GenerateOpaqueSetup() }
            },
            Tenants = [new Tenant
            {
                Id = TenantId,
                Issuer = ApplicationOrigin,
                ApplicationOrigins = [ApplicationOrigin],
                AdminOrigin = AdminOrigin,
                RootPassword = new SecretReference { Value = Password }
            }]
        };

        settings.Validate();

        return settings;
    }

    private static Bootstrapper Bootstrap(SqliteDatabase db, Settings settings) =>
        new(db, settings, new OpaqueService(settings.Opaque, TimeProvider.System),
            new UserLogRepo(db, NullLogger<UserLogRepo>.Instance));

    private static UserEntity User(string username) => new()
    {
        TenantId = TenantId,
        Id = Guid.NewGuid(),
        Username = username,
        Status = UserStatus.Enabled,
        Version = Guid.NewGuid()
    };

    private static SessionEntity Session(Guid userId, string token) => new()
    {
        TenantId = TenantId,
        UserId = userId,
        Origin = AdminOrigin,
        TokenDigest = SessionTokens.Digest(token),
        IdleExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromDays(LifetimeDays),
        AbsoluteExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromDays(LifetimeDays)
    };

    private static UserLogEntity BootstrapAudit(Guid userId) => new()
    {
        TenantId = TenantId,
        Id = Guid.NewGuid(),
        ActorId = userId,
        Action = AuditAction.Bootstrap,
        Severity = UserLogSeverity.High,
        Succeeded = true,
        Timestamp = DateTimeOffset.UtcNow
    };

    private static DefaultHttpContext Http(string origin, string token)
    {
        var uri = new Uri(origin);
        var http = new DefaultHttpContext();

        http.Request.Scheme = uri.Scheme;
        http.Request.Host = HostString.FromUriComponent(uri.Authority);
        http.Request.Headers.Cookie = BrowserSecurity.SessionCookie + "=" + token;

        return http;
    }

    private static ServiceProvider Services(SqliteDatabase db)
    {
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton(new AuthenticationOptions());
        services.AddSingleton<IAuthRepo>(new AuthRepo(db,
            new UserLogRepo(db, NullLogger<UserLogRepo>.Instance)));
        services.AddSingleton<IAdministrationRepo>(new AdministrationRepo(db));
        services.AddMediatR(configuration => configuration
            .RegisterServicesFromAssemblyContaining<AuthorizeAdministrationQuery>());
        services.AddTransient<IValidator<AuthorizeAdministrationQuery>,
            AuthorizeAdministrationValidator>();
        services.AddTransient(
            typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services.BuildServiceProvider();
    }
}
