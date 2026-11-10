# Taurus milestone security model

## OPAQUE passwords

Browser passwords remain in the library client. Network contracts carry only
OPAQUE messages; persistence holds registration records, not password hashes.
Trusted local root provisioning runs library enrollment using the configured
password. It is the explicit exception to normal network password isolation.
The current profile uses the library's memory-constrained key stretching.

Vaultic.OPAQUE.Net 1.4.0 and @serenity-kit/opaque 1.1.0 completed a cross-library
registration/login spike with matching session keys. The running-server smoke
script checks JavaScript login against locally enrolled .NET credentials too.
The JavaScript library's historical audit does not automatically cover the .NET
wrapper, native binaries, newer versions or Taurus's integration.

Persist and protect the OPAQUE server setup independently of credential records.
Losing it prevents login; replacing it requires re-enrollment. A stolen database
alone is a different threat from theft of both server setup and records.
Single-server OPAQUE still permits offline password guessing after both are
compromised; it does not satisfy a full-compromise immunity claim. See
[RFC 9807](https://www.rfc-editor.org/rfc/rfc9807.html).
A compromised server can deliver malicious JavaScript that captures a password.
Recovery and future email authentication must preserve explicit trust boundaries.

Unknown usernames receive library-generated synthetic responses. Authentication
failures are generic; database lookups, policy checks and scheduling may still
produce timing differences. Perfect timing indistinguishability is not claimed.
Server login challenges are bounded, expiring, scoped to tenant/origin/username
and credential record, and consumed once. They currently live in one process;
restarts lose unfinished logins, and clustering needs shared state or affinity.
Configurable rate limiting remains a release prerequisite. `noRateLimits: true`
is an explicit testing opt-out, not production protection.

## Sessions and CSRF

Opaque session cookies have 256 bits of random entropy. Only SHA-256 token digests
are persisted. Hashing random tokens differs from password hashing. A separate
public session ID is safe to return; it is never accepted as a bearer token.
Authorization checks tenant, exact origin, expiry, revocation and enabled-user
status in the database. No cookie token is returned in protobuf.

The server computes a base64 seed using framework HMAC-SHA256 over an unambiguous
encoding of purpose, key version, tenant, origin, user ID and public session ID.
The HMAC key is separate from OPAQUE setup. Login start uses an expiring random
HttpOnly challenge cookie as its browser context and an empty user ID. That
response supplies the seed alongside the OPAQUE server message, avoiding a
separate bootstrap request. Login/current-session return the session-bound seed.

The client holds the seed in memory, generates a fresh 32-byte random salt, and
uses WebCrypto SHA-256 over decoded seed || salt. It sends base64 proof in
`x-taurus-csrf` and base64 salt in `x-taurus-csrf-salt`. The server recomputes the
expected seed from its authenticated context and verifies the proof in fixed
time with exact byte-length checks. Merely hashing public user/session IDs would
allow an attacker to construct a proof; the server-authenticated seed prevents
that. The salt alone is **not replay protection**; OPAQUE challenges are single-use,
while session CSRF proofs remain usable until their session/key context changes.

Mutation and login-start requests require the exact Origin header. Browser APIs
are same-origin gRPC-Web; no CORS is enabled. HTTPS cookies are Secure, HttpOnly,
host-only Path=/ with SameSite=Strict. Explicit loopback HTTP development uses
separate unprefixed cookie names. Both loopback origin and remote address are
checked. Never enable this for public use. Static CSP hashes protect bootstrap
scripts; `wasm-unsafe-eval` permits the OPAQUE WASM module, not arbitrary JS eval.
The library-generated SvelteKit accessibility announcer uses an inline style;
CSP permits only its exact hash with `unsafe-hashes`, while handwritten styles
remain external CSS. Browser checks catch a changed generated style after upgrades.
Server headers prohibit framing. Secrets never appear in return URLs.

## Proxy and transport boundaries

A reverse proxy is recommended but optional. Direct HTTPS uses Kestrel's
certificate facilities. Proxy adapters must preserve the public Host, overwrite
forwarded metadata and identity headers, remove Taurus credentials before the
protected backend, and deny access on auth/storage failure. Trust only explicit
proxy IPs. Each configured application host has an independent session. Mutually
untrusted apps need distinct hosts: same-origin XSS can act as the user despite
HttpOnly cookies. See [forward authentication](guides/forward-auth.md).

Strict service bindings reject unknown protobuf fields recursively before
handlers. Parser tests and the actual wire smoke path cover rejection. Responses
are explicit contracts, never serialized persistence entities. Request/message
sizes and validation bound transport work. Do not log request bodies, OPAQUE
messages, cookies, proofs, seeds, hashes, keys or authorization headers.

## Persistence, audit and HSM boundaries

Tenant-owned keys and relationships include tenant IDs. Identity repositories
require scope. Bootstrap and destructive schema operations are explicit commands.
Strict user-audit mode commits security changes and audit together. Availability
mode explicitly allows gaps; total persistence failure still denies sessions.
Audit is application-append-only, not tamper-proof against a database operator.
Service files contain only allowlisted metadata and safe Taurus diagnostics.

`IOpaqueService` and `IAuthenticationKeys` isolate authentication operations from
callers. A future HSM adapter can implement key operations without changing
transport contracts. The current native OPAQUE wrapper requires exportable setup
material in process memory; it does **not** provide non-exportable HSM support.
No home-grown cryptographic algorithms are used. HSM support and key lifecycle
management need their own implementation and security review.
