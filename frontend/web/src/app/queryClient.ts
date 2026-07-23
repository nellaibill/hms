import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      // 'always' (rather than the default 'online') guarantees a query always settles
      // into a catchable error instead of sitting in a "paused" fetchStatus — a network
      // failure must surface as a distinct, retryable state per docs/ApiStandards.md §8,
      // never an indefinite silent loading spinner.
      networkMode: 'always',
    },
    mutations: {
      // Mutations are never silently auto-retried — a failed write surfaces an
      // explicit retry action instead, per docs/ApiStandards.md's idempotency guidance.
      retry: false,
      networkMode: 'always',
    },
  },
});
