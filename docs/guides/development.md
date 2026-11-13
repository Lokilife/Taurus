# Development and testing

Run the following commands from the repository root.

## Build

Install .NET 10, Node.js 22.12+ (or supported newer LTS) and npm. Linux x86-64
browser generators are automated; other platforms require compatible generators.

```sh
dotnet restore
dotnet tool restore
npm --prefix src/Taurus.WebClient ci
npm --prefix src/Taurus.WebClient run build
dotnet build -m:1
```

Build the web client before Server; never run those builds concurrently.
Generator downloads are pinned and checked. `TAURUS_PROTOC` selects a different
protoc. Generated clients and `wwwroot` are ignored. The SvelteKit application
is static: no Node runtime is needed in production. The OPAQUE .NET package
includes native libraries; platform support must match those packaged runtimes.

```sh
dotnet publish src/Taurus.Server -c Release -o artifacts/server
```

Install the ASP.NET runtime on the target, then run the published server from its
directory.

## Checks and integration tests

```sh
python3 scripts/check-style.py
dotnet test -m:1
# Deletes and recreates only the explicitly supplied disposable test database:
TAURUS_TEST_POSTGRESQL='Host=localhost;Database=taurus_test;Username=taurus' \
  dotnet test -m:1
TAURUS_TEST_ORIGIN=http://127.0.0.1:5080 \
TAURUS_TEST_PASSWORD="$TAURUS_ROOT_PASSWORD" node scripts/smoke.ts
```

The TypeScript smoke script requires Node 24 for native type stripping and a
running configured Taurus. It checks JavaScript/.NET OPAQUE interoperability,
CSRF, strict wire parsing and revocation. `TAURUS_TEST_PREFIX` selects another
prefix, including empty. Use trusted local TLS for HTTPS tests. PostgreSQL tests
are skipped when no disposable connection is supplied. See
[scripts](../../scripts/README.md), [dependencies](../decisions/dependencies.md),
[performance](../development/performance.md) and
[architecture](../development/architecture.md).
