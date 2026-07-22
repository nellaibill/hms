# HMS Backend

.NET 10 modular monolith backend, backed by PostgreSQL. See [docs/Architecture.md](../docs/Architecture.md), [docs/FolderStructure.md](../docs/FolderStructure.md), and [docs/DatabaseGuidelines.md](../docs/DatabaseGuidelines.md) for the full design rationale.

```
backend/
├── HMS.sln
├── src/
│   ├── HMS.Api/                     # Composition root (host)
│   ├── Modules/
│   │   ├── Identity/HMS.Modules.Identity/
│   │   ├── Patients/HMS.Modules.Patients/
│   │   ├── Appointments/HMS.Modules.Appointments/
│   │   ├── Staff/HMS.Modules.Staff/
│   │   ├── Billing/HMS.Modules.Billing/
│   │   └── Notifications/HMS.Modules.Notifications/
│   ├── Shared/
│   │   ├── HMS.Shared.Kernel/
│   │   └── HMS.Shared.Infrastructure/
│   └── Database/
│       └── HMS.Database.Migrations/
└── tests/
    ├── HMS.UnitTests/
    ├── HMS.IntegrationTests/
    └── HMS.ArchitectureTests/
```

_Placeholder — no business logic, entities, endpoints, or authentication implementation yet. Projects are empty scaffolds wired together per the approved dependency flow._
