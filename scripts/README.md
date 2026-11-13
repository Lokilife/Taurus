# Taurus scripts

Run from the repository root. Build browser contracts before smoke tests.
Python scripts use only the standard library; `requirements.txt` has no packages.

- `python3 scripts/check-style.py`: checks handwritten namespaces and mechanical
  style rules, excluding generated output.
- `bash scripts/generate-web-contracts.sh`: generates pinned protobuf JS/gRPC-Web
  TS clients, normally through the web build. Requires restored Grpc.Tools,
  curl, tar and sha256sum. `TAURUS_PROTOC` overrides protoc.
- `python3 scripts/render-proxy.py --prefix /identity --output /tmp/proxy`:
  renders proxy snippets for another prefix. Use `--prefix ''` to disable it.
  Existing destinations are never overwritten.
- `node scripts/smoke.ts`: checks the actual login page/assets, JavaScript/.NET
  OPAQUE interoperability, CSRF, wire unknown-field rejection, revocation and
  user-navigation logout.
  Set `TAURUS_TEST_STANDALONE=true` for direct Taurus to also check the login
  redirect and forward-auth status codes/identity headers (proxies may keep the
  authorization endpoint internal).
  `TAURUS_TEST_ADMIN_ACCESS=allowed` additionally verifies the administration
  access check on an admin origin; `denied` verifies refusal on an application
  origin. Both check strict wire parsing and rejection after session revocation.
  Requires Node 24 and a running Taurus. Set `TAURUS_TEST_ORIGIN` and
  `TAURUS_TEST_PASSWORD`; optional `TAURUS_TEST_USERNAME` defaults to root and
  `TAURUS_TEST_PREFIX` to /_taurus (it may be empty). Use trusted TLS or explicit
  loopback development HTTP.
- `node scripts/browser-smoke.ts`: drives an isolated Chrome debugger at port
  9222. Uses the same origin/password/username/prefix settings. A proxy's mock
  application must display forwarded `user=` and `cookie=` fields. Checks login,
  logout, cookie flags, cookie exclusion, forged identity headers and browser
  errors. Set `TAURUS_TEST_STANDALONE=true` for direct Taurus. Set
  `TAURUS_TEST_LOGIN_REDIRECT=false` for proxies returning 401 (Caddy/Traefik).
- `python3 scripts/measure.py`: samples forward-auth performance. Requires
  `TAURUS_MEASURE_APPLICATION_HOST` and `TAURUS_MEASURE_SESSION`; other options
  are described in the script. This is not an OIDC refresh-token benchmark.

TypeScript tools reuse the WebClient's installed OPAQUE library and generated
contracts. No extra browser automation or Python dependency is installed.
Chrome is an external test prerequisite. Never use debugger profiles with real
credentials or expose the debugging port. Tests must not print secrets or page
bodies that might contain them. Do not disable TLS validation for public services.
See [forward auth](../docs/guides/forward-auth.md) for deployment checks.
