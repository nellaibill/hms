import { HttpClient, UsersApi } from '@hms/shared';
import { env } from '../config/env';

export const httpClient = new HttpClient({
  baseUrl: env.apiBaseUrl,
});

export const usersApi = new UsersApi(httpClient);
