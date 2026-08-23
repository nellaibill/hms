# Build context is the repository root (see docker-compose.yml) — @hms/web depends on
# @hms/shared via a relative "file:../shared" reference, so both must be present at their
# real relative paths for npm to resolve it.

FROM node:20-alpine AS build
WORKDIR /src

# shared must be installed AND built (tsc -> dist/) before web's install, since web's
# file: dependency resolves to shared's package.json "main"/"types" (dist/index.js /
# dist/index.d.ts) — not its TypeScript source directly.
COPY frontend/shared/package.json frontend/shared/package-lock.json frontend/shared/
RUN npm --prefix frontend/shared ci
COPY frontend/shared frontend/shared
RUN npm --prefix frontend/shared run build

COPY frontend/web/package.json frontend/web/package-lock.json frontend/web/
RUN npm --prefix frontend/web ci
COPY frontend/web frontend/web

# Vite bakes VITE_* env vars into the JS bundle at build time, not runtime — the API's
# public URL has to be known here, before `vite build` runs.
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm --prefix frontend/web run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /src/frontend/web/dist /usr/share/nginx/html
COPY frontend/web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
