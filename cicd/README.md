# CI/CD

Supporting scripts and configuration for build, test, and deployment automation.

- `scripts/` — build/test/migrate/deploy scripts invoked by CI, kept portable and runnable locally.
- `environments/` — non-secret configuration templates per environment (dev/staging/production).
- `release/` — versioning notes and release checklists.

The actual GitHub Actions workflow entry point lives at [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) (required by GitHub) and stays thin, calling into `scripts/` here.

_Placeholder — see [docs/Deployment.md](../docs/Deployment.md)._
