# First-release checklist

Status reviewed: 2026-11-09.

Scope: the initial pre-1.0 release built on milestone one: password login,
revocable sessions and proxy-independent HTTP forward authentication. This is not the scope of a
future stable 1.0 release. Stable-release approval remains with the project owner.

`[x]` means implemented with current evidence. `[ ]` means implementation,
verification or an explicit decision remains. Completed components still need
final release-candidate validation. Later features are listed separately and
are not blockers for this release.

## Architecture and code organization

- [x] Six components: Server, WebClient, Shared, Core, Application and
  Infrastructure, with no Node service required in production.
- [x] Clean Architecture boundaries: domain models and repository contracts are
  independent of ASP.NET, protobuf and EF Core.
- [x] CQRS using MediatR, with FluentValidation before application handlers and
  typed application failures through FluentResults where applicable.
- [x] Type-named files, feature folders and folder-matched namespaces.
- [x] Test classes in `tests/Taurus.Tests/tests` without feature slicing;
  root utilities and repository naming such as `IAuthRepo`.
- [x] Documented code-style, performance and dependency-approval guidelines.
  Generated files remain generator-owned and exempt from handwritten rules.
- [x] Development version remains below 1.0 (`0.1.0-dev`).

## Configuration, tenancy and storage

- [x] Startup-loaded YAML with explicitly included tenant files.
- [x] Strict configuration validation: unknown/duplicate properties, conflicting
  origins, invalid settings and unsupported authentication factors are rejected.
- [x] Explicit `--config PATH`, otherwise `/etc/taurus/config.yml` then
  executable-local `config/config.yml`; no configuration environment selector.
- [x] Inline, environment and mounted-file secret sources, exactly one each.
  Secret-file spaces remain significant.
- [x] Direct HTTPS with certificate/password references, explicit loopback-only
  development HTTP, and runtime prefix configuration including an empty prefix.
- [x] Per-tenant origins and stable issuer configuration; unknown origins fail
  closed. Public login messages cannot choose an arbitrary tenant ID.
- [x] Tenant-scoped keys, relationships, uniqueness constraints and queries.
- [x] PostgreSQL and SQLite implementations with generated migrations and an
  explicit migration command.
- [x] User, credential, session, group, membership, attribute and authentication
  policy models. These models do not imply that management APIs already exist.
- [x] Bootstrap root account for each tenant from configured credentials;
  restarting does not reset passwords or reactivate existing accounts.
- [x] Administrator policy bounds user policy; effective requirements must satisfy
  both. Password-only capability is enforced for this milestone.
- [x] YAML attribute definitions, database values and applied schema snapshots.
- [x] Compatible schema application and fail-fast incompatible-schema checks.
- [x] Schema preview and fingerprint-bound approval for destructive changes;
  affected values, schema snapshots and audit events change transactionally.
- [x] Liveness and readiness endpoints, including migration/schema checks.
- [ ] Verify multi-tenant operation end to end with separate roots, application
  hosts, sessions and attempted cross-tenant access on both database providers.
- [ ] Document and rehearse backup/restore, schema-change recovery and supported
  upgrade procedures using representative existing data.

## Login, browser communication and sessions

- [x] Lightweight static SvelteKit login, authenticated landing and logout.
- [x] ASP.NET serves the web assets; Node is needed only for building them.
- [x] Documented protobuf contracts and generated C#/gRPC-Web browser clients.
- [x] Recursive rejection of unknown protobuf fields on the actual wire path.
- [x] Explicit response DTOs and contract tests; persistence entities are not
  serialized to clients.
- [x] Request validation and message-size limits before business operations.
- [x] OPAQUE replaces Argon2 network password verification. Approved .NET and JS
  libraries pass a cross-library registration/login compatibility spike; normal
  network requests contain no plaintext password.
- [x] Local configured-root enrollment, persistent separate server setup, and
  generic failures with library-generated synthetic unknown-account responses.
- [x] Bounded expiring single-use OPAQUE challenges, scoped to tenant/origin/user
  and the credential record. Pending login state is currently process-local.
- [x] Authentication/key operation interfaces reserve future HSM adapters;
  current exportable native setup is not HSM support.
- [x] Random opaque session cookies; only token digests are persisted.
- [x] Secure, HttpOnly, host-only cookies and origin-bound, tenant-bound sessions.
- [x] Server-side expiry, enabled-user checks and revocation on authorization.
- [x] Configurable idle/absolute lifetimes and throttled idle renewal, including
  renewal for short idle lifetimes.
- [x] Server-authenticated CSRF seeds, client salts and SHA-256 proofs, with seeds
  delivered by login-start/login/current-session instead of a bootstrap RPC.
  Separate public session IDs never act as bearer tokens. Salt is not replay protection.
- [x] Same-origin checks and framing/CSP controls, including OPAQUE WASM support.
- [x] TypeScript SvelteKit 3 tooling and adjacent component CSS; generated clients
  and Server wwwroot remain ignored build outputs.
- [x] Same-origin return-destination validation.
- [ ] Expand timing/enumeration regression checks across unknown usernames,
  wrong passwords, disabled users and revoked credentials under concurrent load.
- [ ] Document and verify operator credential rotation, root-account recovery and
  session revocation without relying on the future administration panel.
  Bootstrap-only provisioning must retain its current non-resetting behavior.

## Configurable rate limiting — release blocker

- [ ] Implement configurable protection for user-facing endpoints, particularly
  before expensive password verification. Bound queued/in-flight work as well
  as request frequency.
- [ ] Define tenant/client/account limit keys and trusted-IP handling without
  exposing whether an account exists or enabling trivial account lockout attacks.
- [ ] Support safe limits with `noRateLimits: false`. Currently that setting
  prevents startup because only the explicit testing bypass is implemented.
- [ ] Make the public deployment example enable protection; keep the bypass an
  explicit testing choice.
- [ ] Document transport responses and retry behavior; verify gRPC-Web and Nginx
  handling, tenant isolation, concurrency and fail-closed behavior.
- [ ] Record rate-limited operations safely in service files, without credentials,
  raw headers or request bodies.

## Reverse proxies and forward authentication

- [x] Database-backed authorization endpoint and Nginx `auth_request` example.
- [x] Tenant/user identity headers after successful authorization; incoming
  identity headers are overwritten.
- [x] Ready includes/snippets for Nginx, Caddy and Traefik plus a beginner-first
  guide and separate examples. Defaults remove all app-bound cookies and
  Authorization; application-cookie allowlists require an explicit adaptation.
- [x] Custom/empty-prefix rendering without rebuilding WebClient.
- [x] Apache proxy-only example and documented FastCGI/HTTP adapter prerequisite.
- [ ] Apache forward-auth adapter/module decision and full integration.
- [x] Nginx and Caddy validators, plus live Nginx/Caddy/Traefik API and browser
  checks for custom and disabled prefixes, identity spoofing and cookie exclusion.
- [x] Nginx denies access when the authentication backend is stopped.
- [ ] Expand proxy outage/storage-failure regression checks for every topology.
- [x] Explicit trusted proxies and rejection of untrusted origin/scheme contexts.
- [x] Separate login and host-only sessions for each application host.
- [x] Documented current access policy: any enabled, authenticated tenant user can
  access its configured application. Resource-level permissions are not shipped.
- [x] Browser/proxy OPAQUE protected access, logout, Strict/HttpOnly/Secure
  cookie checks, forged identity replacement and secret-cookie exclusion.
- [ ] Re-run the complete Nginx/browser integration against the release artifact,
  including outages, proxy/header spoofing and the new rate-limit behavior.
- [ ] Validate the deployment guide's TLS, direct-backend-access restrictions and
  distinct-host requirements in the supported release topology.

## User audit and operational logs

- [x] Separate append-only user-audit repository and CQRS recording handler.
- [x] Atomic security/audit writes in strict mode and explicitly configurable
  session-operation availability mode when user-audit storage fails.
- [x] Explicit `None`, `Low`, `Middle` and `High` user-log severities.
- [x] Login events classified Middle, logout Low, and bootstrap, policy and schema
  changes High. Previously unclassified records migrate conservatively to High.
- [x] No automatic user-log deletion; committed events cannot be edited through
  the audit repository.
- [x] Service access and diagnostics use rotating `access.log` and `service.log`
  files, with configurable directory, size, backups and diagnostic level.
- [x] Service logging has no database dependency; HTTP/gRPC outcomes are recorded
  distinctly, using allowlisted metadata.
- [x] Tests cover file rotation, forbidden payload exclusion, severity persistence,
  append-only behavior and audit failure modes.
- [x] Authorized pre-1.0 migration reset: one generated Initial per provider,
  with public session identifiers and no service-log table. Old development
  databases have no automatic upgrade path; operator data is never auto-erased.
- [ ] Review audit coverage and investigation context for every shipped action.
  User logs currently lack client IP/browser/correlation fields; decide and add
  the safe context needed to connect security events to service records.
- [ ] Document the first-release retention choice explicitly. Keep-all user logs
  are supported; automatic age-based batch deletion is not implemented.
- [ ] Rehearse file rotation, disk-full/permission failures and log-directory
  isolation between replicas in a deployment test.

## Dependencies, security review and resource validation

- [x] Dependency decisions and current security limitations are documented.
- [x] SvelteKit 3 toolchain and required OpenTelemetry peer upgrade approved
  and implemented. Remaining low-severity deepmerge advisories are documented.
- [ ] Resolve or assess remaining build dependency advisories before release.
- [ ] Review OPAQUE integration, native-platform coverage, licensing and audit
  version/scope; an audit of the JS project does not certify the .NET wrapper.
- [ ] Review the release candidate's dependency advisories and required notices.
  Discuss any newly proposed external dependency before adding it.
- [ ] Complete a security review of the actual login, session, proxy, audit and
  schema-approval paths, including cancellation and storage-failure behavior.
- [x] Initial resource measurements and a repeatable forward-auth measurement
  script exist.
- [ ] Refresh benchmarks after replacing database service logs with files. The
  existing baseline is historical and cannot describe the current logging path.
- [ ] Agree on resource budgets and representative workloads, then measure memory,
  allocations, startup, sustained login/forward-auth load and large datasets on
  both providers. Document tested limits before making capacity claims.
- [ ] Exercise PostgreSQL with multiple Taurus instances sharing sessions and
  revocation state; keep service-file directories separate per instance.

## Release-candidate checks and distribution

- [x] Current SQLite/unit suite passes, including OPAQUE, CSRF, contracts,
  tenant constraints, sessions, schema approval, audit and file logs.
- [x] All 32 tests pass, including PostgreSQL 16.15 against an explicitly
  disposable database and both providers' generated Initial migrations.
- [x] Live standalone custom-prefix OPAQUE/CSRF/wire-rejection/revocation smoke.
- [x] Live standalone HTTPS and empty-prefix OPAQUE/CSRF/revocation smoke.
- [x] Chrome checks pass for standalone HTTP/HTTPS and Nginx/Caddy/Traefik,
  with custom, default and disabled prefixes. See [validation](validation.md).
- [x] Setup, configuration, migration, Nginx and operational documentation exists.
- [ ] Add automated clean-checkout build/test checks, including PostgreSQL and
  generated-contract/style verification, without requiring manual test setup.
- [x] Static-web-first Release publish and exact-artifact API/browser smoke pass.
- [ ] Repeat the release build in clean-checkout automation.
- [ ] Declare supported platforms and deployment requirements. Browser-generator
  automation currently targets Linux x86-64; other build platforms need a decision.
- [ ] Finish operator documentation for rate limits, backups, recovery, upgrades,
  filesystem permissions, retention and the service-log-table removal.
- [ ] Decide distribution/license terms, assemble required notices, and prepare
  versioned artifacts, release notes and integrity checksums.
- [ ] Review this checklist against release evidence and obtain owner approval
  for the pre-1.0 release. Do not silently promote the project to stable 1.0.

## Required pre-1.0 email milestones

Email integration is required before stable 1.0, after this development release.

- [ ] Email delivery integration; discuss the delivery library/provider adapter
  before adding dependencies. Separate delivery from identity use cases.
- [ ] Email password recovery with OPAQUE re-enrollment, email second factor and
  email-only one-time-code login. Codes must be single-use, expiring, attempt
  bounded and audited, with generic account-existence responses and safe recovery.

## Other planned milestones

Release sequencing for these features remains a project decision.

- [ ] Administration panel and CQRS management APIs for users, groups,
  permissions, credentials, sessions and user authentication policies.
- [ ] TOTP, recovery codes, WebAuthn and other allowed factor enrollment.
- [ ] Password/email/profile changes and recovery workflows.
- [ ] Failed-attempt aggregation, recovery-use tracking and session-IP anomalies.
- [ ] User-log batch retention and a dedicated audit backend with outbox or
  equivalent delivery/atomicity guarantees.
- [ ] OIDC/OAuth2, LDAP, Kerberos and other authentication adapters; domain
  entities must remain independent of individual protocols.
- [ ] Cross-domain SSO, resource permissions and richer claim mappings.
- [ ] Additional database providers/routing and dynamic config reload as needed.

## After 1.0 roadmap

- [ ] Kafka integration for suitable workloads, with a simple built-in persistent
  queue option for small homelabs.
- [ ] Phone verification through external services.
- [ ] Login through external OAuth2 providers such as Google and Facebook.
- [ ] Lightweight Linux launcher: an unprivileged user, filesystem allowlist,
  IPC limited to CLI, and explicit network access plus proxy responses, using
  Linux isolation without containers. Secure defaults with minimal dependencies.
- [ ] SELinux support and documented confinement policy.
- [ ] PKI lifecycle management and built-in CA.
- [ ] HSM-backed key operations and lifecycle management.
- [ ] Dynamic-complexity proof of work for user-facing endpoints.
- [ ] Taurus.CLI for operations, config/setup generation, migrations, users and
  secure root provisioning.
- [ ] Taurus.SDK: C#, JavaScript web components and native Kotlin Android APIs.
- [ ] Clustering and load balancing, including shared/affinitized OPAQUE state.
- [ ] Validate the future combined target: 2,500 OIDC refresh-token/JWT exchanges
  plus 500 OPAQUE logins per second on one instance with 4 vCPU/8 GB RAM;
  database/load generators are outside that allocation. Not a current claim.

References: [setup](../README.md), [forward auth](guides/forward-auth.md),
[security](security.md), [auditing](auditing.md), [dependencies](dependencies.md),
[performance](performance.md), [architecture](architecture.md),
[code style](code-style.md).
