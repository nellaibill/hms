import {
  AuthApi,
  BrandingApi,
  MastersApi,
  OfflineHttpClient,
  PatientsApi,
  ProductsApi,
  RolesApi,
  ShiftAssignmentsApi,
  ShiftsApi,
  ShiftSwapRequestsApi,
  StaffAvailabilityApi,
  UsersApi,
  WeeklyRostersApi,
} from '@hms/shared';
// Module-level token holder — the HttpClient instance below is created once at import
// time, before AuthProvider mounts, so it reads the current token through this indirection
// rather than a value captured at construction. AuthContext is the only writer.
let authToken: string | null = null;

export function setAuthToken(token: string | null) {
  authToken = token;
}

// Demo branch (feature/demo-ui-with-mock-data): every *Api call below must always use
// in-app mock data, regardless of whether a real backend happens to be reachable — not
// just as a fallback when it isn't. OfflineHttpClient never calls fetch; it immediately
// rejects every request with NetworkError, so every module's existing NetworkError-catch
// mock fallback (mockRolesStore.ts, masterStoreFactory.ts, mockProductsStore.ts, etc.) is
// always taken. See docs note in offlineHttpClient.ts. getAuthToken is wired through
// unchanged (even though this client never actually calls fetch) so AuthContext's existing
// setAuthToken flow keeps working exactly as it does for a real backend.
export const httpClient = new OfflineHttpClient({ getAuthToken: () => authToken });

export const authApi = new AuthApi(httpClient);
export const usersApi = new UsersApi(httpClient);
export const rolesApi = new RolesApi(httpClient);
export const patientsApi = new PatientsApi(httpClient);
export const brandingApi = new BrandingApi(httpClient);
export const mastersApi = new MastersApi(httpClient);
export const productsApi = new ProductsApi(httpClient);
export const shiftsApi = new ShiftsApi(httpClient);
export const staffAvailabilityApi = new StaffAvailabilityApi(httpClient);
export const weeklyRostersApi = new WeeklyRostersApi(httpClient);
export const shiftAssignmentsApi = new ShiftAssignmentsApi(httpClient);
export const shiftSwapRequestsApi = new ShiftSwapRequestsApi(httpClient);
