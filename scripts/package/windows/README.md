# Packaged Windows install

HMS shipped to a hospital as an installer, not as source code. One Windows service (`HMS`) runs the
self-contained `HMS.Api.exe`. It serves the API and the React app on one HTTPS port, so there is no
nginx, NSSM or .NET runtime to install, and no CORS. PostgreSQL 18 with pgvector runs next to it.

This folder holds the pieces the build script and installer use. So far:

- `appsettings.Production.template.json`: the install's settings. The installer copies it into the
  install folder as `appsettings.Production.json` and fills in each `__NAME__` placeholder.

## What the API does for a packaged install

- **Windows service.** `WindowsServiceConfiguration` reports to the Service Control Manager when the
  process runs as a service, and logs warnings and errors to the Windows Event Log. Register it with
  `sc.exe create HMS binPath= "<install folder>\HMS.Api.exe" start= auto`. No NSSM is needed.
- **Content root.** When the current folder has no `appsettings.json` (a service starts in
  `C:\Windows\System32`), the API switches to the folder `HMS.Api.exe` is in. So
  `HMS.Api.exe migrate` works from any folder.
- **React app.** `WebAppHostingConfiguration` serves a build copied into `<install folder>\webapp`
  (or the `WebApp:RootPath` setting). Build it with `VITE_API_BASE_URL=same-origin`, so the app calls
  the API on whatever address it was opened from. Files under `/assets` are cached for good, and `index.html`
  is re-checked on every load, so an upgrade never leaves browsers on the old app. Deep links get
  `index.html`. Paths under `/api`, `/uploads`, `/health`, `/swagger` and `/hangfire` never do.
- **Security headers.** App pages get a Content-Security-Policy that allows only this server's
  scripts plus the inline scripts in that exact `index.html`, by SHA-256 hash. API and upload paths
  keep the strict `script-src 'none'` policy.

## Placeholders

| Placeholder | Value |
|---|---|
| `__HTTPS_PORT__` | Usually `443` |
| `__CERTIFICATE_PFX_PATH__`, `__CERTIFICATE_PFX_PASSWORD__` | The hospital's server certificate (one CA per hospital, never the dev or staging one) |
| `__DB_PASSWORD__` | Random, letters and digits only (a `;` would break the connection string) |
| `__JWT_SIGNING_KEY__` | Random, 64+ bytes, Base64. Keep it on upgrades, or everyone is signed out |
| `__PLATFORM_ADMIN_FULL_NAME__`, `__PLATFORM_ADMIN_EMAIL__` | The Platform Admin account seeded on first install |
| `__PG_BIN_DIR__` | For example `C:\\Program Files\\PostgreSQL\\18\\bin` (JSON-escaped backslashes) |
| `__BACKUP_DIR__` | For example `C:\\HMS-Backups` |

Lock the file down to `SYSTEM` and `Administrators`: it holds the database password and the JWT key.

## Still to do

- The build script, the obfuscation step and the installer itself.
- File logging: as a service, the console output is lost, and only warnings and errors reach the
  Event Log.
- Patient files still live inside the install folder (`App_Data`, `wwwroot\uploads`). An upgrade
  must never delete them, and an uninstall must leave them alone.
