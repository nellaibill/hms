<#
  Builds the React frontend for production into frontend/web/dist - the static files nginx
  (nginx-hms-reverse-proxy.conf) serves. Replaces running `npm run dev` on a server: the Vite
  dev server compiles every module on request (~75 requests / 4.4 MB just for /login, 8-19 s
  page loads on staging), serves the TypeScript source to anyone, and keeps a stale
  pre-bundled @hms/shared whenever frontend/shared changes without a rebuild.

  Always rebuilds @hms/shared first: web consumes shared's compiled dist/, so building web
  against an old shared dist ships old API methods (this is how Patient Details' Medical
  Information tab broke on staging - a missing opdConsultationApi.listByPatient).

  Usage (on the VPS, after `git pull`):
    powershell -ExecutionPolicy Bypass -File build-frontend.ps1 -ApiBaseUrl "http://162.35.105.234"

  -ApiBaseUrl is baked into the bundle at build time (see docs/Deployment.md step 3): use the
  public origin nginx serves on (same-origin /api proxy), or "http://<ip>:58158" if the
  browser still calls the API port directly.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path,
    # Skip `npm ci` when node_modules is already current - saves a few minutes per deploy.
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'

function Invoke-Npm {
    param([string]$Dir, [string[]]$NpmArgs)
    Write-Host "npm $($NpmArgs -join ' ')  ($Dir)" -ForegroundColor Cyan
    & npm --prefix $Dir @NpmArgs
    if ($LASTEXITCODE -ne 0) { throw "npm $($NpmArgs -join ' ') failed in $Dir (exit $LASTEXITCODE)." }
}

$shared = Join-Path $RepoRoot "frontend\shared"
$web = Join-Path $RepoRoot "frontend\web"

if (-not $SkipInstall) { Invoke-Npm $shared @('ci') }
Invoke-Npm $shared @('run', 'build')

if (-not $SkipInstall) { Invoke-Npm $web @('ci') }
# Clear Vite's dev pre-bundle cache too, so a later `npm run dev` on this machine can never
# serve an older @hms/shared than the one just built.
$viteCache = Join-Path $web "node_modules\.vite"
if (Test-Path $viteCache) { Remove-Item -Recurse -Force $viteCache }

$env:VITE_API_BASE_URL = $ApiBaseUrl
try {
    Invoke-Npm $web @('run', 'build')
} finally {
    Remove-Item Env:VITE_API_BASE_URL -ErrorAction SilentlyContinue
}

$dist = Join-Path $web "dist"
Write-Host "Built $dist (API base URL: $ApiBaseUrl)." -ForegroundColor Green
Write-Host "nginx serves it directly - no restart needed. Verify: curl http://localhost/health" -ForegroundColor Green
