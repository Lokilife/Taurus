# Setup and configuration

Build Taurus using the [development guide](development.md) before initialization.

## Configuration and keys

Taurus checks `/etc/taurus/config.yml`, then `$BIN/config/config.yml`, where
`$BIN` means the executable's directory. `--config PATH` selects exactly that
file with no fallback. It works with serving and maintenance commands.

Copy [the sample](../../config/taurus.yml) and configure database, tenants, exact public
origins, listeners and logging. Secret references accept exactly one of `value`,
`environment`, or `file`, including root passwords and certificate passwords:

```yaml
rootPassword:
  value: "replace-this-example"
```

Protect inline-secret YAML like a key file. File references are relative to the
working directory. Only terminal CR/LF is trimmed; spaces are preserved.
Generate and persist separate OPAQUE setup and CSRF authentication keys **once**:

```sh
umask 077
dotnet run --no-build --project src/Taurus.Server -- generate-opaque-setup > opaque.key
dotnet run --no-build --project src/Taurus.Server -- generate-csrf-key > csrf.key
chmod 600 opaque.key csrf.key
```

These commands intentionally emit secrets to stdout; redirect to protected files,
never logs. Use `file` references or export their contents as `TAURUS_OPAQUE_SETUP`
and `TAURUS_CSRF_KEY`. Do not regenerate setup on each launch: changing OPAQUE setup
invalidates enrolled credentials. CSRF key/version rotation invalidates browser
proofs; fetching the current session supplies the new seed. Keep these keys
separate from the identity database and include them in protected backups.

```sh
export TAURUS_DATABASE='Data Source=/absolute/path/taurus.db'
read -rs -p 'Root password: ' TAURUS_ROOT_PASSWORD
export TAURUS_ROOT_PASSWORD
dotnet run --no-build --project src/Taurus.Server -- --config config/taurus.yml migrate
dotnet run --no-build --project src/Taurus.Server -- --config config/taurus.yml bootstrap
dotnet run --no-build --project src/Taurus.Server -- --config config/taurus.yml
```

For PostgreSQL set `provider: PostgreSql` and use an Npgsql connection string.
Run migrations once before serving. Root enrollment reads the configured password
only in the trusted local process and runs the library client/server enrollment.
Normal browser login never sends the password to Taurus. Existing roots are not
reset, enabled or re-enrolled on restart; run provisioning serially.

This development change resets migration history to one generated `Initial` per
provider. Earlier development databases are incompatible: recreate only known
disposable databases or explicitly migrate your data offline. Startup rejects
pending/mismatched migrations. No general operator database is erased for you.

## Direct serving and reverse proxies

A reverse proxy is recommended, not required. For direct production HTTPS:

```yaml
listenUrls: [https://0.0.0.0:5443]
http:
  requestPrefix: /_taurus
  certificateFile: /etc/taurus/server.pfx
  certificatePassword:
    file: /etc/taurus/certificate-password
trustedProxies: []
```

Use a certificate matching configured HTTPS origins. ASP.NET's standard
certificate configuration can also be used. For explicit local development only:

```yaml
listenUrls: [http://127.0.0.1:5080]
http:
  requestPrefix: /_taurus
  allowLoopbackHttp: true
```

Use literal loopback origins such as `http://127.0.0.1:5080` in the tenant's
issuer and application origins. HTTP is accepted only when the request's effective
remote address is loopback too. Development cookies have distinct names without
Secure; HTTPS cookies always use Secure, HttpOnly, host-only Path=/ and Strict
SameSite. Do not use development mode for a public origin.

[Forward-auth guide](forward-auth.md) starts with include/copy-ready
Nginx, Caddy and Traefik configurations, then explains trust boundaries and Apache
prerequisites. `example/` holds deployment examples; `deployment/` holds usable
snippets. Apache can proxy Taurus itself; its forward-auth adapter is pending.

`http.requestPrefix` defaults to `/_taurus`; `""` disables it. The same compiled
UI works at `<prefix>/ui/` without rebuilding. Keep its trailing slash. With no
prefix, reserve `/auth`, `/ui/` and `/taurus.v1.Authentication/`. `/health/live` and
`/health/ready` remain at the root. Configure exact tenant application origins
and specific trusted proxy IPs. Different application hosts get separate sessions.

Sessions use random opaque cookies; only digests are persisted. Each authorization
checks expiry, revocation and enabled-user status. Defaults are seven-day idle and
thirty-day absolute lifetime. Idle writes are throttled to five minutes or half
the idle lifetime. There is no JWT refresh service or positive authorization cache.
Only enabled authenticated tenant users are allowed today; resource permissions
and cross-domain SSO are future features. Browser CSRF proofs use an authenticated
seed and client salt, described in [security](../security/overview.md).
