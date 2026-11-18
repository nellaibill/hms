// VITE_API_BASE_URL is baked into the bundle at build time. "same-origin" means the API is the
// same site that serves this app: the packaged Windows install, where HMS.Api serves this build
// itself. That way one build works at any hospital's address. (A word, not "/": Git Bash rewrites
// a lone "/" into its own install folder before Vite ever sees it.)
const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL;

export const env = {
  apiBaseUrl: configuredApiBaseUrl === 'same-origin' ? window.location.origin : configuredApiBaseUrl || 'http://localhost:5000',
};
