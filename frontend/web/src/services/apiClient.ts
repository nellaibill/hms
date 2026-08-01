import { BrandingApi, HttpClient, MastersApi, PatientsApi, ProductsApi, RolesApi, UsersApi } from '@hms/shared';
import { env } from '../config/env';

export const httpClient = new HttpClient({
  baseUrl: env.apiBaseUrl,
});

export const usersApi = new UsersApi(httpClient);
export const rolesApi = new RolesApi(httpClient);
export const patientsApi = new PatientsApi(httpClient);
export const brandingApi = new BrandingApi(httpClient);
export const mastersApi = new MastersApi(httpClient);
export const productsApi = new ProductsApi(httpClient);
