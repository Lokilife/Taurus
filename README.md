# Taurus

Taurus is a lightweight high-performance identity-provider.
Taurus being developed as a solution for homelabs but also being capable of enterprise loads.

> **ATTENTION!**
> Early stages of development. Most of claimed features not implemented yet.
> Return back later, when 1.0.0 will be released.
> [Here is the checklist that I update frequently](docs/development/todo.md)

Features:
- Many authentication methods: password, WebAuthN, PKI, OTP, TOTP
- Gateways for: OIDC/OAuth2, LDAP, Kerberos
- Augmented PAKE in password-based logins instead of hashing
- Proof-of-work endpoint protection with dynamic complexity
- Different authentication policies for any needs (email-only login, MFA and more)
- Supported databases: PostgreSQL, SQLite

## Documentation

- [Documentation index](docs/README.md)
- [Setup and configuration](docs/guides/setup.md)
- [Forward authentication](docs/guides/forward-auth.md)
- [Development and testing](docs/guides/development.md)
- [Operations](docs/guides/operations.md)
- [Security model](docs/security/overview.md)
