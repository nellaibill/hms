/** Mirrors HMS.Modules.Identity.Contracts.UserResponse. */
export interface User {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  profilePhotoUrl?: string | null;
  roleId: string;
  roleName: string;
  emailVerified: boolean;
  lastLoginAt?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Identity.Contracts.CreateUserRequest. */
export interface CreateUserRequest {
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roleId: string;
}

/** Mirrors HMS.Modules.Identity.Contracts.UpdateUserRequest. */
export interface UpdateUserRequest {
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roleId: string;
}

/** Mirrors HMS.Modules.Identity.Contracts.UserListQuery. */
export interface UserListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}

/** Mirrors HMS.Modules.Identity.Contracts.SetPasswordRequest. */
export interface SetPasswordRequest {
  password: string;
}

/** Mirrors HMS.Modules.Identity.Contracts.StaffDirectoryEntryResponse — a deliberately
 * minimal, low-sensitivity user view any authenticated staff member can fetch (unlike
 * `User` above, which needs an admin-level permission). Used for the messaging module's
 * "start a conversation" staff picker. */
export interface StaffDirectoryEntry {
  id: string;
  firstName: string;
  lastName: string;
  roleName: string;
}
