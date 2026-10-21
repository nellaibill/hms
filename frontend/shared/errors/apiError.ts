/** Mirrors HMS.Shared.Kernel.ApiErrorResponse — see docs/ApiStandards.md §5. */
export interface ValidationErrorItem {
  field: string;
  message: string;
}

export interface ApiErrorResponse {
  errorCode: string;
  message: string;
  validationErrors?: ValidationErrorItem[];
  correlationId: string;
  timestamp: string;
}

/**
 * What an error body can actually look like on the wire. Most API errors are a full
 * ApiErrorResponse, but some responses carry none: the auth middleware's 401/403 have an empty
 * body, nginx answers 502/504 with an HTML page, and a framework ProblemDetails has `title`
 * instead of `message`. The HTTP client passes whatever it could parse, or nothing.
 */
export type ApiErrorBody = Partial<ApiErrorResponse> & { title?: string };

/**
 * The message for an HTTP status when the server sent none of its own. Desk staff read these
 * directly, so each one says what kind of problem it is and what to try. The status code goes
 * at the end so IT can tell the cases apart.
 */
export function messageForHttpStatus(status: number): string {
  switch (status) {
    case 400:
      return 'The server could not accept this request. Check the details you entered and try again. (Error 400)';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return "You don't have permission to do this. Ask your administrator to check your role's permissions. (Error 403)";
    case 404:
      return 'The record or page was not found. It may have been deleted or moved. (Error 404)';
    case 408:
      return 'The request took too long. Check the network connection and try again. (Error 408)';
    case 409:
      return 'This record was changed by someone else at the same time. Reload the page and try again. (Error 409)';
    case 413:
      return 'The file or data is too large to send. Try a smaller file. (Error 413)';
    case 429:
      return 'Too many requests in a short time. Wait a moment and try again. (Error 429)';
    case 502:
    case 503:
      return `The HMS server is not responding right now; it may be restarting. Wait a minute and try again. If it keeps happening, contact IT. (Error ${status})`;
    case 504:
      return 'The HMS server took too long to respond. Try again in a moment. If it keeps happening, contact IT. (Error 504)';
  }
  if (status >= 500) {
    return `Something went wrong on the HMS server. Try again. If it keeps happening, contact IT. (Error ${status})`;
  }
  return `The request failed. Try again. If it keeps happening, contact IT. (Error ${status})`;
}

/**
 * Typed error thrown by the shared HTTP client for every non-2xx API response, so every
 * layer of the frontend handles one consistent error object (docs/FrontendArchitecture.md §12).
 * `message` is always readable: the server's own message when it sent one, otherwise
 * messageForHttpStatus().
 */
export class ApiError extends Error {
  readonly errorCode: string;
  readonly status: number;
  readonly validationErrors?: ValidationErrorItem[];
  readonly correlationId: string;
  readonly timestamp: string;

  constructor(status: number, body?: ApiErrorBody | null) {
    const parsed = body && typeof body === 'object' ? body : undefined;
    super(parsed?.message || parsed?.title || messageForHttpStatus(status));
    this.name = 'ApiError';
    this.status = status;
    this.errorCode = parsed?.errorCode ?? `HTTP.${status}`;
    this.validationErrors = parsed?.validationErrors;
    this.correlationId = parsed?.correlationId ?? '';
    this.timestamp = parsed?.timestamp ?? '';
  }
}

const OFFLINE_MESSAGE = 'This device is offline. Check the network cable or Wi-Fi, then try again.';
const UNREACHABLE_MESSAGE =
  "Can't reach the HMS server. Check this device's network connection. If other websites open, the HMS server may be down; contact IT.";

/** True only when the platform positively reports no network (browsers; React Native has no navigator.onLine). */
function isKnownOffline(): boolean {
  const nav = (globalThis as { navigator?: { onLine?: boolean } }).navigator;
  return nav?.onLine === false;
}

/**
 * Thrown when the request never got an HTTP response: the device is offline, the server is
 * unreachable, or the browser blocked it (e.g. an untrusted certificate). Distinct from
 * ApiError. With no message given, it says which of those it can tell apart.
 */
export class NetworkError extends Error {
  constructor(message?: string) {
    super(message ?? (isKnownOffline() ? OFFLINE_MESSAGE : UNREACHABLE_MESSAGE));
    this.name = 'NetworkError';
  }
}
