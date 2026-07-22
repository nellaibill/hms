# GitHub Actions Workflows

## build.yml

Minimal build-validation workflow — no deployment. Runs on every push and pull request against `main`:

- **Backend job:** restores, builds, and tests `backend/HMS.sln` (.NET 10).
- **Frontend job:** installs, lints, and tests each of `frontend/web`, `frontend/mobile`, `frontend/shared` (Node).

Its purpose is to catch build breakage early, not to deploy anywhere — see [docs/Deployment.md](../../docs/Deployment.md) for the (currently manual, MVP-scoped) deployment process, and [cicd/](../../cicd/) for supporting scripts.
