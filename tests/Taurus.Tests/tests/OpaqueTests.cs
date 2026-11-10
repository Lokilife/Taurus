using OPAQUE.Net;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Infrastructure.Configuration.Secrets;

namespace Taurus.Tests.tests;

public class OpaqueTests
{
    private const string Password = "interoperability-test-password";
    private const string WrongPassword = "incorrect-password";
    private const string Tenant = "tenant";
    private const string OtherTenant = "other";
    private const string Origin = "https://app.localhost";
    private const string Username = "root";
    private const int Capacity = 1;
    private const int ExpiredMinutes = 3;

    [Fact]
    public void ProofCreatesOneSessionAndIsBoundToTenant()
    {
        var service = Create();
        var record = service.Enroll(OpaqueService.Identifier(Tenant, Username), Password);
        var client = new OpaqueClient();

        Assert.True(client.StartLogin(Password, out var start));
        Assert.NotNull(start);

        var challenge = service.Begin(Tenant, Origin, Username, record,
            start.StartLoginRequest);

        Assert.True(client.FinishLogin(start.ClientLoginState, challenge.Response,
            Password, out var finish));
        Assert.NotNull(finish);
        Assert.True(service.Finish(Tenant, Origin, Username, record,
            challenge.Id, finish.FinishLoginRequest));
        Assert.False(service.Finish(Tenant, Origin, Username, record,
            challenge.Id, finish.FinishLoginRequest));

        var second = service.Begin(Tenant, Origin, Username, record,
            start.StartLoginRequest);

        Assert.False(service.Finish(OtherTenant, Origin, Username, record,
            second.Id, finish.FinishLoginRequest));
    }

    [Fact]
    public void WrongPasswordsAndMissingRecordsFailWithoutExposingExistence()
    {
        var service = Create();
        var record = service.Enroll(OpaqueService.Identifier(Tenant, Username), Password);
        var client = new OpaqueClient();

        Assert.True(client.StartLogin(WrongPassword, out var start));
        Assert.NotNull(start);

        var challenge = service.Begin(Tenant, Origin, Username, record,
            start.StartLoginRequest);

        Assert.False(client.FinishLogin(start.ClientLoginState, challenge.Response,
            WrongPassword, out _));
        Assert.False(service.Finish(Tenant, Origin, Username, record,
            challenge.Id, string.Empty));

        var missing = service.Begin(Tenant, Origin, Username, null,
            start.StartLoginRequest);

        Assert.Equal(challenge.Response.Length, missing.Response.Length);
        Assert.False(client.FinishLogin(start.ClientLoginState, missing.Response,
            WrongPassword, out _));
    }

    [Fact]
    public void PendingWorkIsBounded()
    {
        var service = Create();
        var client = new OpaqueClient();

        Assert.True(client.StartLogin(Password, out var start));
        Assert.NotNull(start);

        service.Begin(Tenant, Origin, Username, null, start.StartLoginRequest);

        Assert.Throws<InvalidOperationException>(() => service.Begin(
            Tenant, Origin, Username, null, start.StartLoginRequest));
    }

    [Fact]
    public void ExpiredChallengesAreRejectedAndCapacityIsReclaimed()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var service = Create(clock);
        var client = new OpaqueClient();

        Assert.True(client.StartLogin(Password, out var start));
        Assert.NotNull(start);

        var first = service.Begin(Tenant, Origin, Username, null,
            start.StartLoginRequest);

        clock.Advance(TimeSpan.FromMinutes(ExpiredMinutes));

        Assert.False(service.Finish(Tenant, Origin, Username, null,
            first.Id, string.Empty));

        service.Begin(Tenant, Origin, Username, null, start.StartLoginRequest);
        clock.Advance(TimeSpan.FromMinutes(ExpiredMinutes));
        service.Begin(Tenant, Origin, Username, null, start.StartLoginRequest);
    }

    private static OpaqueService Create(TimeProvider? clock = null)
    {
        Assert.True(new OpaqueServer().CreateSetup(out var setup));

        return new OpaqueService(new OpaqueOptions
        {
            Setup = new SecretReference { Value = setup },
            MaximumPendingLogins = Capacity
        }, clock ?? TimeProvider.System);
    }
}
