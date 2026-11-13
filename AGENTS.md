# Taurus development rules

Follow [the code style guidelines](docs/development/code-style.md) for all added
or changed code. Discuss exceptions before introducing them.

Use type-named files and feature folders. Group small related types only when
the complete file stays within 30 lines. Apply this rule to every new type;
avoid catch-all definition files. Namespaces must match folder paths. Keep
Taurus.Tests classes in `tests/Taurus.Tests/tests`, without feature slicing;
leave project utilities and global usings at the root. Auto-generated
files are exempt from all rules and must remain generator-owned. Name database
access abstractions and implementations with `Repo`, such as `IAuthRepo`.

Follow [the performance guidelines](docs/development/performance.md) and
[the CQRS feature design](docs/development/architecture.md). Use MediatR and
FluentValidation for application features; FluentResults is also permitted.

Discuss new external dependencies with the user before adopting them. Never
implement cryptographic algorithms. Record dependency decisions in
[the dependency notes](docs/decisions/dependencies.md).

Keep domain types free of HTTP, protobuf, and EF Core dependencies. Put use cases
in Application and adapters in Infrastructure. Require tenant scope for identity
operations and composite tenant foreign keys. Never serialize persistence
entities as API responses.

Document contracts with comments. Reject unknown request fields recursively;
test serialized response contracts and the actual wire-level rejection path.
Never log credentials, hashes, keys, tokens, cookies, authorization headers, or
request bodies. User-audit producers must set severity explicitly and use the
append-only audit boundary. Service diagnostics/access logs belong in rotating
files, never in the identity database. Preserve atomic security/audit writes when
changing persistence; a future separate audit backend needs an outbox or an
equivalent delivery mechanism.

Build the static web client before building or publishing Server. Do not run
those builds concurrently. Run `python3 scripts/check-style.py` and relevant
checks before reporting completion. PostgreSQL contract tests require a disposable
database; they delete and recreate it. See
[the development guide](docs/guides/development.md) for integration checks.

Do not claim unfinished milestone functionality is delivered. Keep the project
pre-1.0 until the user explicitly decides to make a stable release.
