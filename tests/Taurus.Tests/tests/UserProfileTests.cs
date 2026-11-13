using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Taurus.Application.Features.Sessions;
using Taurus.Application.Features.Users.CurrentUser;
using Taurus.Core.Groups;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Infrastructure.Configuration.Secrets;
using Taurus.Infrastructure.Configuration.Tenants;

namespace Taurus.Tests.tests;

public sealed class UserProfileTests
{
    private const string TenantId = "tenant";
    private const string OtherTenantId = "other";
    private const string Origin = "https://app.localhost";
    private const string AdminOrigin = "https://admin.localhost";
    private const string Username = "root";
    private const string OtherUsername = "ordinary";
    private const string Password = "profile-test-password";
    private const string GroupName = "operators";
    private const string OtherGroupName = "other-tenant-group";
    private const string Token = "test-profile-session";
    private const string AppToken = "test-profile-app-session";
    private const int LifetimeDays = 1;

    [Fact]
    public async Task ProfileReturnsScopedGroupsAndEffectivePermissions()
    {
        await using var db = await TestDatabase.CreateAsync();
        var settings = TestSettings.Create(new Tenant
        {
            Id = TenantId,
            Issuer = Origin,
            ApplicationOrigins = [Origin],
            AdminOrigin = AdminOrigin,
            RootPassword = new SecretReference { Value = Password }
        });
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        db.Users.Add(new UserEntity
        {
            TenantId = TenantId,
            Id = userId,
            Username = Username,
            Root = true,
            Status = UserStatus.Enabled
        });
        db.Users.Add(new UserEntity
        {
            TenantId = OtherTenantId,
            Id = userId,
            Username = OtherUsername,
            Status = UserStatus.Enabled
        });
        db.Set<GroupEntity>().AddRange(
            new GroupEntity { TenantId = TenantId, Id = groupId, Name = GroupName },
            new GroupEntity
            {
                TenantId = OtherTenantId,
                Id = groupId,
                Name = OtherGroupName
            });
        db.Set<GroupMembershipEntity>().AddRange(
            new GroupMembershipEntity
            {
                TenantId = TenantId,
                GroupId = groupId,
                UserId = userId
            },
            new GroupMembershipEntity
            {
                TenantId = OtherTenantId,
                GroupId = groupId,
                UserId = userId
            });
        db.Sessions.Add(Session(userId, AdminOrigin, Token));
        db.Sessions.Add(Session(userId, Origin, AppToken));
        await db.SaveChangesAsync();

        using var application = TestApplication.Create(db, settings);
        var sender = application.GetRequiredService<ISender>();
        var admin = await sender.Send(new CurrentUserQuery(TenantId, AdminOrigin, Token));

        Assert.NotNull(admin);
        Assert.True(admin.CanManageUsers);
        Assert.Equal(userId, admin.User.Id);
        Assert.Equal(Username, admin.User.Username);
        Assert.Equal(GroupName, Assert.Single(admin.User.Groups).Name);

        var app = await sender.Send(new CurrentUserQuery(TenantId, Origin, AppToken));

        Assert.NotNull(app);
        Assert.False(app.CanManageUsers);
        Assert.Equal(admin.User.Id, app.User.Id);
        Assert.Null(await sender.Send(new CurrentUserQuery(
            OtherTenantId, AdminOrigin, Token)));

        var user = await db.Users.SingleAsync(x => x.TenantId == TenantId);

        user.Root = false;
        await db.SaveChangesAsync();

        var ordinary = await sender.Send(new CurrentUserQuery(
            TenantId, AdminOrigin, Token));

        Assert.NotNull(ordinary);
        Assert.False(ordinary.CanManageUsers);

        user.Status = UserStatus.Disabled;
        await db.SaveChangesAsync();

        Assert.Null(await sender.Send(new CurrentUserQuery(
            TenantId, AdminOrigin, Token)));
    }

    private static SessionEntity Session(
        Guid userId, string origin, string token) => new()
    {
        TenantId = TenantId,
        UserId = userId,
        Origin = origin,
        TokenDigest = SessionTokens.Digest(token),
        IdleExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromDays(LifetimeDays),
        AbsoluteExpiresAt = DateTimeOffset.UtcNow + TimeSpan.FromDays(LifetimeDays)
    };
}
