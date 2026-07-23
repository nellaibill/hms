// Expo requires the EXPO_PUBLIC_ prefix for env vars to be inlined at build time
// (docs/FrontendArchitecture.md §10).
export const env = {
  apiBaseUrl: process.env.EXPO_PUBLIC_API_BASE_URL || 'http://localhost:5000',
};
