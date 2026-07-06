using Microsoft.Extensions.DependencyInjection;

using Taurus.Application.Features.Users.CreateUser;

namespace Taurus.Application;

public static class TaurusApplication
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateUserHandler>();
        return services;
    }
}
