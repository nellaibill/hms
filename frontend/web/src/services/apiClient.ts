import {
  AdmissionsApi,
  AppointmentTypesApi,
  AuthApi,
  BedsApi,
  BrandingApi,
  ConsultantsApi,
  DepartmentsApi,
  EventsApi,
  HttpClient,
  IpdDashboardApi,
  MastersApi,
  PatientsApi,
  ProductsApi,
  RolesApi,
  ShiftAssignmentsApi,
  ShiftsApi,
  ShiftSwapRequestsApi,
  StaffAvailabilityApi,
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
export const staffAvailabilityApi = new StaffAvailabilityApi(httpClient);
export const weeklyRostersApi = new WeeklyRostersApi(httpClient);
export const shiftAssignmentsApi = new ShiftAssignmentsApi(httpClient);
export const shiftSwapRequestsApi = new ShiftSwapRequestsApi(httpClient);
export const eventsApi = new EventsApi(httpClient);
export const wardsApi = new WardsApi(httpClient);
export const bedsApi = new BedsApi(httpClient);
export const admissionsApi = new AdmissionsApi(httpClient);
export const ipdDashboardApi = new IpdDashboardApi(httpClient);
