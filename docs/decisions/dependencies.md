# Dependency decisions

The user approved EF Core and PostgreSQL/SQLite providers, YamlDotNet,
SQLitePCLRaw, Microsoft configuration/options, MediatR, FluentValidation,
FluentResults, protobuf/gRPC and static SvelteKit/browser gRPC-Web tooling.
Discuss additional dependencies before adopting them.

## Password protocol

Vaultic.OPAQUE.Net **1.4.0** and **@serenity-kit/opaque 1.1.0** are approved for
OPAQUE. The .NET package includes platform-native libraries and has no managed
package dependencies for its net8.0 target. Its package source commit is
`723ea49a9a9e9a9dc9f0c9eb052246c3fa2962c8`.
The temporary Isopoh Argon2 implementation and its direct dependency were removed.
The libraries perform cryptography, including client key stretching; Taurus does
not implement OPAQUE, Argon2 or another cryptographic algorithm itself.

A JavaScript client registered and logged in against the .NET server in an
isolated compatibility spike, producing matching session keys. Reproducible
server login is exercised with `node scripts/smoke.ts` after local enrollment.
This is interoperability evidence, not an independent cryptographic audit.
The JS project's historical audit is by 7ASecurity with Open Technology Fund
support, not Red Hat; its version and scope do not automatically cover these
versions, the wrapper or Taurus. Security review remains a release gate.

The native wrapper requires in-process exportable setup material today. Future
HSM support cannot be claimed solely because Taurus defines interfaces. See
[security](../security/overview.md) for compromise and browser-delivery limits.

## Web build

The user explicitly approved SvelteKit 3, its Vite/Svelte/TypeScript toolchain
upgrade and the required `@opentelemetry/api` peer dependency. Configuration is
TypeScript in `vite.config.ts`. Handwritten browser/tooling code is TypeScript;
styles live next to components in CSS. The lockfile records exact versions.

The upgraded dependency audit currently reports four low-severity entries in the
Kit/plugin/adapter dependency graph, originating from deepmerge prototype
poisoning. Upgrade alone is not a clean-audit claim;
review current advisories and their applicability before distribution. No Node
server, SSR production process or server-side form actions are deployed.

Pinned JavaScript protobuf/gRPC-Web generators produce ignored CommonJS and
TypeScript clients. Generator output and EF migrations remain generator-owned.
The portable static adapter converts generated absolute asset URLs to relative
ones and regenerates inline-script CSP hashes using Node's crypto API. SvelteKit
3 hash fallback output currently keeps absolute URLs despite `paths.relative`.
The build step owns this transformation; generated output is never hand-edited.
Build static assets before building or publishing Server to avoid asset races.
Node is a build/test dependency only, with no additional production runtime.

## Operational tooling

File rotation, service diagnostics, key operations and CSRF use existing .NET
APIs; browser hashing/randomness use WebCrypto. Python scripts use only the
standard library (`scripts/requirements.txt` is intentionally empty of packages).
No new logging, mail, queue, Apache adapter or proxy-plugin dependency was added.
