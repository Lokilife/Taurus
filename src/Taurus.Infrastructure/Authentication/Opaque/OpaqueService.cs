using Taurus.Infrastructure.Configuration.Secrets;
using System.Text.Json;
using OPAQUE.Net;
using Taurus.Core.Credentials.Opaque;
using Taurus.Application.Features.Sessions;

namespace Taurus.Infrastructure.Authentication.Opaque;

public sealed class OpaqueService : IOpaqueService
{
    private const string LibraryExceptionNamespace = "OPAQUE.Net.Types.Exceptions";

    private readonly OpaqueOptions options;
    private readonly TimeProvider clock;
    private readonly string setup;
    private readonly object gate = new();
    private readonly Dictionary<string, PendingLogin> pending;

    public OpaqueService(OpaqueOptions options, TimeProvider clock)
    {
        this.options = options;
        this.clock = clock;
        setup = options.Setup.Resolve(SecretReferencePaths.OpaqueSetup);
        pending = new Dictionary<string, PendingLogin>(options.MaximumPendingLogins);
    }

    public string Enroll(string identifier, string password)
    {
        var client = new OpaqueClient();
        var server = new OpaqueServer();

        if (!client.StartRegistration(password, out var start) || start is null
            || !server.CreateRegistrationResponse(setup, identifier,
                start.RegistrationRequest, out var response) || response is null
            || !client.FinishRegistration(password, response,
                start.ClientRegistrationState, out var finish) || finish is null)
            throw new InvalidOperationException("OPAQUE enrollment failed");

        return finish.RegistrationRecord;
    }

    public OpaqueChallenge Begin(
        string tenantId,
        string origin,
        string username,
        string? record,
        string request)
    {
        var now = clock.GetUtcNow();

        lock (gate)
        {
            foreach (var pair in pending)
            {
                if (pair.Value.ExpiresAt <= now)
                    pending.Remove(pair.Key);
            }

            if (pending.Count >= options.MaximumPendingLogins)
                throw new InvalidOperationException("Authentication capacity reached");

            var server = new OpaqueServer();
            var identifier = Identifier(tenantId, username);

            if (!server.StartLogin(setup, request, identifier, record,
                out var result, out _) || result is null)
                throw new InvalidOperationException("Invalid OPAQUE request");

            var id = SessionTokens.Create();

            pending.Add(id, new PendingLogin(tenantId, origin, username,
                record, result.ServerLoginState, now + options.ChallengeLifetime));

            return new OpaqueChallenge(id, result.LoginResponse);
        }
    }

    public bool Finish(
        string tenantId,
        string origin,
        string username,
        string? record,
        string challengeId,
        string request)
    {
        PendingLogin? login;

        lock (gate)
        {
            if (!pending.Remove(challengeId, out login))
                return false;
        }

        if (login.ExpiresAt <= clock.GetUtcNow() || login.TenantId != tenantId
            || login.Origin != origin || login.Username != username
            || login.Record != record)
            return false;

        try
        {
            return new OpaqueServer().FinishLogin(login.State, request, out _);
        }
        catch (Exception exception) when (
            exception.GetType().Namespace == LibraryExceptionNamespace
            || exception is ArgumentException or FormatException)
        {
            return false;
        }
    }

    public static string Identifier(string tenantId, string username) =>
        JsonSerializer.Serialize(new[] { tenantId, username });
}
