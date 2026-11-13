using Taurus.Infrastructure.Configuration.Secrets;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Taurus.Application.Behaviors;
using Taurus.Application.Features.Authentication.BeginLogin;
using Taurus.Application.Features.Authentication.Login;
using Taurus.Application.Features.Provisioning.Bootstrap;
using Taurus.Application.Features.Schemas.ApplyCompatible;
using Taurus.Application.Features.Schemas.Approve;
using Taurus.Application.Features.Schemas.Plan;
using Taurus.Application.Features.Sessions.CurrentSession;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication.Keys;
using Taurus.Core.Authentication;
using Taurus.Core.Credentials.Opaque;
using Taurus.Core.Credentials;
using Taurus.Core.Provisioning;
using Taurus.Core.Users.Attributes;
using Taurus.Infrastructure.Authentication.Keys;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Infrastructure.Configuration.Persistence;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Logging;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Authentication;
using Taurus.Infrastructure.Persistence.Providers.PostgreSql;
using Taurus.Infrastructure.Persistence.Providers.Sqlite;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Provisioning;
using Taurus.Infrastructure.Schemas;
using Taurus.Server.Auditing;
using Taurus.Server.Configuration;
using Taurus.Server.Security;
using Taurus.Server.Transport.Grpc.Authentication;
using Taurus.Server.Transport.Grpc.Interceptors;

namespace Taurus.Server;

public static class Program
{
    public static async Task Main(string[] args)
    {
        const string migrateCommand = "migrate";
        const string bootstrapCommand = "bootstrap";
        const string schemaPlanCommand = "schema-plan";
        const string schemaApproveCommand = "schema-approve";
        const int commandArgument = 0;
        const int fingerprintArgument = 1;
        const int maximumMessageBytes = 8192;
        const int standaloneArgumentCount = 1;
        const string opaqueSetupCommand = "generate-opaque-setup";
        const string csrfKeyCommand = "generate-csrf-key";

        if (args.Length == standaloneArgumentCount)
        {
            if (args[commandArgument] == opaqueSetupCommand)
            {
                Console.WriteLine(KeyBootstrap.GenerateOpaqueSetup());
                return;
            }

            if (args[commandArgument] == csrfKeyCommand)
            {
                Console.WriteLine(KeyBootstrap.GenerateCsrfKey());
                return;
            }
        }

        var configurationPath = ConfigDiscovery.Resolve(ref args);
        var settings = Settings.Load(configurationPath);
        var connection = settings.Database.Resolve(SecretReferencePaths.Database);
        var builder = WebApplication.CreateBuilder(args);

        if (settings.ListenUrls.Length != default)
            builder.WebHost.UseUrls(settings.ListenUrls);

        if (settings.Http.CertificateFile is not null)
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                settings.Http.CertificateFile,
                settings.Http.CertificatePassword?.Resolve(
                    SecretReferencePaths.CertificatePassword));

            builder.WebHost.ConfigureKestrel(options =>
                options.ConfigureHttpsDefaults(https =>
                    https.ServerCertificate = certificate));
        }

        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(settings.ServiceLogs);
        builder.Services.AddSingleton<ServiceLogFiles>();
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        builder.Logging.SetMinimumLevel(settings.ServiceLogs.MinimumLevel);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
        builder.Logging.AddFilter("Grpc", LogLevel.Warning);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(settings.Sessions);
        builder.Services.AddSingleton(settings.Opaque);
        builder.Services.AddSingleton(settings.Keys);
        builder.Services.AddSingleton<IAuthenticationKeys, AuthenticationKeys>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IOpaqueService, OpaqueService>();
        builder.Services.AddScoped<IdentityDatabase>(_ => settings.Provider switch
        {
            DatabaseProvider.Sqlite => new SqliteDatabase(
                new DbContextOptionsBuilder<SqliteDatabase>()
                    .UseSqlite(connection).Options),
            DatabaseProvider.PostgreSql => new PostgreSqlDatabase(
                new DbContextOptionsBuilder<PostgreSqlDatabase>()
                    .UseNpgsql(connection).Options),
            _ => throw new InvalidOperationException("Unsupported provider")
        });
        builder.Services.AddScoped<IUserLogRepo, UserLogRepo>();
        builder.Services.AddScoped<IAuthRepo, AuthRepo>();
        builder.Services.AddScoped<SchemaManager>();
        builder.Services.AddScoped<Bootstrapper>();
        builder.Services.AddScoped<IBootstrapService>(services =>
            services.GetRequiredService<Bootstrapper>());
        builder.Services.AddScoped<ISchemaService>(services =>
            services.GetRequiredService<SchemaManager>());
        builder.Services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<LoginCommand>());
        builder.Services.AddTransient<IValidator<LoginCommand>, LoginValidator>();
        builder.Services.AddTransient<IValidator<BeginLoginCommand>,
            BeginLoginValidator>();
        builder.Services.AddTransient<IValidator<ApproveSchemaCommand>,
            ApproveSchemaValidator>();
        builder.Services.AddTransient(
            typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = maximumMessageBytes;
            options.MaxSendMessageSize = maximumMessageBytes;
            options.EnableDetailedErrors = false;
            options.Interceptors.Add<SafeRpcInterceptor>();
        });
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var proxy in settings.TrustedProxies)
                options.KnownProxies.Add(IPAddress.Parse(proxy));
        });

        var app = builder.Build();
        app.Services.GetRequiredService<IAuthenticationKeys>();

        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(Program).FullName!);

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDatabase>();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            if (args.Length > commandArgument)
            {
                switch (args[commandArgument])
                {
                    case migrateCommand:
                        await db.Database.MigrateAsync();
                        return;
                    case bootstrapCommand:
                        await sender.Send(new BootstrapCommand());
                        return;
                    case schemaPlanCommand:
                        Console.WriteLine(await sender.Send(new PlanSchemaQuery()));
                        return;
                    case schemaApproveCommand when args.Length > fingerprintArgument:
                        await sender.Send(new ApproveSchemaCommand(
                            args[fingerprintArgument]));
                        return;
                }
            }

            if (db.Database.HasPendingModelChanges())
                throw new InvalidOperationException("Database model requires migration");

            foreach (var migration in await db.Database.GetPendingMigrationsAsync())
                throw new InvalidOperationException("Run migrate before serving");

            await sender.Send(new ApplyCompatibleSchemaCommand());
            await sender.Send(new BootstrapCommand());
        }

        var webRoot = app.Environment.WebRootPath
            ?? Path.Combine(app.Environment.ContentRootPath,
                BrowserSecurity.WebRootDirectory);

        if (!File.Exists(Path.Combine(webRoot, BrowserSecurity.IndexFile)))
            throw new InvalidOperationException("Build Taurus.WebClient before serving");

        if (settings.TrustedProxies.Count != default)
            app.UseForwardedHeaders();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = BrowserSecurity.NoStore;
            context.Response.Headers.XContentTypeOptions = BrowserSecurity.NoSniff;
            context.Response.Headers.ContentSecurityPolicy =
                BrowserSecurity.FramePolicy;

            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await next(context);
                return;
            }

            var origin = BrowserSecurity.Origin(context);

            try
            {
                if ((!context.Request.IsHttps
                        && !(settings.Http.AllowsDevelopmentOrigin(new Uri(origin))
                            && context.Connection.RemoteIpAddress is { } remote
                            && IPAddress.IsLoopback(remote)))
                    || settings.Resolve(origin) is null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                if (string.Equals(context.Request.Path.Value,
                    settings.Http.RequestPrefix + "/ui", StringComparison.Ordinal))
                {
                    context.Response.Redirect(settings.Http.RequestPrefix + "/ui/");
                    return;
                }

                await next(context);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError("Request processing failed");

                if (!context.Response.HasStarted)
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }
            finally
            {
                await ServiceAudit.WriteAsync(context, app.Services, logger);
            }
        });

        app.UseRouting();
        app.UseGrpcWeb();
        app.MapGet("/health/live", () => Results.Ok());
        app.MapGet("/health/ready", async (IdentityDatabase db, SchemaManager schemas) =>
        {
            try
            {
                if (!await db.Database.CanConnectAsync())
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

                foreach (var migration in await db.Database.GetPendingMigrationsAsync())
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

                await schemas.CheckAsync(CancellationToken.None);

                return Results.Ok();
            }
            catch
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        });

        app.MapGet(settings.Http.RequestPrefix + "/auth",
            async (HttpContext http, ISender sender) =>
        {
            var origin = BrowserSecurity.Origin(http);
            var tenant = settings.Resolve(origin)!;
            var identity = await sender.Send(new CurrentSessionQuery(
                tenant.Id, origin, BrowserSecurity.ReadSession(http)),
                http.RequestAborted);

            if (identity is null)
                return Results.StatusCode(StatusCodes.Status401Unauthorized);

            http.Response.Headers[BrowserSecurity.UserIdHeader] =
                identity.UserId.ToString();
            http.Response.Headers[BrowserSecurity.UsernameHeader] =
                Uri.EscapeDataString(identity.Username);
            http.Response.Headers[BrowserSecurity.TenantHeader] =
                Uri.EscapeDataString(tenant.Id);

            return Results.NoContent();
        });

        app.MapGroup(settings.Http.RequestPrefix)
            .MapGrpcService<AuthenticationRpc>().EnableGrpcWeb();
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = settings.Http.RequestPrefix + "/ui"
        });
        app.MapFallbackToFile(settings.Http.RequestPrefix + "/ui/{*path:nonfile}",
            BrowserSecurity.IndexFile);
        app.MapGet("/", () => Results.Redirect(settings.Http.RequestPrefix + "/ui/"));

        await app.RunAsync();
    }
}
