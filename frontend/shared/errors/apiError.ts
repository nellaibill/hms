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
 * Typed error thrown by the shared HTTP client for every non-2xx API response, so every
 * layer of the frontend handles one consistent error object (docs/FrontendArchitecture.md §12).
 */
export class ApiError extends Error {
  readonly errorCode: string;
  readonly status: number;
  readonly validationErrors?: ValidationErrorItem[];
  readonly correlationId: string;
  readonly timestamp: string;

  constructor(status: number, body: ApiErrorResponse) {
    super(body.message);
    this.name = 'ApiError';
    this.status = status;
    this.errorCode = body.errorCode;
    this.validationErrors = body.validationErrors;
    this.correlationId = body.correlationId;
    this.timestamp = body.timestamp;
  }
}

/** Thrown when the network request itself fails (offline, timeout) — distinct from ApiError. */
export class NetworkError extends Error {
  constructor(message = 'Network request failed.') {
    super(message);
    this.name = 'NetworkError';
  }
}
