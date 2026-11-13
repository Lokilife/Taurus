# Taurus performance guidelines

Apply these rules when implementing and reviewing features. Security and correct
behavior remain prerequisites for every optimization.

## Allocations and execution

- Avoid unnecessary heap allocations. Reuse existing data and avoid temporary
  collections, captured closures, and repeated conversions.
- Avoid in-memory LINQ expressions. Use direct operations or explicit loops
  when processing collections. EF Core query expressions are different: keep
  filtering and projection in the database rather than loading data to avoid
  query syntax.
- Avoid repeated array reallocations and collection growth. Use known sizes or
  capacities when available; do not enumerate an input twice just to size it.
- Avoid boxing and unboxing. Prefer typed values and generic APIs over object
  containers for value types.
- Prefer stack storage when appropriate. Use spans and bounded stack buffers
  where they simplify allocation control; never size stack allocations directly
  from untrusted input or allocate them repeatedly inside loops.
- Do not use pooling or stack storage to retain secrets longer than necessary.
  Preserve cryptographic-library ownership and cleanup requirements.

## Clarity and dependency injection

Prefer declarative, explicit intent over hand-written imperative machinery when
it does not introduce unnecessary allocations. In-memory LINQ avoidance still
applies; do not use this preference to justify allocation-heavy pipelines.

Normal dependency-injection allocations are acceptable. Registration, startup,
and dependency creation have bounded lifetimes, but scoped and transient
services can also be created on every request. Do not assume all DI allocation
occurs only once during process startup.

On hot paths, scrutinize per-request dependency creation, mediator behaviors,
validation, logging, serialization, and persistence work. Performance has very
high priority there, while security and correctness must be preserved. Do not
bypass required validation or auditing to save allocations.

## Verification

Measure representative workloads before claiming a performance improvement.
Record allocation volume, latency, throughput, and deployment conditions when
evaluating hot paths. Avoid speculative complexity and unsupported performance
claims; currently no path has been established as hot through measurement.

## Future acceptance target

On one Taurus instance allocated **4 vCPU and 8 GB RAM**, target simultaneous
**2,500 OIDC refresh-token exchanges with JWT issuance per second** and
**500 completed OPAQUE logins per second**. Database and load generators run
outside that allocation. Refresh means a future OIDC token operation, not the
current cookie-session lookup or idle-expiry renewal. OIDC is not implemented;
this is a future performance goal, not a current release gate or measured claim.

Before validating it, pin workload/data size, hardware, database configuration,
OPAQUE profile, concurrency and latency/error budgets. Measure complete flows,
audit cost, CPU, memory and allocations under sustained combined load. Include
negative authentication cases and storage failures. Retain security validation,
rate limits and audit requirements while measuring.
