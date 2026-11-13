using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Application.Features.Sessions;
using Taurus.Core.Administration;
using Taurus.Core.Auditing;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Infrastructure.Administration;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Infrastructure.Persistence.Administration;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;
using Taurus.Infrastructure.Provisioning;
using Taurus.Server.Security;
using Taurus.Server.Transport.Grpc.Administration;

namespace Taurus.Tests.tests;

public sealed class AdministrationTests
{
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
    public async Task RootMarkerIsStableAndCannotFollowAnotherUsername()
    {
        await using var db = await TestDatabase.CreateAsync();
        var settings = Settings();
        var bootstrap = Bootstrap(db, settings);

        await bootstrap.RunAsync(default);

        var root = await db.Users.SingleAsync(x => x.Root);
        var original = (await db.Credentials.SingleAsync()).Material;

        await bootstrap.RunAsync(default);

        Assert.Equal(root.Id, (await db.Users.SingleAsync(x => x.Root)).Id);
        Assert.Equal(original, (await db.Credentials.SingleAsync()).Material);
        Assert.Single(await db.UserLogs.Where(
            x => x.Action == AuditAction.Bootstrap).ToListAsync());

        var ordinary = User(OtherUsername);

        db.Users.Add(ordinary);
        await db.SaveChangesAsync();
        settings.Tenants[default(int)].RootUsername = OtherUsername;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bootstrap.RunAsync(default));
        Assert.Equal(root.Id, (await db.Users.SingleAsync(x => x.Root)).Id);
        Assert.NotEqual(ordinary.Id, root.Id);
    }

    [Fact]
    public async Task RootCreationRequiresAuditPersistence()
    {
        await using var db = await TestDatabase.CreateAsync();

        await db.Database.ExecuteSqlRawAsync(RejectAuditInsert);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Bootstrap(db, Settings()).RunAsync(default));
        Assert.Empty(await db.Users.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task AuthorizationRequiresDedicatedOriginAndCurrentRootSession()
    {
        await using var db = await TestDatabase.CreateAsync();
        var settings = Settings();

        await Bootstrap(db, settings).RunAsync(default);

        var root = await db.Users.SingleAsync();
        var ordinary = User(OtherUsername);

        db.Users.Add(ordinary);
        db.Sessions.Add(Session(root.Id, RootToken));
        db.Sessions.Add(Session(ordinary.Id, OtherToken));
        await db.SaveChangesAsync();

        using var services = TestApplication.Create(db, settings);
        var boundary = new AdministrationAuthorization(settings,
            services.GetRequiredService<ISender>(),
            new AdministrationOriginPolicy(settings));

        Assert.Equal(root.Id, (await boundary.RequireAsync(
            Http(AdminOrigin, RootToken), default)).UserId);
        await Denied(boundary, ApplicationOrigin, RootToken, StatusCode.PermissionDenied);
        await Denied(boundary, OtherOrigin, RootToken, StatusCode.PermissionDenied);
        await Denied(boundary, AdminOrigin, string.Empty, StatusCode.Unauthenticated);
        await Denied(boundary, AdminOrigin, OtherToken, StatusCode.PermissionDenied);

        root.Root = false;
        await db.SaveChangesAsync();

        await Denied(boundary, AdminOrigin, RootToken, StatusCode.PermissionDenied);

        root.Root = true;
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
        await using var db = await TestDatabase.CreateAsync();
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

        var rootId = (await db.Users.SingleAsync(
            x => x.TenantId == TenantId && x.Root)).Id;
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

        using var services = TestApplication.Create(db, settings);
        var boundary = new AdministrationAuthorization(settings,
            services.GetRequiredService<ISender>(),
            new AdministrationOriginPolicy(settings));

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

    private static Settings Settings() => TestSettings.Create(new Tenant
    {
        Id = TenantId,
        Issuer = ApplicationOrigin,
        ApplicationOrigins = [ApplicationOrigin],
        AdminOrigin = AdminOrigin,
        RootPassword = new SecretReference { Value = Password }
    });

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

    private static DefaultHttpContext Http(string origin, string token)
    {
        var uri = new Uri(origin);
        var http = new DefaultHttpContext();

        http.Request.Scheme = uri.Scheme;
        http.Request.Host = HostString.FromUriComponent(uri.Authority);
        http.Request.Headers.Cookie = BrowserSecurity.SessionCookie + "=" + token;

        return http;
    }
}
