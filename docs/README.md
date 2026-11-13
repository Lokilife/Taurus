# Taurus documentation

## Guides

- [Setup and configuration](guides/setup.md): configuration discovery, secrets,
  key generation, database initialization and direct serving.
- [Forward authentication](guides/forward-auth.md): ready proxy configurations,
  supported integrations and deployment trust boundaries.
- [Development and testing](guides/development.md): prerequisites, build,
  publishing, automated checks and integration tests.
- [Operations](guides/operations.md): attribute schema changes and audit settings.

## Security

- [Security model](security/overview.md): OPAQUE, sessions, CSRF, transport,
  persistence and key-operation boundaries.
- [Auditing and service diagnostics](security/auditing.md): severity, atomicity,
  retention and rotating operational logs.

## Development

- [Architecture and CQRS](development/architecture.md): application layers,
  feature design and persistence boundaries.
- [Code style](development/code-style.md): formatting, file organization,
  namespaces, typed values and WebClient conventions.
- [Performance guidelines](development/performance.md): allocations,
  dependency injection, measurement and the future throughput target.
- [Release checklist and roadmap](development/todo.md): completed work,
  release gates and planned milestones.
- [Validation results](development/validation.md): recorded build, test,
  browser and deployment checks, with their limitations.
- [Historical resource baseline](development/baseline.md) and
  [raw measurements](development/baseline.json): measurements of the earlier
  implementation, not current OPAQUE performance claims.

## Decisions

- [Dependency decisions](decisions/dependencies.md): selected libraries,
  compatibility evidence and outstanding dependency reviews.

## Repository references

- [Project overview](../README.md)
- [Contributor and agent guidelines](../AGENTS.md)
- [Script reference](../scripts/README.md)
- [Sample configuration](../config/taurus.yml)
