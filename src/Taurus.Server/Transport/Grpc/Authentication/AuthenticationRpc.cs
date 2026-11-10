using FluentValidation;
using Grpc.Core;
using MediatR;
using Taurus.Application.Features.Authentication;
using Taurus.Application.Features.Authentication.Login;
using Taurus.Application.Features.Authentication.BeginLogin;
using Taurus.Core.Authentication.Keys;
using Taurus.Application.Features.Authentication.Logout;
using Taurus.Application.Features.Sessions;
using Taurus.Application.Features.Sessions.CurrentSession;
using Taurus.Core.Authentication;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Server.Security;
using Taurus.Shared;

namespace Taurus.Server.Transport.Grpc.Authentication;

[BindServiceMethod(typeof(StrictServiceBinder), nameof(StrictServiceBinder.BindService))]
public sealed class AuthenticationRpc(
    ISender sender,
    Settings settings,
    IAuthenticationKeys keys,
    ILogger<AuthenticationRpc> logger) : Taurus.Shared.Authentication.AuthenticationBase
{
    private const int PrimaryErrorIndex = 0;

    public override async Task<BeginLoginResponse> BeginLogin(
        BeginLoginRequest request,
        ServerCallContext context)
    {
        var http = context.GetHttpContext();
        var tenant = Resolve(http);

        BrowserSecurity.RequireOrigin(http);

        var result = await sender.Send(new BeginLoginCommand(tenant.Id,
            BrowserSecurity.Origin(http), request.Username, request.Request),
            context.CancellationToken);

        http.Response.Cookies.Append(BrowserSecurity.CsrfCookieName(http), result.Id,
            BrowserSecurity.Cookie(http, DateTimeOffset.UtcNow
                + settings.Opaque.ChallengeLifetime));

        return new BeginLoginResponse
        {
            Response = result.Response,
            CsrfSeed = keys.CsrfSeed(tenant.Id, BrowserSecurity.Origin(http),
                Guid.Empty, result.Id)
        };
    }

    public override async Task<SessionResponse> Login(
        LoginRequest request,
        ServerCallContext context)
    {
        var http = context.GetHttpContext();
        var tenant = Resolve(http);

        var challenge = http.Request.Cookies[BrowserSecurity.CsrfCookieName(http)]
            ?? string.Empty;

        BrowserSecurity.RequireCsrf(http, keys.CsrfSeed(tenant.Id,
            BrowserSecurity.Origin(http), Guid.Empty, challenge));

        try
        {
            var result = await sender.Send(new LoginCommand(
                tenant.Id,
                BrowserSecurity.Origin(http),
                request.Username,
                challenge,
                request.Request), context.CancellationToken);

            if (result.IsFailed)
            {
                var unavailable = result.Errors[PrimaryErrorIndex]
                    is AuthenticationError error
                    && error.Failure == AuthenticationFailure.Unavailable;

                throw new RpcException(new Status(unavailable
                    ? StatusCode.Unavailable : StatusCode.Unauthenticated,
                    "Authentication could not be completed"));
            }

            http.Response.Cookies.Append(
                BrowserSecurity.SessionCookieName(http), result.Value,
                BrowserSecurity.Cookie(http, DateTimeOffset.UtcNow
                    + settings.Sessions.AbsoluteLifetime));

            http.Response.Cookies.Delete(BrowserSecurity.CsrfCookieName(http),
                BrowserSecurity.Cookie(http));

            var identity = await sender.Send(new CurrentSessionQuery(tenant.Id,
                BrowserSecurity.Origin(http), result.Value), context.CancellationToken);

            if (identity is null)
                throw new RpcException(new Status(StatusCode.Unavailable,
                    "Authentication could not be completed"));

            return Response(tenant.Id, http, identity);
        }
        catch (ValidationException)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                "Request could not be accepted"));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("Authentication operation failed");

            throw new RpcException(new Status(StatusCode.Unavailable,
                "Authentication could not be completed"));
        }
    }

    public override async Task<SessionResponse> CurrentSession(
        EmptyRequest request,
        ServerCallContext context)
    {
        var http = context.GetHttpContext();
        var tenant = Resolve(http);
        var identity = await sender.Send(new CurrentSessionQuery(
            tenant.Id, BrowserSecurity.Origin(http), BrowserSecurity.ReadSession(http)),
            context.CancellationToken);

        if (identity is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated,
                "Authentication required"));

        return Response(tenant.Id, http, identity);
    }

    public override async Task<EmptyResponse> Logout(
        EmptyRequest request,
        ServerCallContext context)
    {
        var http = context.GetHttpContext();
        var tenant = Resolve(http);

        var identity = await sender.Send(new CurrentSessionQuery(tenant.Id,
            BrowserSecurity.Origin(http), BrowserSecurity.ReadSession(http)),
            context.CancellationToken);

        if (identity is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated,
                "Authentication required"));

        BrowserSecurity.RequireCsrf(http, keys.CsrfSeed(tenant.Id,
            BrowserSecurity.Origin(http), identity.UserId,
            identity.SessionId.ToString()));

        var revoked = await sender.Send(new LogoutCommand(tenant.Id,
            BrowserSecurity.Origin(http), BrowserSecurity.ReadSession(http)),
            context.CancellationToken);

        if (!revoked)
            throw new RpcException(new Status(StatusCode.Unavailable,
                "Operation could not be completed"));

        http.Response.Cookies.Delete(
            BrowserSecurity.SessionCookieName(http), BrowserSecurity.Cookie(http));

        return new EmptyResponse();
    }

    private SessionResponse Response(
        string tenantId,
        HttpContext http,
        Taurus.Core.Sessions.SessionIdentity identity) => new()
    {
        UserId = identity.UserId.ToString(),
        Username = identity.Username,
        Locale = identity.Locale,
        Timezone = identity.Timezone,
        SessionId = identity.SessionId.ToString(),
        CsrfSeed = keys.CsrfSeed(tenantId, BrowserSecurity.Origin(http),
            identity.UserId, identity.SessionId.ToString())
    };

    private Tenant Resolve(HttpContext context) =>
        settings.Resolve(BrowserSecurity.Origin(context))
        ?? throw new RpcException(new Status(StatusCode.NotFound,
            "Unknown application"));
}
