/**
 * A locally-unique id for client-only purposes (React list keys, an HTTP idempotency-key
 * header value) — never persisted or treated as a real UUID by the server. `crypto.randomUUID()`
 * only exists in a "secure context" (HTTPS or localhost); a plain-HTTP deployment (e.g. an
 * IP-address-only VM without TLS yet) has no `crypto.randomUUID` at all, which throws
 * "crypto.randomUUID is not a function" the moment any component that used it renders. This
 * falls back to a `Math.random()`-based id in that case — good enough for "don't collide
 * within this one browser tab's session," which is all either use case actually needs.
 */
export function generateClientId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}
