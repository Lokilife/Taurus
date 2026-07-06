using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;

using Taurus.Application.Features.Users.CreateUser;
using Taurus.Infrastructure;
using Taurus.Infrastructure.Persistence;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration.AddYamlFile("appsettings.yml", optional: false)
                     .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yml", optional: true)
                     .AddEnvironmentVariables("TAURUS_IDP_");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddAuthorization();
// builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaurusDbContext>();
    await db.TaurusMigrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1/openapi.json", "v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Todo[] sampleTodos =
// [
//     new(1, "Walk the dog"),
//     new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
//     new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
//     new(4, "Clean the bathroom"),
//     new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
// ];

// var todosApi = app.MapGroup("/todos");
// todosApi.MapGet("/", () => sampleTodos)
//         .WithName("GetTodos");

// todosApi.MapGet("/{id}", Results<Ok<Todo>, NotFound> (int id) =>
//     sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
//         ? TypedResults.Ok(todo)
//         : TypedResults.NotFound())
//     .WithName("GetTodoById");

var api = app.MapGroup("/api");
var users = api.MapGroup("/users");
users.MapPost("/", static async Task<Results<Ok<CreateUserResult>, BadRequest<string>>>
                        (CreateUserCommand command, CreateUserHandler handler, CancellationToken ct) =>
    {
        var result = await handler.HandleAsync(command, ct);
        return result.Success ? TypedResults.Ok(result) : TypedResults.BadRequest(result.ErrorMessage);
    })
    .WithName("CreateUser")
    .WithDisplayName("Create user")
    .WithDescription("Creates new user");

app.Run();

// public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(CreateUserResult[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
