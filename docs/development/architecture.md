# Taurus architecture

Taurus uses Clean Architecture with CQRS for application use cases. The boundaries
keep identity behavior independent of storage providers and transport protocols.
This document describes the intended responsibilities and identifies current
adaptations; it does not imply that every planned protocol is implemented.

## Components and dependencies

| Component | Responsibility | Project dependencies |
| --- | --- | --- |
| Core | Domain entities and capability interfaces | No other Taurus project |
| Application | Use cases, policy evaluation and validation pipelines | Core |
| Infrastructure | Persistence, configuration, cryptographic library adapters and file logging | Core, Application |
| Server | ASP.NET entry point, composition and transport adapters | Application, Infrastructure, Shared |
| Shared | Protobuf contracts and generated transport types | No domain or application project |
| WebClient | Static SvelteKit browser application | Generated clients from Shared contracts |
| Tests | Unit and integration verification | References Server to exercise the composed layers |

Core must not depend on HTTP, protobuf, EF Core or a particular database vendor.
Application depends on domain capabilities rather than their implementations.
Infrastructure implements those capabilities using external systems and libraries.
Server references Infrastructure to register implementations in dependency
injection; that composition dependency does not move use-case ownership to Server.

Shared is a transport contract boundary, not a container for domain entities or
business logic. WebClient communicates through generated gRPC-Web contracts;
it cannot access persistence directly. Its static assets are served by ASP.NET,
so production does not require a separate Node service.

## Domain model and capability interfaces

Core contains identity state: users, credentials, groups, memberships, attributes,
sessions, authentication policies and user-audit events. Tenant root bindings
identify the original bootstrap administrator by stable user ID, not username. These types represent
separate concerns; a user is not also a credential or a transport response.

Tenant-owned identity operations require explicit tenant scope. Persistence keys
and relationships must preserve that scope, including composite tenant foreign
keys. Database mapping belongs in Infrastructure. Domain invariants must remain
valid independently of transport validation.

The current entities are mostly data-bearing types. Taurus has not established
rich aggregates, aggregate roots or a complete DDD model. Do not assume those
patterns exist merely because domain types and repository interfaces are present.

Capability interfaces define what use cases need from persistence, provisioning,
schema management and authentication/key operations. Their current home is Core.
Their implementations may change without requiring transport contracts to expose
provider-specific types. Naming conventions belong in [code style](code-style.md).

## Application use cases and CQRS

Commands express operations that change state; queries express requests for
information. Each use case has a dedicated request and handler, organized by
feature in Application and dispatched through MediatR. CQRS does not require
separate databases, event sourcing or separate deployment processes.

Handlers coordinate domain checks, policy evaluation, repository operations and
external capabilities. They must not depend on ASP.NET contexts, protobuf
messages or concrete EF contexts. Pass tenant scope and cancellation explicitly.
Keep business decisions in Application or domain behavior rather than in
transport mapping or database-provider code.

FluentValidation validators check application request requirements through a
MediatR pipeline behavior before handlers execute. Pipeline behaviors provide
shared execution concerns; they should not obscure feature-specific decisions.
Transport parsing and domain invariants remain separate validation boundaries.

Expected failures should have explicit application meanings; FluentResults is
available for representing them. Transport adapters map failures to documented,
non-sensitive responses. Ordinary rejected credentials are not exceptional
application failures. Exceptions remain appropriate for unexpected faults.

## Repositories and services

Repositories encapsulate persistence operations required by use cases. Interfaces
belong in Core and implementations in Infrastructure. They enforce tenant-scoped
access and map domain data to the selected provider. They must not expose EF
queryables or transport responses to application callers. A generic CRUD layer
is not required, and repository methods must not absorb unrelated business policy.

Services represent specific capabilities or cohesive behavior that does not fit
an entity or a single handler. State their responsibility explicitly rather than
using a service as a catch-all for business logic. Application coordinates use
cases; Infrastructure services adapt configuration, persistence, cryptographic
libraries and operational logging where needed.

Authentication and key-operation interfaces isolate callers from current library
implementations. They reserve an adapter boundary for future HSM integration;
they do not establish HSM support today. Cryptographic algorithms must come from
approved implementations, not Taurus-owned algorithm implementations.

## Transport adapters and request flow

Server exposes gRPC services and MVC HTTP controllers. Controllers own HTTP
routes for browser redirects/logout, forward-auth and health probes; Program
composes dependencies, middleware, static files and transport registration.
The `taurus` route token resolves to the configured request prefix, including
an empty prefix. Health probes and the site-root login redirect retain their
unprefixed routes. These adapters resolve the configured application/tenant context,
apply transport security, map contracts to application requests and construct
explicit response contracts. Future protocol adapters must follow
the same boundary rather than introduce another business-logic layer.

A normal browser request follows this flow:

1. Server resolves the configured origin and tenant and applies relevant browser
   and transport protections.
2. The gRPC binding parses the contract, rejects unknown fields recursively and
   enforces message limits before invoking the service.
3. The adapter maps the request to an application command or query.
4. MediatR runs application validation and dispatches the handler.
5. The handler coordinates domain checks and infrastructure capabilities.
6. The adapter maps the outcome to the documented transport response.

Persistence entities must never be serialized as API responses. Unknown-field
rejection must be tested on the actual wire path, and serialized responses must
be checked against their contracts. Health and static-file endpoints do not need
artificial application handlers when no application use case is involved.

## Transactions, audit and failures

Transaction requirements belong to the use case, with Infrastructure implementing
the persistence mechanism. Preserve atomic security changes and user-audit writes
through the current shared scoped EF unit of work. Do not wrap every query in an
unconditional write transaction.

User audit has a separate append-only repository boundary and recording handler.
Producers explicitly select severity. Retention is a separate deliberate batch
operation, not ordinary entity deletion. A future independent audit backend needs
an outbox or equivalent delivery mechanism; replacing an atomic write with an
uncoordinated remote call would lose the guarantee.

Strict audit mode requires persistence of security changes and their audit.
The explicit availability mode permits audit gaps for supported session operations;
bootstrap and destructive schema administration remain strict. Storage failure
must not grant access. Service access and diagnostic logs belong in rotating files,
not identity database entities. See [auditing](../security/auditing.md).

## Current adaptations and limitations

- Provisioning and schema use cases enter through Application handlers, but their
  current Infrastructure services also contain orchestration and direct EF work.
  This is a current boundary compromise, not a general rule for new features.
- Session lookup queries can perform throttled idle-expiry renewal. This is an
  intentional persistence side effect; current queries are not universally pure.
- Repository operations currently combine session persistence and audit writes
  to preserve their atomicity. Separating backends requires a delivery design.
- Pending OPAQUE exchanges are bounded process-local state. Shared database
  sessions alone do not make unfinished login exchanges work across replicas.
- Existing domain types provide foundations for future protocols and management
  features; their presence does not mean those features are implemented.

Dependency selections and approval history belong in
[dependency decisions](../decisions/dependencies.md). Formatting and naming belong
in [code style](code-style.md); allocation guidance belongs in
[performance](performance.md). Detailed security guarantees and limitations belong
in the [security model](../security/overview.md).
