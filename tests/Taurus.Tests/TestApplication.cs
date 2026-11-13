using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Taurus.Application.Behaviors;
using Taurus.Application.Features.Administration.Authorize;
using Taurus.Application.Features.Users.CurrentUser;
using Taurus.Core.Administration;
using Taurus.Core.Authentication;
using Taurus.Core.Users.Profiles;
using Taurus.Infrastructure.Administration;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Persistence.Administration;
using Taurus.Infrastructure.Persistence.Auditing;
using Taurus.Infrastructure.Persistence.Authentication;
using Taurus.Infrastructure.Persistence.Users;

namespace Taurus.Tests;

public static class TestApplication
{
    public static ServiceProvider Create(IdentityDatabase db, Settings settings)
    {
        var services = new ServiceCollection();

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton(settings.Sessions);
        services.AddSingleton<IAdministrationOriginPolicy>(
            new AdministrationOriginPolicy(settings));
        services.AddSingleton<IUserProfileRepo>(new UserProfileRepo(db));
        services.AddSingleton<IAuthRepo>(new AuthRepo(db,
            new UserLogRepo(db, NullLogger<UserLogRepo>.Instance)));
        services.AddSingleton<IAdministrationRepo>(new AdministrationRepo(db));
        services.AddMediatR(configuration => configuration
            .RegisterServicesFromAssemblyContaining<AuthorizeAdministrationQuery>());
        services.AddTransient<IValidator<AuthorizeAdministrationQuery>,
            AuthorizeAdministrationValidator>();
        services.AddTransient<IValidator<CurrentUserQuery>, CurrentUserValidator>();
        services.AddTransient(
            typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services.BuildServiceProvider();
    }
}
