# Development validation — 2026-11-09

These results cover the OPAQUE/configuration/proxy change, not a stable-release
approval or a throughput claim. Temporary servers, databases, certificates and
OS test libraries were isolated from operator data. No additional production
runtime was introduced.

## Build and automated tests

- Static SvelteKit 3 build completed before .NET builds/publish.
- .NET build completed without warnings or errors; Release publish completed.
- All **32 tests passed**, none skipped, with SQLite and a newly initialized
  PostgreSQL **16.15** instance using an explicitly disposable database. The
  PostgreSQL contract deleted/recreated that test database. Both generated
  Initial migrations match their current models.
- `python3 scripts/check-style.py` passed. Python scripts compiled successfully.
- Generated HTML uses portable relative asset URLs; inline bootstrap CSP hashes
  were independently recomputed and matched the document.
- No handwritten JavaScript remains; generated clients and wwwroot are ignored.

## Password and browser security

A separate compatibility spike registered a JavaScript client with the .NET
server and completed login with matching session keys. Live smoke tests then
used JavaScript OPAQUE login against .NET-enrolled roots. Wrong passwords,
synthetic unknown-user responses, single consumption, scope binding, capacity
and expiry are covered by tests.

The actual gRPC-Web path rejects unknown fields on both empty and OPAQUE-start
requests. Response serialization round-trips without undocumented fields.
A public session ID cannot authenticate as a cookie token. Missing CSRF proofs
are rejected, and revoked cookies remain invalid when restored by the client.
Unit tests also cover recursive unknown fields and CSRF tenant/origin binding.

Chrome **157.0.8103.38** completed login/logout with no browser errors, checking
Strict/HttpOnly/Secure cookie settings as applicable. Self-signed local TLS used
an explicit test certificate trust/pin, not relaxed production settings.

## Deployment matrix

| Deployment | Prefix | API smoke | Chrome login/logout |
| --- | --- | --- | --- |
| Standalone development HTTP | `/identity` | Passed | Passed |
| Standalone HTTPS | empty | Passed | Passed |
| Exact Release artifact, standalone HTTPS | default `/_taurus` | Passed | Passed |
| Nginx 1.24.0 HTTPS | `/identity`, empty | Passed | Passed |
| Caddy 2.6.2 HTTPS | `/identity`, empty | Passed | Passed |
| Traefik 3.7.17 HTTPS | `/identity`, empty | Passed | Passed |

Nginx and Caddy configuration validators passed for custom and empty prefixes.
Traefik loaded the generated dynamic configurations and served real requests.
Tests changed only deployment-specific hosts, upstreams and certificates.

Browser proxy checks supplied forged identity headers and confirmed replacement
with the authenticated identity. Taurus cookies and the mock application's
unapproved cookie did not reach the protected backend. Nginx also denied access
when Taurus was stopped after a successful authenticated request. Untrusted
forwarded metadata was ignored by a standalone server; trusted public-host
forwarding worked, including the host/port sent through X-Forwarded-Host.

Service-log checks found none of the disposable fixture's passwords or keys.
Existing tests cover rotation, forbidden payloads, append-only audit and atomic
security/audit writes. They do not establish cryptographic tamper evidence.

## Remaining limits

- Apache proxy-only setup is documented; HTTP forward-auth needs an adapter or
  separately approved module. It is not claimed as implemented.
- Configurable rate limiting remains a release blocker; development explicitly
  requires `noRateLimits: true`.
- npm audit reports four low-severity dependency-graph entries originating in
  deepmerge. Their remediation/applicability review remains open.
- Expand storage/outage/header-spoofing cases for every supported proxy topology,
  automate clean-checkout release checks, and perform an integration security
  review before distribution.
- No benchmark validates the future combined OIDC-refresh/OPAQUE-login target.
  Historical Argon2 measurements do not describe this implementation.

Reproduction commands and prerequisites are in [setup](../README.md),
[scripts](../scripts/README.md), and [forward auth](guides/forward-auth.md).
