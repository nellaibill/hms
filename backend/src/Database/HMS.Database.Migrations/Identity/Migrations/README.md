# Identity Migrations

`20260723020633_InitialCreateUsers.cs`, its `.Designer.cs` companion, and
`IdentityDbContextModelSnapshot.cs` are tool-generated (`dotnet ef migrations add`) from
the model in `HMS.Modules.Identity.Infrastructure` (see `IdentityDbContext` and
`Configurations/UserConfiguration.cs`) — they create the `identity` schema and the
`identity.users` table with the naming, audit-column, soft-delete, and indexing standards
from [docs/DatabaseArchitecture.md](../../../../../../docs/DatabaseArchitecture.md).

This migration set replaces an earlier hand-authored `20260722120000_InitialCreateUsers.cs`
that shipped without its `.Designer.cs`/`ModelSnapshot.cs` companions (the .NET SDK wasn't
available when it was authored). Without those files EF Core's tooling doesn't recognize the
class as a real migration at all (`dotnet ef database update` reported "No migrations were
found in assembly..."), so it could never actually be applied. It also named the primary key
constraint `pk_users` by hand-guessing, which happened to diverge from what the model would
really have produced (`UserConfiguration` didn't set an explicit key name at the time), so
regenerating it surfaced that gap too — `UserConfiguration.HasKey(...)` now sets
`.HasName("pk_users")` explicitly.

To regenerate after a future model change, from `backend/src/Database/HMS.Database.Migrations`:

```bash
dotnet ef migrations add <DescriptiveName> --context IdentityDbContext --output-dir Identity/Migrations
```

(`IdentityDbContextFactory` supplies the design-time connection string, so no
`--startup-project` is required; set `HMS_DESIGN_TIME_CONNECTION_STRING` to point at a
real Postgres instance if the factory's fallback isn't reachable.)
