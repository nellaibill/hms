# syntax=docker/dockerfile:1

# Build context is frontend/ (see docker-compose.yml) so this can npm-workspace-install and
# build @hms/shared before building web, exactly as .github/workflows/deploy-pages.yml does.

# glibc (not alpine/musl) — avoids npm's optional-dependency resolution bug
# (github.com/npm/cli/issues/4828) that drops rollup's musl-specific native binary when the
# lockfile wasn't generated on musl, which broke `vite build` under node:20-alpine.
FROM node:20-bookworm-slim AS build
WORKDIR /app

COPY package.json ./
COPY web/package.json web/package.json
COPY shared/package.json shared/package.json
# Deliberately NOT copying the repo's package-lock.json: it was generated on Windows, and
# npm's optional-dependency resolution (npm/cli#4828) then fails to install rollup/esbuild's
# Linux native binary under both `npm ci` and `npm install` when that lock is present — even
# on glibc. Installing without a lock lets npm resolve fresh against this container's actual
# platform. The repo's committed lockfile is untouched; this only affects the image build.
RUN --mount=type=cache,target=/root/.npm npm install

COPY shared/ shared/
RUN npm run build --workspace=shared

COPY web/ web/

# Baked in at build time (Vite inlines import.meta.env.* into the static bundle) — must be
# the browser-reachable API origin, not a container-internal one. Defaulted to the demo's
# published API port; override with --build-arg for a different port/host.
ARG VITE_API_BASE_URL=http://localhost:8080
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build --workspace=web

FROM nginx:alpine AS runtime
COPY web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/web/dist /usr/share/nginx/html

EXPOSE 80
