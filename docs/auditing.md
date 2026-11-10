# User audit and service diagnostics

## User audit

User logs remain in PostgreSQL or SQLite for this milestone. `IUserLogRepo` owns
append-only persistence; `RecordUserLogCommand` has a dedicated CQRS handler.
Authentication repositories enlist their security changes and user-audit events
in the same scoped database unit of work. Strict mode commits both atomically;
availability mode can commit security changes without audit records after an
audit failure. Bootstrap and destructive schema administration remain strict.

There is no automatic user-log deletion or generic update/delete repository API.
The repository rejects changes to committed audit events. A future retention
handler can implement explicit, bounded batches older than a configured cutoff,
including policies such as three calendar months. Until then, retention defaults
to keeping all user events.

A future dedicated audit backend belongs behind the audit boundary. Moving it
out of the identity database will require an outbox or equivalent delivery
protocol to preserve security-operation guarantees across storage boundaries.
Replacing the repository with an independent remote write alone is insufficient.
Database administrators can still modify database contents; this is not a claim
of cryptographic tamper evidence.

## Severity

- `None`: informational access without meaningful consequences.
- `Low`: consequential activity of limited investigative importance, such as an
  avatar change. Current logout events use this level.
- `Middle`: events useful in security investigations, including failed login
  attempts, recovery requests left unused, or session IP changes. Current login
  success/failure events use this level.
- `High`: sensitive changes that can have irreversible consequences, including
  password/email changes, user blocks and group changes. Current bootstrap,
  administrator-policy changes and schema changes use this level.

Producers must explicitly set severity. The examples guide future features;
failed-attempt aggregation, recovery workflows and IP-change detection are not
implemented by introducing the enum.

The database has a conservative `High` default, but application producers must
set severity explicitly. Explicit `None` remains `None`; the default does not
override a supplied value.

## Service files

Operational records use two active JSON-lines files, configured through YAML:

```yaml
serviceLogs:
  directory: logs
  maximumFileBytes: 10485760 # 10 MiB per active file
  retainedFiles: 3
  minimumLevel: Information
```

`access.log` records endpoint templates, parsed client IPs, protocol and status.
HTTP and gRPC status codes are distinguished by the protocol field. It never
records raw URLs, query strings, cookies, authorization headers or request bodies.
Health probes are excluded. Rejected host/scheme requests are recorded without
including their supplied URL.

`service.log` records diagnostic category, level, event ID and exception type.
Taurus's own fixed diagnostic messages are included. Framework message payloads
and exception messages are omitted because they can contain request data.
Future diagnostics must follow the same secret-handling rules.

Each file rotates by size and retains the configured number of numbered backups.
A single complete record can exceed the threshold; rotation happens at record
boundaries. Runtime file failures produce a generic stderr diagnostic without
falling back to the database. Use a distinct directory per process/replica and
protect the directory with appropriate filesystem permissions.

## OPAQUE development reset

The pre-1.0 OPAQUE change replaces migration history with a generated `Initial`
for each provider. Earlier service-log-table upgrade migrations are historical,
not a supported upgrade path from those development databases. Existing operator
data is not automatically deleted. Only explicitly selected disposable test
databases may be recreated by tests.

`LoginStarted` records the start of an OPAQUE attempt at Middle severity, including
when a client later abandons the exchange after an incorrect password. It does
not assert that authentication succeeded. A consumed finish records the final
Login outcome. No OPAQUE message, credential record or browser proof is included.
