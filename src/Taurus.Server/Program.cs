using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Taurus.Application.Behaviors;
using Taurus.Application.Features.Administration.Authorize;
using Taurus.Application.Features.Users.CurrentUser;
using Taurus.Application.Features.Authentication.BeginLogin;
using Taurus.Application.Features.Authentication.Login;
using Taurus.Application.Features.Provisioning.Bootstrap;
using Taurus.Application.Features.Schemas.ApplyCompatible;
using Taurus.Application.Features.Schemas.Approve;
using Taurus.Application.Features.Schemas.Plan;
using Taurus.Core.Administration;
using Taurus.Core.Users.Profiles;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication.Keys;
using Taurus.Core.Authentication;
using Taurus.Core.Credentials.Opaque;
using Taurus.Core.Credentials;
using Taurus.Core.Provisioning;
using Taurus.Core.Users.Attributes;
using Taurus.Infrastructure.Administration;
using Taurus.Infrastructure.Authentication.Keys;
using Taurus.Infrastructure.Authentication.Opaque;
using Taurus.Infrastructure.Configuration.Persistence;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Logging;
using Taurus.Infrastructure.Persistence.Administration;
using Taurus.Infrastructure.Persistence.Users;
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
using Taurus.Server.Transport.Grpc.Administration;
using Taurus.Server.Transport.Grpc.Authentication;
using Taurus.Server.Transport.Grpc.Interceptors;
using Taurus.Server.Transport.Http;

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
        var connection = settings.Database.Resolve();
        var builder = WebApplication.CreateBuilder(args);

        if (settings.ListenUrls.Length != default)
            builder.WebHost.UseUrls(settings.ListenUrls);

        if (settings.Http.CertificateFile is not null)
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                settings.Http.CertificateFile,
                settings.Http.CertificatePassword?.Resolve());

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
        builder.Services.AddSingleton<IAdministrationOriginPolicy,
            AdministrationOriginPolicy>();
        builder.Services.AddScoped<IUserProfileRepo, UserProfileRepo>();
        builder.Services.AddScoped<IAdministrationRepo, AdministrationRepo>();
        builder.Services.AddScoped<AdministrationAuthorization>();
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
        builder.Services.AddTransient<IValidator<AuthorizeAdministrationQuery>,
            AuthorizeAdministrationValidator>();
        builder.Services.AddTransient<IValidator<CurrentUserQuery>,
            CurrentUserValidator>();
        builder.Services.AddTransient<IValidator<LoginCommand>, LoginValidator>();
        builder.Services.AddTransient<IValidator<BeginLoginCommand>,
            BeginLoginValidator>();
        builder.Services.AddTransient<IValidator<ApproveSchemaCommand>,
            ApproveSchemaValidator>();
        builder.Services.AddTransient(
            typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddControllers(options =>
            options.Conventions.Add(new RequestPrefixConvention(
                settings.Http.RequestPrefix)));
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
        app.MapControllers();

        app.MapGroup(settings.Http.RequestPrefix)
            .MapGrpcService<AuthenticationRpc>().EnableGrpcWeb();
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = settings.Http.RequestPrefix + "/ui"
        });
        app.MapFallbackToFile(settings.Http.RequestPrefix + "/ui/{*path:nonfile}",
            BrowserSecurity.IndexFile);

        await app.RunAsync();
    }
}
