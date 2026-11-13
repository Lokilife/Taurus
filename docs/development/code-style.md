# Taurus code style

These rules apply to code added or changed in Taurus. Additional rules may be
introduced during review. Discuss exceptions before using them.

Auto-generated files that should not be edited manually are exempt from every
rule in these guidelines. This includes EF migrations, model snapshots, generated
protobuf code, and generated build outputs. Leave their formatting, namespaces,
and layout as generated; update them only through their generator.

## Line length

Keep every code line at or below 90 characters. Wrap long declarations,
expressions, and calls across lines.

## Files and folders

Put each type definition in a file named after that type. This applies to
entities, enums, interfaces, records, commands, queries, handlers, validators,
configuration types, helpers, and tests.

Closely related small types may share a descriptively named file only when the
entire file, including imports, namespace, comments, and blank lines, contains
at most 30 lines. A larger type always needs its own file. Keep related types
together by feature; do not collect unrelated definitions in catch-all files
such as `Entities.cs` or `Contracts.cs`.

Organize folders by architectural layer and feature so paths reveal ownership.
For example, use `Taurus.Core/Users/UserEntity.cs`,
`Taurus.Core/Credentials/CredentialEntity.cs`, and
`Taurus.Application/Features/Authentication/Login/LoginHandler.cs`.
Keep infrastructure implementations under their concern, such as
`Persistence/Providers/Sqlite` or `Authentication/Opaque`. Put shared pipeline
behaviors in `Behaviors`.

Taurus.Tests is exempt from feature slicing: keep test classes in
`tests/Taurus.Tests/tests`, using type-named files.
Keep project utilities and global usings in the project root.

A handwritten C# namespace must match the full folder path relative to `src` or
`tests`. For example, the namespace for
`Taurus.Core/Authentication/Policies/AdminAuthenticationPolicyEntity.cs` is
`Taurus.Core.Authentication.Policies`. Update imports when moving files. Preserve
existing persisted data with generated migrations when type identities change.

Name database-access interfaces and implementations as repositories using the
`Repo` suffix, for example `IAuthRepo` and `AuthRepo`. Use `repo`, `authRepo`, or
`userLogRepo` as appropriate for local variables and dependencies; do not call
these components stores. Business services and file-log writers remain services
or writers rather than being renamed as database repositories.

## Blank lines

Separate the using-directive block, namespace declaration, and type declarations
with an empty line.

Leave an empty line between declarations of entities of any kind, including
classes, interfaces, and methods. Fields and properties are an exception:
consecutive members of the same kind may have no empty lines between them.
Group constants, IoC-provided properties, public fields, private fields, and
ordinary properties separately. Put an empty line between these groups.
Grouping is by member role and visibility, not by the member's CLR data type.

Inside methods, separate logical parts with empty lines. Treat variable
definitions, validation, and the main action as distinct parts. A method with
multiple actions can repeat these parts. Omit parts when unnecessary, but end
each present part with an empty line so its boundary is visible, except at the
end of a block. Never leave an empty line immediately before a closing brace.

Leave an empty line after every `for`, `while`, or `foreach` loop unless the
loop is the final statement in its containing block.

```csharp
using Taurus.Core.Users;

namespace Taurus.Application;

public sealed class Example
{
    public void Execute(IEnumerable<UserEntity> users)
    {
        var enabledUsers = new List<UserEntity>();

        ArgumentNullException.ThrowIfNull(users);

        foreach (var user in users)
        {
            if (user.Status != UserStatus.Enabled)
                continue;

            enabledUsers.Add(user);
        }

        Save(enabledUsers);
    }

    private void Save(IEnumerable<UserEntity> users)
    {
        // Implementation omitted for this formatting example.
    }
}
```

## Conditions and blocks

When an `if` statement has no braces, place its instruction on the next line,
indented beneath the condition. Never put both on one line.

```csharp
if (condition)
    Execute();
```

Never write a non-empty enum on one line. Put its braces and members on
separate lines.

```csharp
public enum UserStatus
{
    Enabled,
    Disabled
}
```

Never write a non-empty `try` or `catch` block on one line.

```csharp
try
{
    Execute();
}
catch (InvalidOperationException)
{
    HandleFailure();
}
```

## Strings and numbers

Do not use magic strings or numbers. Define values using enums, named constants,
or static immutable fields.

The following contexts are exempt:

- Log entries.
- C# exceptions.
- Endpoints in methods.
- Default values in configuration.
- Easily understood defaults in database models and method arguments, such as
  locale `"en-US"`, timezone `"UTC"`, or an initial version of `1`. Specific or
  unexplained values such as `4563.45f` still require a named constant.
- Statically checked strings.

Discuss any other exception before introducing it. When it is unclear whether a
string is statically checked, use a named value or discuss the case first.

## Prefer typed values

Use enums or other appropriate types instead of strings when the value has a
known meaning or a closed set of choices. For example, credential kinds must use
`CredentialType`, not arbitrary strings. Apply this to related policy fields too.

Strings remain acceptable at boundaries where stronger typing would complicate
correct behavior without a useful benefit, such as storing timezone identifiers
in a database. Do not create an enum for open-ended user data or custom attribute
names merely to avoid strings.

## WebClient

Use TypeScript for handwritten application code, configuration and browser tools.
Place styles in separate CSS files adjacent to their components. Generated
JavaScript remains generator-owned and is not tracked in the repository.

## Commit messages

Commit messages must follow
[Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/).
Follow the specification closely; do not introduce deviations without a concrete
need and an explicit documented agreement.

Use `<type>[optional scope][optional !]: <description>` for the subject, with
an accurate, concise description. Use `feat` for new features, `fix` for bug
fixes, and `docs` for documentation-only changes. Other meaningful types are
permitted by the specification. A scope is optional; include it when useful.

Mark breaking changes with `!` before the colon or a `BREAKING CHANGE:` footer.
Bodies and footers are optional; when present, follow the specification's
blank-line and footer syntax. Do not require extra sections or scopes merely
for formality.

Example: `docs: reorganize documentation and clarify architecture`.
