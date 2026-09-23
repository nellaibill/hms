# API load tests

Read-only [k6](https://k6.io) scripts for load-testing a deployed HMS API.

## Prerequisites

1. Install k6: `winget install k6` (or see k6.io/docs/get-started/installation).
2. **Raise the rate limit on the target first.** The API allows `RateLimiting:PerUserPermitLimit`
   requests/minute per user (default 300, ADR-084), so a single-token test otherwise
   measures the limiter rather than the server. On a staging host only, restart the API with e.g.:

   ```bash
   RateLimiting__PerUserPermitLimit=100000
   RateLimiting__PerIpCeilingPermitLimit=100000
   ```

   Put the defaults back afterwards. Never do this on production.
3. Log in to the web app as the account to test with. In the browser devtools console run
   `JSON.parse(sessionStorage.getItem('hms-session')).token` and put the value in an
   environment variable yourself. Don't commit it or paste it into a ticket or chat.

## Run

```bash
k6 run -e BASE_URL=http://<host>:58158 -e HMS_TOKEN=$HMS_TOKEN -e PROFILE=baseline scripts/load-test/read-baseline.js
```

`PROFILE` options:

| Profile    | Shape                                   | Use for                          |
|------------|-----------------------------------------|----------------------------------|
| `baseline` | 10 → 20 VUs, ~3 min                     | Latency baseline                 |
| `ramp`     | 10 → 100 VUs, ~8 min                    | Finding the breaking point       |
| `soak`     | 30 VUs for 30 min                       | Leaks / slow degradation         |

The run aborts automatically when more than 5% of requests fail or p95 latency goes over 5s. The
end-of-run summary breaks latency down per endpoint (the `name` tag).

Latency includes the network round trip from wherever k6 runs. For server-side
numbers, run k6 from a machine close to the API host.
