# Taurus application feature design

## CQRS

Implement application features using CQRS and MediatR. Separate commands that
change state from queries that return data, with a dedicated request and handler
for each use case. CQRS does not require separate databases or event sourcing.

Keep requests, handlers, and validators in Taurus.Application, grouped by feature.
Keep domain entities and repository contracts in Taurus.Core. Keep EF Core and
other external adapters in Taurus.Infrastructure. Taurus.Server maps transport
contracts to application requests and maps results back to explicit responses.

Use FluentValidation validators and a MediatR pipeline behavior to validate
requests before handlers execute. Transport parsing, unknown-field rejection,
and message size limits remain transport-boundary responsibilities. Domain
invariants must also hold independently of transport validation.

Propagate cancellation and tenant context explicitly. Keep transaction ownership
in the application use case and infrastructure implementation; do not wrap every
query in an unconditional write transaction.

FluentResults is permitted for expected application failures. Keep failures typed
and map them to documented transport errors without exposing secrets or internal
exception details. Do not use exceptions for ordinary rejected credentials.

MediatR, FluentValidation, and FluentResults are approved dependencies. Discuss
any additional library before adopting it. Follow the performance guidelines
when designing request pipelines and their dependency lifetimes.

## Persistence and audit boundaries

Name database repositories with `Repo`, such as `IAuthRepo` and `IUserLogRepo`.
User-audit recording has its own handler and persistence boundary. Preserve
atomic security changes with the current shared unit of work; a future independent
audit backend will need an outbox or equivalent delivery design. Retention belongs
to a separate explicit batch operation, rather than ordinary entity deletion.
Operational access and diagnostic logs go to rotating files, not EF entities.
