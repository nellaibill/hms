import { API_ROUTES } from '../../constants';
import type { CreateUserRequest, SetPasswordRequest, UpdateUserRequest, User, UserListQuery } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedUsers {
  items: User[];
  meta: PaginationMeta;
}

/**
 * Typed API service for the Users module, built on the shared HTTP client. Feature code
 * (web/mobile) calls this, never the HTTP client directly — docs/FrontendArchitecture.md §6.
 */
export class UsersApi {
  constructor(private readonly client: HttpClient) {}

  async getUsers(query: UserListQuery = {}): Promise<PagedUsers> {
    const response = await this.client.get<User[]>(API_ROUTES.users.base, {
      query: {
        page: query.page,
        pageSize: query.pageSize,
        sort: query.sort,
        search: query.search,
        isActive: query.isActive,
      },
    });
    return {
      items: response.data,
      meta: response.meta as PaginationMeta,
    };
  }

  async getUserById(id: string): Promise<User> {
    const response = await this.client.get<User>(API_ROUTES.users.byId(id));
    return response.data;
  }

  async createUser(request: CreateUserRequest): Promise<User> {
    const response = await this.client.post<User>(API_ROUTES.users.base, request);
    return response.data;
  }

  async updateUser(id: string, request: UpdateUserRequest): Promise<User> {
    const response = await this.client.put<User>(API_ROUTES.users.byId(id), request);
    return response.data;
  }

  async deleteUser(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.users.byId(id));
  }

  async activateUser(id: string): Promise<User> {
    const response = await this.client.post<User>(API_ROUTES.users.activate(id));
    return response.data;
  }

  async deactivateUser(id: string): Promise<User> {
    const response = await this.client.post<User>(API_ROUTES.users.deactivate(id));
    return response.data;
  }

  async setPassword(id: string, request: SetPasswordRequest): Promise<User> {
    const response = await this.client.post<User>(API_ROUTES.users.password(id), request);
    return response.data;
  }

  async uploadProfilePhoto(id: string, file: File): Promise<User> {
    const formData = new FormData();
    // Field name must be "photo" — mirrors UsersController.UploadProfilePhoto's IFormFile
    // parameter name, which ASP.NET Core model binding matches against.
    formData.append('photo', file);
    const response = await this.client.postFormData<User>(API_ROUTES.users.profilePhoto(id), formData);
    return response.data;
  }
}
