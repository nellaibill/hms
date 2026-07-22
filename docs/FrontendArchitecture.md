# Frontend Architecture — HMS

This document defines the frontend architecture for the Hospital Management System. It is a design document — it contains no source code, UI screens, React/React Native components, or business logic. It aligns with the approved [Architecture.md](Architecture.md) (repository architecture), the backend architecture, [DatabaseArchitecture.md](DatabaseArchitecture.md), and [ApiStandards.md](ApiStandards.md).

**Stack:** React + TypeScript + Vite (web), React Native + TypeScript + Expo (mobile), a shared TypeScript library (`frontend/shared`), consuming a .NET 10 Web API modular monolith over REST/JSON.

---

## 1. Overall Architecture

### Why three frontend projects
`web` and `mobile` are genuinely different runtimes — React renders to the DOM, React Native renders to native platform views. They cannot share UI code, so they must be separate projects with their own build tooling (Vite vs Expo). Everything that **isn't** UI, however — API contracts, validation rules, constants, HTTP/auth client logic — is plain TypeScript and can be written once. `shared` exists specifically to capture that non-UI logic so it is never duplicated between the two apps.

This mirrors the backend's own separation of concerns: just as backend modules separate Domain/Application from Infrastructure, the frontend separates "what the data and rules are" (`shared`) from "how it's presented" (`web`, `mobile`).

### Responsibilities of each project
| Project | Responsibility |
|---|---|
| `frontend/web` | React web application: routing, pages, UI components, browser-specific concerns (browser storage, responsive layout). |
| `frontend/mobile` | React Native application: navigation, screens, native UI components, mobile-specific concerns (secure keystore, platform permissions). |
| `frontend/shared` | Platform-agnostic TypeScript: API client, authentication client, HTTP client, DTOs, interfaces, enums, constants, utility functions, validation, error models. **No UI, no CSS, no platform-specific code.** |

### Communication between projects
`web` and `mobile` each depend on `shared` as an internal workspace library. `shared` depends on neither — the same zero-dependency-foundation rule the backend applies to `HMS.Shared.Kernel` applies here. `web` and `mobile` never depend on each other. Where a platform needs something `shared` can't provide directly (e.g., where the auth token physically lives), `shared` defines the contract (an interface) and each platform supplies its own implementation — `shared` never assumes a specific storage mechanism.

---

## 2. Folder Structure

The structure below refines the currently scaffolded folders (see [FolderStructure.md](FolderStructure.md)) to explicitly satisfy `shared`'s required responsibilities. Folders marked **(new)** extend what exists today and should be added when real implementation begins — this document does not create them.

### `frontend/web/`
```
web/
├── public/              # Static assets served as-is by Vite (favicon, etc.)
├── src/
│   ├── app/             # (new) Bootstrap: entry point, root App component, global provider composition
│   ├── assets/          # Images, fonts, icons bundled by the build
│   ├── components/      # Reusable, presentation-only UI building blocks used across features
│   ├── features/        # One folder per business capability, mirroring backend modules
│   ├── layouts/         # Shared page shells (authenticated layout, public layout)
│   ├── pages/           # Route-level components composing features into full pages
│   ├── routes/          # Route table, protected-route guard, lazy-loaded route definitions
│   ├── hooks/           # App-wide (non-feature-specific) hooks
│   ├── services/        # Web-specific glue around shared/api-client (e.g., browser storage adapter)
│   ├── store/           # Global client state (session/auth state, app-wide UI state)
│   └── config/          # Reads Vite environment variables into typed configuration
├── tests/
├── package.json, tsconfig.json, vite.config.ts
```

### `frontend/mobile/`
```
mobile/
├── app.config.ts         # (new) Expo app configuration
├── src/
│   ├── app/              # (new) Bootstrap: root component, navigation container, global providers
│   ├── assets/
│   ├── components/
│   ├── features/         # One folder per business capability, mirroring backend modules
│   ├── screens/          # Navigable screen components composing features (mobile's equivalent of `pages/`)
│   ├── navigation/       # Navigator definitions (stacks/tabs), route guards, linking config
│   ├── hooks/
│   ├── services/         # Mobile-specific glue around shared/api-client (secure keystore adapter)
│   ├── store/
│   └── config/           # Reads Expo configuration into typed configuration
├── tests/
├── package.json, tsconfig.json
```

**Note on `android/` and `ios/`:** with Expo's managed workflow, these native project folders are generated on demand (`expo prebuild`) rather than maintained by hand, and are typically not committed to source control at all. They are kept in the current scaffold only as placeholders; the team should decide — and record in [DecisionLog.md](DecisionLog.md) — whether to stay on managed workflow (recommended for MVP simplicity, no native tooling required) or move to bare workflow (only if a required native module forces it).

### `frontend/shared/`
```
shared/
├── api-client/          # HTTP client: base request/response handling, interceptors, endpoint builders
├── auth-client/         # (new) Authentication-specific client: login/logout/refresh calls, token-attach orchestration
├── dtos/                # (new) Request/response DTOs per backend module (e.g. dtos/patients, dtos/appointments)
├── types/                # Interfaces and common types not tied to a specific DTO (e.g. Result<T>, Paginated<T>)
├── enums/                # (new) Shared enums mirroring backend enums
├── constants/            # Shared constants (roles, route names, config keys)
├── validation/           # Shared validation schemas
├── errors/               # (new) Error model types matching the ApiStandards.md error envelope
├── utils/                # Pure, platform-agnostic utility functions
├── package.json, tsconfig.json
```

Every folder here maps directly to one of the responsibilities the project brief assigns to `shared`. Nothing in this list is a React/React Native component, a stylesheet, or platform-specific code — if a piece of logic needs the DOM, a native module, or JSX, it does not belong in `shared`.

---

## 3. Feature Organization

Each feature folder groups everything specific to one business capability and mirrors the backend module of the same name: `authentication`, `patients`, `appointments`, `billing`, `staff` (and future modules as the backend grows). Inside a feature folder:

- `components/` — UI pieces used only within this feature
- `hooks/` — data-fetching and stateful logic specific to this feature
- (occasionally) `services/` — feature-specific API composition, if it needs more than the generic shared API service provides

Web composes features into `pages/`; mobile composes features into `screens/`. Both consume `shared/dtos`, `shared/api-client`, `shared/validation`, and `shared/enums` for the module they mirror.

**Adding a new module** (e.g., a future Pharmacy module) always follows the same steps, regardless of which developer does it:
1. Add DTOs, validation schemas, and enums for the module under `shared/dtos/pharmacy`, `shared/validation/pharmacy`, `shared/enums/pharmacy`.
2. Add an API service in `shared/api-client` for the module's endpoints (per [ApiStandards.md](ApiStandards.md)).
3. Add `features/pharmacy/` to `web` and, if the module needs mobile support, to `mobile`.
4. Wire a route (web) or screen + navigator entry (mobile).

No module's implementation is generated here — only the repeatable pattern.

---

## 4. Routing Strategy

### Web
- **Public routes** — login, password reset, and similar unauthenticated pages, rendered inside a public layout.
- **Protected routes** — everything else, wrapped by a single route guard component that checks the global session state (§5) and redirects to login if absent. Implemented once, applied declaratively wherever a route needs it — never duplicated per page.
- **Lazy loading** — each feature's page component is code-split via dynamic import, so the initial bundle only includes what's needed for first paint. Route-level lazy loading is the standard granularity; splitting at a finer level is not needed for MVP.
- **Route organization** — a single `routes/` module maps paths to lazy-loaded page components, kept separate from the pages themselves — the same separation the backend keeps between its `Endpoints` layer and `Application` layer.

### Mobile
React Navigation organizes the app as a root navigator with two states, switched on the same global session state web uses:
- An **Auth navigator** (login and other public screens) shown when no valid session exists.
- An **App navigator** (the authenticated stack/tabs) shown once a session is restored or established.

Feature screens are registered as routes within the App navigator, grouped into stacks for multi-step flows within a feature, or tabs for top-level sections. This is the direct mobile equivalent of web's public/protected route split — one source of truth (session state) drives both.

---

## 5. State Management

A deliberately minimal, layered approach — no heavyweight global state framework for an MVP that is mostly CRUD over server data:

| Layer | Approach | Used for |
|---|---|---|
| Local state | React's built-in `useState`/`useReducer` | Component-local UI state (toggle, focus, expanded/collapsed) |
| Server state | A dedicated server-state library (e.g., TanStack Query) | Anything fetched from the API — handles caching, refetch, loading/error state, request de-duplication |
| Global state | A minimal store (React Context, or a lightweight library only if Context becomes unwieldy) | Genuinely cross-cutting client state only — session/auth state, app-wide UI state (theme, nav state) |
| Form state | A dedicated form library (e.g., React Hook Form) bound to `shared/validation` schemas | Form field values, touched/dirty state, validation results |

**Justification:** almost all "global" data in a CRUD-heavy hospital application — patients, appointments, billing records — is actually **server state**, not client UI state. Putting it in a hand-rolled global store duplicates what a server-state library already does well (caching, invalidation, refetching) and is a common source of unnecessary complexity. Reserving global state for session/auth and app-wide UI concerns keeps it small and easy for a 2–3 person team to reason about, consistent with this project's "avoid unnecessary complexity" constraint.

---

## 6. API Communication

- **API Client architecture** — `shared/api-client` exposes one configured HTTP client instance (base URL, default headers per [ApiStandards.md](ApiStandards.md)) and one service module per backend module's endpoint group (e.g., a patients API service, an appointments API service), built on top of that single client. Both `web` and `mobile` import these typed services rather than building their own request logic.
- **HTTP client** — a thin wrapper that builds requests per ApiStandards.md conventions (JSON content type, correlation ID header, versioned base path) and automatically unwraps the standard response envelope (`data`/`meta`/`messages`), so calling code receives an already-typed payload rather than re-parsing the envelope on every call.
- **Request interceptors** — attach `Authorization: Bearer {token}` (reading through the storage-adapter interface `shared/auth-client` defines, implemented per platform) and attach/propagate `X-Correlation-Id`.
- **Response interceptors** — unwrap the envelope, and detect the standard error shape from [ApiStandards.md](ApiStandards.md) §5, normalizing it into `shared/errors`' typed error model before it reaches feature code.
- **Token refresh strategy** — on a `401`, the client attempts exactly one token refresh and retries the original request once; if the refresh also fails, it surfaces a session-expired error and the app transitions to the logged-out state (§7). This logic lives once in `shared/auth-client`, so both platforms get it for free.
- **Error handling** — every API error reaching feature code is already the shared, typed error model — feature code never handles raw, unparsed HTTP responses (see §8).

---

## 7. Authentication Flow

*(Contract and flow only — the authentication mechanism itself is designed in [Authentication.md](Authentication.md); this section does not implement it.)*

- **Login flow** — credentials submitted → `shared/auth-client` calls the login endpoint → on success, access + refresh tokens are handed to the platform's storage adapter (web vs. mobile implementation, per Authentication.md) → global session state updates → protected routes/screens become reachable.
- **Logout flow** — clears stored tokens via the platform storage adapter, clears global session state, optionally calls a backend logout/revocation endpoint, and returns the app to the public route/navigator.
- **Token storage strategy** — `shared/auth-client` defines a small storage-adapter interface (get/set/clear token); each platform supplies its own implementation. Shared code never assumes a specific mechanism.
- **Session restoration** — on app start, the platform's storage adapter is checked for an existing valid (or refreshable) token before the protected app renders. If found, the user is silently restored into the authenticated state; otherwise, the public/auth flow renders.
- **Route protection** — web's route guard and mobile's navigator switch both key off the same global session state — one source of truth for "is the user logged in," consumed identically by both platforms.

---

## 8. Error Handling

- **Global error handling** — a single top-level error boundary per platform catches rendering errors that escape feature-level handling, shows a generic fallback, and logs the error — the frontend analog of the backend's single global exception handler ([ErrorHandling.md](ErrorHandling.md)).
- **Validation errors** — surfaced inline next to the offending field, using the `validationErrors` array from either client-side validation (§9) or the API's error response — never shown as a generic banner when a specific field is known.
- **Network errors** — connectivity/timeout failures are surfaced as a distinct, retryable state (e.g., "check your connection" plus a retry action), separate from the generic error fallback, since the resolution differs from a validation or auth failure.
- **Unauthorized responses** — a `401` triggers the token-refresh-then-retry flow (§6); if that fails, or a `403` is returned, the user is routed to the appropriate state (logged out, or a permission-denied message) rather than a raw error screen.
- **Retry strategy** — the server-state library's built-in retry/backoff covers transient failures on reads automatically. Mutating requests are never silently auto-retried (to avoid duplicate side effects); a failed mutation instead surfaces an explicit retry action to the user, consistent with the idempotency guidance in [ApiStandards.md](ApiStandards.md).

---

## 9. Form Validation

- **Shared validation** — rules that must match backend business rules (format, required fields, length) are defined once in `shared/validation` and imported by both `web` and `mobile` forms — this is the primary de-duplication benefit of the shared project.
- **Client validation** — forms validate against the shared schema before submission (via the form library resolving against it), giving immediate feedback without a network round-trip.
- **Server validation** — the backend remains the final authority (per [ApiStandards.md](ApiStandards.md) §7); client validation is a UX convenience, not a security boundary. The API client's error normalization (§6) maps server-returned `validationErrors` onto the same field-level display client validation uses, so forms have one error-rendering path regardless of where a failure originated.

---

## 10. Environment Configuration

| Environment | Web (Vite) | Mobile (Expo) |
|---|---|---|
| Development | `.env.development`, read via `import.meta.env` | `app.config.ts` + local env, read via `expo-constants` |
| Testing | A dedicated `.env.test` pointing at a test API/mock server | An equivalent Expo test configuration profile |
| Production | Variables injected at build time | Injected via EAS build profiles (`eas.json`) |

**Organization:** environment variable names are consistently prefixed per each tool's requirement (`VITE_` for web, `EXPO_PUBLIC_` for mobile) and documented in [Configuration.md](Configuration.md). The same source code is built once per environment with different injected configuration — never a forked or hand-edited build per environment. `shared` itself never reads environment variables directly; the platform-specific `config/` module resolves environment values and passes them into `shared`'s clients at initialization, keeping `shared` platform-agnostic.

---

## 11. Project Conventions

| Element | Convention | Example pattern |
|---|---|---|
| Folder naming | lowercase `kebab-case` | `api-client`, `lab-results` |
| File naming (components) | PascalCase | `PatientList.tsx` |
| File naming (non-component) | camelCase | `patientApi.ts`, `useAuth.ts` |
| Component naming | PascalCase, descriptive noun phrase, no generic suffix | `AppointmentCard`, not `Card2` |
| Hook naming | camelCase, always prefixed `use` | `usePatients`, `useAuth` |
| Service naming | suffixed `Api` for API-calling modules, `Service` for non-API business helpers | `patientsApi.ts` |
| DTO naming | suffixed `Dto`, mirroring backend DTO naming for easy cross-reference | `CreatePatientRequestDto`, `PatientResponseDto` |
| Type naming | PascalCase, no `I` prefix on interfaces; enums PascalCase with PascalCase members | `Patient`, not `IPatient` |

These extend, and do not duplicate, the general rules in [NamingConventions.md](NamingConventions.md).

---

## 12. Reusable Patterns

- **Feature modules** — each feature folder exposes a small, explicit public surface (a barrel file) so other parts of the app import from the feature's root rather than reaching into its internals — the same "public Contracts" discipline the backend applies to its modules.
- **API services** — one file per backend module's endpoint group, built on the shared HTTP client, returning already-typed DTOs. Feature code never calls the HTTP client directly, only through these typed service functions.
- **Custom hooks** — encapsulate a feature's data-fetching (wrapping the server-state library) or stateful UI logic, so components stay focused on rendering rather than orchestration.
- **Utilities** — pure, side-effect-free functions only (formatting, calculations), living in `shared/utils`, imported by both platforms — never platform-specific logic.
- **Constants** — enums and magic-string replacements defined once in `shared/constants`/`shared/enums`, imported everywhere, matching backend constants so the two never drift apart.
- **Error models** — a small set of typed error shapes in `shared/errors` mirroring the [ApiStandards.md](ApiStandards.md) error envelope, so every layer of the frontend handles one consistent error object rather than raw, untyped API responses.

---

## 13. Build & Deployment Considerations

- **Build configuration** — web builds via Vite (`vite build`), producing static assets deployable to any static host/CDN. Mobile builds via Expo Application Services (EAS) build profiles (development/preview/production), producing platform-specific binaries. `shared` is not published as a standalone package at MVP scale — it is referenced directly through the monorepo's workspace linking.
- **Environment separation** — each build profile/environment injects its own configuration at build time (§10); there is exactly one source code path per environment, never environment-specific forks.
- **Static assets** — images/fonts/icons live in each platform's own `assets/` folder, not shared — web and mobile have different asset pipelines and formats. Anything genuinely identical (e.g., a logo) is duplicated per platform rather than building a shared-asset pipeline for a marginal de-duplication win at MVP scale.
- **Versioning** — web's version follows the repository's overall release versioning (see [ReleaseNotes.md](ReleaseNotes.md)). Mobile additionally tracks native build/version numbers required by app stores (via `app.config.ts`), incremented per store submission independently of the web version.

No CI/CD implementation is defined here — build/deploy automation belongs to [Deployment.md](Deployment.md) and `cicd/`.

---

No source code, UI screens, components, or business logic were generated — this is architecture design only.
