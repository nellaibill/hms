/** Mirrors HMS.Modules.Identity.Contracts.UserResponse. */
export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Identity.Contracts.CreateUserRequest. */
export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
}

/** Mirrors HMS.Modules.Identity.Contracts.UpdateUserRequest. */
export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
}

/** Mirrors HMS.Modules.Identity.Contracts.UserListQuery. */
export interface UserListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
