// Read-only API load test for HMS (k6 — https://k6.io).
//
// Usage (see README.md in this folder):
//   k6 run -e BASE_URL=http://host:58158 -e HMS_TOKEN=... -e PROFILE=baseline read-baseline.js
//
// GET requests only, so it's safe against any environment's data. It still counts against
// the API's rate limiter (RateLimiting section of appsettings.json, ADR-084): one token is one
// "user" partition, so raise RateLimiting__PerUserPermitLimit on the target first or the run
// measures the limiter, not the server.
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL;
const TOKEN = __ENV.HMS_TOKEN;
if (!BASE_URL || !TOKEN) {
  throw new Error('Set BASE_URL and HMS_TOKEN (-e BASE_URL=... -e HMS_TOKEN=...)');
}

const profiles = {
  // Light baseline: latency numbers at modest concurrency.
  baseline: [
    { duration: '30s', target: 10 },
    { duration: '2m', target: 20 },
    { duration: '30s', target: 0 },
  ],
  // Step up until the thresholds below abort the run.
  ramp: [
    { duration: '1m', target: 10 },
    { duration: '2m', target: 25 },
    { duration: '2m', target: 50 },
    { duration: '2m', target: 100 },
    { duration: '1m', target: 0 },
  ],
  // Steady load for leaks / slow degradation.
  soak: [
    { duration: '1m', target: 30 },
    { duration: '30m', target: 30 },
    { duration: '1m', target: 0 },
  ],
};

export const options = {
  stages: profiles[__ENV.PROFILE || 'baseline'],
  thresholds: {
    http_req_failed: [{ threshold: 'rate<0.05', abortOnFail: true, delayAbortEval: '20s' }],
    http_req_duration: [{ threshold: 'p(95)<5000', abortOnFail: true, delayAbortEval: '20s' }],
  },
  // Per-endpoint p95 in the end-of-run summary.
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

// [path, relative weight] — mirrors what the dashboard / list pages fetch.
const endpoints = [
  ['/api/v1/patients?page=1&pageSize=20', 4],
  ['/api/v1/patients?search=a&page=1&pageSize=20', 3],
  ['/api/v1/opd/patients', 3],
  ['/api/v1/opd/consultations/summary', 2],
  ['/api/v1/billing/invoices?page=1&pageSize=20', 3],
  ['/api/v1/ipd/dashboard', 1],
  ['/api/v1/ipd/admissions', 2],
  ['/api/v1/ipd/beds', 1],
  ['/api/v1/laboratory/orders', 2],
  ['/api/v1/laboratory/orders/dashboard-summary', 1],
  ['/api/v1/masters/consultants', 1],
  ['/api/v1/hr/dashboard', 1],
];
const bag = endpoints.flatMap(([path, weight]) => Array(weight).fill(path));
const params = (path) => ({
  headers: { Authorization: `Bearer ${TOKEN}`, Accept: 'application/json' },
  tags: { name: path.split('?')[0] + (path.includes('search=') ? '?search' : '') },
});

export default function () {
  const path = bag[Math.floor(Math.random() * bag.length)];
  const res = http.get(`${BASE_URL}${path}`, params(path));
  check(res, {
    'status 200': (r) => r.status === 200,
    'not rate limited': (r) => r.status !== 429,
  });
}
