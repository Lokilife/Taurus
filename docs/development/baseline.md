# Milestone-one resource baseline

Historical measurements: collected before the 2026-11-07 change from database
service logs to rotating files. They do not describe the current logging path
or the OPAQUE authentication implementation.

Collected on 2026-11-04 using the Release publish and default ASP.NET server GC.

## Environment and workload

- Ubuntu 24.04, x86-64, Intel Core i5-7300HQ under virtualization.
- Four visible logical CPUs; container quota of two CPUs and 4 GiB memory.
- .NET SDK 10.0.403, ASP.NET runtime 10.0.14, PostgreSQL 16.15.
- One tenant and one enabled root user in each freshly provisioned database.
- Argon2id: 64 MiB, three passes, one lane; maximum two concurrent checks.
- User and service auditing enabled with strict user-audit failure behavior.
- Local HTTP requests simulate the configured trusted HTTPS proxy boundary.

## Results

RSS was sampled immediately after readiness, before the login workload. It
excludes the database server, Nginx, browser and client processes. Startup timing
includes configuration, model/schema checks and existing-root provisioning checks.

| Provider | Startup (s) | RSS at readiness (MiB) |
| --- | ---: | ---: |
| Sqlite | 4.13 | 263.9 |
| PostgreSql | 3.30 | 276.1 |

Login values are medians of five sequential attempts per category, including
database and audit work. Samples are small and were collected on shared hardware.

| Provider | Success (ms) | Wrong password (ms) | Unknown user (ms) |
| --- | ---: | ---: | ---: |
| Sqlite | 1750.1 | 590.6 | 586.8 |
| PostgreSql | 619.6 | 589.8 | 578.1 |

Forward-auth results use 200 requests per case after ten warmup requests, with
one fresh HTTP connection per request and persistent service-log writes enabled.

| Provider | Concurrency | Median ms | p95 ms | Requests/s |
| --- | ---: | ---: | ---: | ---: |
| Sqlite | 1 | 2.55 | 3.58 | 365.6 |
| Sqlite | 4 | 5.56 | 13.96 | 357.8 |
| PostgreSql | 1 | 4.12 | 5.63 | 233.2 |
| PostgreSql | 4 | 31.70 | 54.67 | 125.6 |

The framework-dependent publish occupied approximately 63 MiB on disk.

## Interpretation and reproduction

These measurements establish a functionality baseline. Memory use remains an
optimization target for constrained homelabs; a 256 MiB deployment budget has
not been established. A workstation-GC comparison did not demonstrate a material
RSS reduction, so the default ASP.NET GC configuration remains unchanged.

The small workload and client connection overhead limit capacity conclusions.
Use representative data volumes, sustained concurrency and production audit
storage when assessing enterprise capacity. Timing samples do not prove absence
of authentication side channels.

Build the web client, publish Server in Release mode, migrate and bootstrap a
fresh database, then start the published service. Poll `/health/ready` and sample
`VmRSS` from `/proc/PID/status` at readiness. Obtain a session through login and
run `scripts/measure.py` using environment variables:

```sh
export TAURUS_MEASURE_HOST='127.0.0.1:5080'
export TAURUS_MEASURE_APPLICATION_HOST='app.localhost'
# Set TAURUS_MEASURE_SESSION to the test session cookie value without logging it.
TAURUS_MEASURE_COUNT=200 TAURUS_MEASURE_CONCURRENCY=1 python3 scripts/measure.py
TAURUS_MEASURE_COUNT=200 TAURUS_MEASURE_CONCURRENCY=4 python3 scripts/measure.py
```

The measurement connection must originate from a configured trusted proxy IP.
See [raw measurements](baseline.json) for the recorded values.
