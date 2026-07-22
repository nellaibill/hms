# HMS — Hospital Management System

MVP Hospital Management System built as a Modular Monolith in a monorepo.

- **Frontend:** React (web) + React Native (mobile) + shared TypeScript — see [frontend/README.md](frontend/README.md)
- **Backend:** .NET 10 Modular Monolith + PostgreSQL — see [backend/README.md](backend/README.md)
- **Docs:** architecture, standards, and operational references — see [docs/README.md](docs/README.md)
- **CI/CD:** build/test/deploy scripts and pipeline config — see [cicd/README.md](cicd/README.md)

## Repository Layout

```
hms/
├── frontend/   # web, mobile, shared TypeScript
├── backend/    # .NET 10 modular monolith solution
├── docs/       # architecture & process documentation
├── cicd/       # CI/CD scripts and environment config
└── .github/    # GitHub Actions workflows
```

## Getting Started

See [docs/DevelopmentGuidelines.md](docs/DevelopmentGuidelines.md) for local environment setup.

## Status

This repository is currently a structural scaffold — folders, project files, and documentation templates only. No business logic, entities, endpoints, or authentication has been implemented yet.
