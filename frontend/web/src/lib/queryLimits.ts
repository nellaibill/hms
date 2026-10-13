/**
 * The API rejects any query-string value longer than this with a 400 (backend
 * QueryStringGuardFilter.DefaultMaxValueLength). Search boxes and filter fields stop typing
 * here so a request can't fail on length.
 */
export const MAX_QUERY_VALUE_LENGTH = 100;
