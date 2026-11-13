# Operations

Initialize Taurus using the [setup guide](setup.md) first. Run maintenance
commands from the repository root when using the examples below.

## Attribute schemas and audit

Attribute schemas live in YAML, with snapshots and values in the database.
Incompatible changes block web startup. Preview and explicitly approve changes:

```sh
dotnet run --no-build --project src/Taurus.Server -- --config config/taurus.yml schema-plan
dotnet run --no-build --project src/Taurus.Server -- \
  --config config/taurus.yml schema-approve FINGERPRINT
```

Approval erases affected values transactionally with the snapshot and audit.
Required missing values need a separate data migration. Back up before approval.
User audit is append-only through its own boundary; service diagnostics/access
use rotating files. See [auditing](../security/auditing.md). Audit failure mode `Block`
is the default; `Allow` explicitly permits gaps for session operations. Schema
administration and bootstrap always require audit persistence.
