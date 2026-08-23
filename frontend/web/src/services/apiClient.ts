import {
  AdmissionsApi,
  AppointmentTypesApi,
  AuthApi,
  BedsApi,
  BillingApi,
  BrandingApi,
  ConsultantsApi,
  DepartmentsApi,
  EventsApi,
  HttpClient,
  IpdDashboardApi,
  MastersApi,
  PatientsApi,
  PharmacyApi,
  PlatformAuthApi,
  PlatformHospitalsApi,
  ProductsApi,
  RolesApi,
  ShiftAssignmentsApi,
  ShiftsApi,
  ShiftSwapRequestsApi,
  StaffAvailabilityApi,
  StatesApi,
  UsersApi,
  WardsApi,
  WeeklyRostersApi,
} from '@hms/shared';
import { env } from '../config/env';

// Module-level token holder — the HttpClient instance below is created once at import
// time, before AuthProvider mounts, so it reads the current token through this indirection
// rather than a value captured at construction. AuthContext is the only writer.
let authToken: string | null = null;

export function setAuthToken(token: string | null) {
  authToken = token;
}

export const httpClient = new HttpClient({
  baseUrl: env.apiBaseUrl,
  getAuthToken: () => authToken,
});

// Independent token holder + HttpClient instance for the Platform Portal — a Platform Admin
// session must never share a bearer token with a hospital session (PlatformAuthContext is
// the only writer), even though both hit the same API origin.
let platformAuthToken: string | null = null;

export function setPlatformAuthToken(token: string | null) {
  platformAuthToken = token;
}

export const platformHttpClient = new HttpClient({
  baseUrl: env.apiBaseUrl,
  getAuthToken: () => platformAuthToken,
});

export const platformAuthApi = new PlatformAuthApi(platformHttpClient);
export const platformHospitalsApi = new PlatformHospitalsApi(platformHttpClient);

export const authApi = new AuthApi(httpClient);
export const usersApi = new UsersApi(httpClient);
export const rolesApi = new RolesApi(httpClient);
export const patientsApi = new PatientsApi(httpClient);
export const brandingApi = new BrandingApi(httpClient);
export const mastersApi = new MastersApi(httpClient);
export const productsApi = new ProductsApi(httpClient);
export const shiftsApi = new ShiftsApi(httpClient);
export const departmentsApi = new DepartmentsApi(httpClient);
export const consultantsApi = new ConsultantsApi(httpClient);
export const appointmentTypesApi = new AppointmentTypesApi(httpClient);
export const statesApi = new StatesApi(httpClient);
export const staffAvailabilityApi = new StaffAvailabilityApi(httpClient);
export const weeklyRostersApi = new WeeklyRostersApi(httpClient);
export const shiftAssignmentsApi = new ShiftAssignmentsApi(httpClient);
export const shiftSwapRequestsApi = new ShiftSwapRequestsApi(httpClient);
export const eventsApi = new EventsApi(httpClient);
export const wardsApi = new WardsApi(httpClient);
export const bedsApi = new BedsApi(httpClient);
export const admissionsApi = new AdmissionsApi(httpClient);
export const ipdDashboardApi = new IpdDashboardApi(httpClient);
export const billingApi = new BillingApi(httpClient);
export const pharmacyApi = new PharmacyApi(httpClient);
