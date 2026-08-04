import { NetworkError } from '../errors';
import { HttpClient, type ApiResponseEnvelope, type HttpClientConfig, type RequestOptions } from './httpClient';

/**
 * Always-offline HttpClient for the demo branch — never calls fetch, immediately rejects
 * every request with NetworkError. Every *Api call then takes the exact same NetworkError
 * fallback path every module's mock store/hook already implements for "backend genuinely
 * unreachable" (see mockRolesStore.ts, masterStoreFactory.ts, mockProductsStore.ts,
 * mockPatientsStore.ts) — this class just guarantees that path is always taken, regardless
 * of whether a real backend happens to be reachable. Accepts the same config shape as
 * HttpClient (baseUrl is irrelevant here) purely so callers can keep wiring getAuthToken
 * unchanged even though it's never actually read.
 */
export class OfflineHttpClient extends HttpClient {
  constructor(config?: Omit<HttpClientConfig, 'baseUrl'>) {
    super({ baseUrl: '', ...config });
  }

  override get<T>(_path: string, _options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return Promise.reject(new NetworkError());
  }

  override post<T>(_path: string, _body?: unknown, _options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return Promise.reject(new NetworkError());
  }

  override put<T>(_path: string, _body?: unknown, _options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return Promise.reject(new NetworkError());
  }

  override delete<T = void>(_path: string, _options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return Promise.reject(new NetworkError());
  }

  override postFormData<T>(_path: string, _formData: FormData, _options?: RequestOptions): Promise<ApiResponseEnvelope<T>> {
    return Promise.reject(new NetworkError());
  }
}
