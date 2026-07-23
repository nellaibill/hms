import { ApiError, NetworkError, type ApiErrorResponse } from '../errors';

/** Mirrors HMS.Shared.Kernel.ApiResponse<T> — see docs/ApiStandards.md §4. */
export interface ApiResponseEnvelope<T> {
  data: T;
  meta?: unknown;
  messages?: string[];
}

export interface HttpClientConfig {
  baseUrl: string;
  /** Reads the current access token. Not wired to anything yet — Authentication ships later. */
  getAuthToken?: () => string | null | undefined;
  getCorrelationId?: () => string | undefined;
}

export interface RequestOptions {
  query?: Record<string, string | number | boolean | undefined | null>;
  signal?: AbortSignal;
}

function buildUrl(baseUrl: string, path: string, query?: RequestOptions['query']): string {
  const normalizedBase = baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`;
  const url = new URL(path.replace(/^\//, ''), normalizedBase);

  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        url.searchParams.set(key, String(value));
      }
    }
  }

  return url.toString();
}

/**
 * Thin, platform-agnostic HTTP client shared by web and mobile (docs/FrontendArchitecture.md §6).
 * Builds requests per docs/ApiStandards.md conventions and unwraps the standard envelope, so
 * calling code (frontend/shared/api-client/services/*) never re-parses it. Fetch is available
 * in both the browser and React Native/Expo, so no platform-specific HTTP library is needed.
 */
export class HttpClient {
  constructor(private readonly config: HttpClientConfig) {}

  get<T>(path: string, options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return this.request<T>('GET', path, undefined, options);
  }

  post<T>(path: string, body?: unknown, options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return this.request<T>('POST', path, body, options);
  }

  put<T>(path: string, body?: unknown, options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return this.request<T>('PUT', path, body, options);
  }

  delete<T = void>(path: string, options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return this.request<T>('DELETE', path, undefined, options);
  }

  private async request<T>(
    method: string,
    path: string,
    body: unknown,
    options?: RequestOptions,
  ): Promise<ApiResponseEnvelope<T>> {
    const url = buildUrl(this.config.baseUrl, path, options?.query);

    const headers: Record<string, string> = {
      Accept: 'application/json',
    };

    if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
    }

    // TODO(Authentication iteration): attach Authorization header and implement the
    // refresh-then-retry-once flow described in docs/FrontendArchitecture.md §6.
    const token = this.config.getAuthToken?.();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const correlationId = this.config.getCorrelationId?.();
    if (correlationId) {
      headers['X-Correlation-Id'] = correlationId;
    }

    let response: Response;
    try {
      response = await fetch(url, {
        method,
        headers,
        body: body !== undefined ? JSON.stringify(body) : undefined,
        signal: options?.signal,
      });
    } catch (err) {
      // A deliberate cancellation (e.g. React Query tearing down a query on unmount,
      // notably under StrictMode's double-effect-invocation in development) must
      // surface as the original AbortError, not a NetworkError — callers rely on
      // recognizing cancellation to avoid treating it as a real failed request.
      if (err instanceof DOMException && err.name === 'AbortError') {
        throw err;
      }
      throw new NetworkError();
    }

    if (response.status === 204) {
      return { data: undefined as T };
    }

    const payload = await response.json().catch(() => undefined);

    if (!response.ok) {
      throw new ApiError(response.status, payload as ApiErrorResponse);
    }

    return payload as ApiResponseEnvelope<T>;
  }
}
