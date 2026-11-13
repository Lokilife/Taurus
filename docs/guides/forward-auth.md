# Forward authentication

## Ready-to-use configurations

Install `deployment/` alongside Taurus, for example in
`/usr/local/lib/taurus/deployment`. These snippets use the default `/_taurus`
prefix. Configure your application origin in Taurus, trust only the actual proxy
IP, and keep Taurus and the protected application's backend ports private.
TLS certificates and your application upstream remain deployment-specific.

### Nginx

Requires the `auth_request` module. In an HTTPS `server` block:

```nginx
set $taurus_backend http://127.0.0.1:5080;
set $taurus_app http://127.0.0.1:8080;
include /usr/local/lib/taurus/deployment/nginx.conf;
```

See [complete example](../../example/nginx/nginx.conf). The include contains
server-level locations, not an `http` block, and redirects unauthenticated
requests to the local Taurus login page. Do not combine it with conflicting
`location /` definitions. Literal IP upstreams avoid DNS resolver requirements.

### Caddy

```caddy
import /usr/local/lib/taurus/deployment/caddy.conf
app.example.com {
    vars taurus_backend 127.0.0.1:5080
    vars taurus_app 127.0.0.1:8080
    import taurus
}
```

The variables select Taurus and the protected backend. Caddy manages
public TLS according to its normal configuration. An unauthenticated response is
401; visit `https://app.example.com/_taurus/ui/` to sign in. See
[Caddy's forward_auth reference](https://caddyserver.com/docs/caddyfile/directives/forward_auth).

### Traefik

Load `deployment/traefik.yml` using the file provider as shown in
[the static example](../../example/traefik/traefik.yml). Copy the dynamic file and
edit its host and upstream URLs. Configure the `websecure` entry point and TLS
certificate/resolver separately. Authentication failures return 401; sign in at
`https://app.example.com/_taurus/ui/`. The middleware order is deliberate: remove
untrusted identity, authorize, then remove secrets before contacting the app.
See [the ForwardAuth reference](https://doc.traefik.io/traefik/middlewares/http/forwardauth/).

The supplied configurations strip **all cookies and Authorization** before the
protected backend receives a request. This prevents Taurus session/challenge
credentials leaking to applications. Applications needing their own cookies
require a reviewed cookie allowlist; these snippets do not preserve them.

### Apache

[The Apache example](../../example/apache/taurus.conf) publishes Taurus itself
with standard proxy modules. It does **not** authenticate an unrelated app.
Apache's bundled [mod_authnz_fcgi](https://httpd.apache.org/docs/current/en/mod/mod_authnz_fcgi.html)
uses the FastCGI authorizer protocol rather than Taurus's HTTP forward-auth
contract. An HTTP-to-authorizer adapter or an additional module needs a separate
implementation/dependency decision. Until then, place a supported forward-auth
proxy before an Apache application. Do not assume ProxyPass enforces login.

### Custom or disabled prefix

Set `http.requestPrefix` in Taurus (`""` disables it). No WebClient rebuild is
needed. Generate matching snippets into a new directory:

```sh
python3 scripts/render-proxy.py --prefix /identity --output /tmp/taurus-proxy
python3 scripts/render-proxy.py --prefix '' --output /tmp/taurus-no-prefix
```

Include/load those generated files instead. With no prefix, reserve `/auth`, `/logout`,
`/ui/`, and `/taurus.v1.Authentication/` for Taurus. Keep the trailing `/` on
login page URLs. The hash router and relative assets allow the same compiled UI
to run at each prefix.

## Log out from an application

Link to Taurus on the **same application origin**:

```html
<a href="/_taurus/logout">Sign out</a>
```

A user can also type that URL into the browser's address bar. Visiting it revokes
that browser's current session in the database, clears its session cookie, and
redirects to the Taurus login page. Application code does not need to obtain a
CSRF token or manage Taurus cookies for this navigation.

The route is `<requestPrefix>/logout`: use `/identity/logout` for prefix
`/identity`, or `/logout` when the prefix is disabled. Ready proxy includes
already forward the prefixed route to Taurus without applying application auth.
For a disabled prefix, regenerate the includes with `render-proxy.py`. Custom
proxy setups must likewise route the logout endpoint to Taurus before application
authentication and preserve the browser's Cookie and `Sec-Fetch-*` headers.
Admins can expose another public path through an explicit proxy mapping.

This signs out the visitor's session for that application origin. It does not
revoke other browsers, other application origins or another user's sessions.
Clear application-owned login state separately if the application maintains any.
No return URL is accepted; the destination is always Taurus's configured login
page. Repeated visits without a valid session still go to that page. Storage or
required audit failures return 503 rather than claiming that logout succeeded.

The GET endpoint requires user-activated, top-level navigation, identified by
Fetch Metadata headers: `Sec-Fetch-Mode: navigate`, `Sec-Fetch-Dest: document`,
`Sec-Fetch-User: ?1`, and `Sec-Fetch-Site: same-origin` or `none`. This prevents
cross-origin pages, images, frames, prefetches and background fetches from forcing
a logout. Missing or disallowed metadata returns 403; HEAD cannot revoke a session.
Use a normal same-origin link; background fetches and non-user-activated
redirects do not qualify. Clients
without these browser headers can use the existing gRPC Logout operation with
its CSRF proof. See the [Fetch Metadata specification](https://www.w3.org/TR/fetch-metadata/).

## What Taurus needs

A reverse proxy is recommended for public TLS and traffic controls, but Taurus
can serve HTTPS directly with a configured certificate. Local testing can enable
HTTP only for literal loopback origins; see [setup](setup.md).

Each protected public origin must appear in exactly one tenant's
`applicationOrigins`. Sessions belong to that tenant and exact origin. Users
sign in separately on each application host; cross-domain SSO is future work.
Current policy grants any enabled authenticated tenant user access to its
configured applications. Per-resource/group authorization is not implemented.

The auth endpoint is `GET <prefix>/auth`. Forward the browser's Cookie header,
preserve the **public Host including its port**, and supply the public scheme
through `X-Forwarded-Proto`. Adapters such as Traefik send the public host in
`X-Forwarded-Host`; Taurus accepts it only from configured trusted proxy IPs. Only configured trusted proxy addresses can supply
forwarded metadata. Taurus returns 204 and `X-Taurus-User-Id`, `X-Taurus-User`,
`X-Taurus-Tenant` for a valid session, 401 otherwise. User and tenant text is
URI escaped. Storage failures must deny access (5xx), never fall through to the
application. No response token is needed by the application.

## Advanced deployment and trust boundaries

Terminate public TLS at the proxy or Kestrel. Permit plain backend HTTP only on
loopback or an isolated trusted network. Bind interfaces deliberately. Restrict
backend access so clients cannot bypass authorization. Overwrite forwarded scheme
and client-address headers at the public edge; configure additional trusted hops
explicitly rather than trusting arbitrary incoming headers. Proxy metadata must
identify the protected public origin, not the private Taurus upstream.

Strip client-supplied identity headers before authorization and set only the
allowlisted fields returned by Taurus. Remove session/challenge cookies before
forwarding to the application. Do not log Cookie, Authorization, passwords,
OPAQUE messages, CSRF seeds/proofs or bodies. Do not cache authorization results;
per-request database checks allow immediate revocation. The auth subrequest has
no request body. Cap body sizes and request timeouts at the proxy too.

Keep Taurus paths outside the application's authentication middleware to avoid
login loops. Redirect only browser navigations when implementing custom 401
handling; API clients should receive 401. Same-origin return destinations are
validated by the UI. A shared host means shared browser trust: an application
with same-origin script execution can act as the user. Use distinct hosts for
mutually untrusted applications; an HttpOnly cookie alone cannot prevent XSS
from making authenticated same-origin requests.

Run the matching proxy's configuration validator and smoke-test valid/invalid
sessions, spoofed identity/forwarded headers, backend outages and cookie stripping
before exposing a deployment. The snippets are starting configurations; adapting
network topology still requires checking its trust boundaries.
