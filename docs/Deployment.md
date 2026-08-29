# Deployment

## Purpose
This document explains how the application is built and deployed. The app ships as three
Docker images (`postgres`, `HMS.Api`, and an nginx-served frontend build) orchestrated by
the root `docker-compose.yml` — see that file plus `.env.example` for the runnable stack;
no separate orchestration platform (Kubernetes, etc.) is used at this MVP stage.

## Scope
Covers environments, build process, deployment process, database migration deployment, and rollback strategy.

**Out of scope:** the internal logic of CI scripts (see `cicd/scripts/`) and general configuration management (see [Configuration.md](Configuration.md)).

## When to Update This Document
- The deployment process or target environment changes.
- A new environment is introduced.
- The rollback strategy changes.

## Recommended Sections
- Overview
- Environments
- Build Process
- Deployment Process
- Database Migration Deployment
- Rollback Strategy
- Manual Deployment Steps (MVP fallback)

---

## Overview
_To be documented._

## Environments
_To be documented._

## Build Process
_To be documented._

## Deployment Process
_To be documented._

## Database Migration Deployment
Per [DatabaseArchitecture.md](DatabaseArchitecture.md)'s Migration Strategy: pending
migrations are applied as an explicit, logged step before the new application version
begins serving traffic — not automatically on every startup.

`dotnet HMS.Api.dll migrate` runs that step: it migrates the Platform database, seeds the
Platform Admin account, and — only when `Bootstrap:SeedLegacyTenant` resolves to `true`
(default) — also migrates and seeds the legacy tenant (`ConnectionStrings:Default`). It
then exits (code `0` on success, non-zero on failure) without starting Kestrel, so it's
safe to run as a one-off step ahead of starting the real app process, in any
`ASPNETCORE_ENVIRONMENT`. Set `Bootstrap__SeedLegacyTenant=false` for a real multi-tenant
production deployment — hospitals should be provisioned through the Register Hospital
flow, not the legacy dev/QA seed path.

This is the same migrate+seed logic `ASPNETCORE_ENVIRONMENT=Development` already runs
automatically on a plain `dotnet run`, for local convenience — `migrate` is the explicit
counterpart meant for a deploy pipeline or container entrypoint, usable in Production too.

## Rollback Strategy
_To be documented._

## Manual Deployment Steps
_To be documented._
