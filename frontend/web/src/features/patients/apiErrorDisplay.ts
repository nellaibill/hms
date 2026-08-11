import { ApiError, NetworkError } from '@hms/shared';

/**
 * Normalizes a mutation error into the ApiError shape the patient forms already know how to
 * render — a NetworkError (server unreachable) becomes a synthetic ApiError with a message
 * that's explicit about nothing having been saved, rather than being silently swallowed.
 */
export function toDisplayError(error: unknown): ApiError | null {
  if (error instanceof ApiError) {
    return error;
  }
  if (error instanceof NetworkError) {
    return new ApiError(0, {
      errorCode: 'NETWORK.UNREACHABLE',
      message: 'Could not reach the server — this patient was NOT saved. Check your connection and try again.',
      correlationId: '',
      timestamp: new Date().toISOString(),
    });
  }
  return null;
}
