# HMS.Database.Migrations

Aggregates EF Core migrations for every module's `DbContext` into one coordinated deployment artifact against the single shared PostgreSQL database.

The Identity module's migrations live under `Identity/Migrations/` — see that folder's
README. Other modules have no `DbContext`/migrations yet.
