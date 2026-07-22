# Database Guidelines

## Purpose
This document defines conventions for schema design in the single shared PostgreSQL database used by the modular monolith, so modules stay logically isolated even though they share one physical database.

## Scope
Covers schema-per-module conventions, migration strategy, indexing guidelines, and data-ownership rules between modules.

**Out of scope:** module-specific data models (documented alongside each module), and naming syntax details (see [NamingConventions.md](NamingConventions.md)).

## When to Update This Document
- A new module requires its own schema.
- The migration process or tooling changes.
- A cross-module data access rule is clarified or changed.

## Recommended Sections
- Database Overview
- Schema-per-Module Convention
- Migration Strategy
- Naming Conventions (reference)
- Indexing Guidelines
- Data Ownership Rules Between Modules

---

## Database Overview
_To be documented._

## Schema-per-Module Convention
_To be documented._

## Migration Strategy
_To be documented._

## Naming Conventions
_To be documented. See [NamingConventions.md](NamingConventions.md)._

## Indexing Guidelines
_To be documented._

## Data Ownership Rules Between Modules
_To be documented._
