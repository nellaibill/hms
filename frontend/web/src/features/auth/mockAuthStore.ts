import { ApiError, type LoginRequest, type LoginResponse } from '@hms/shared';
import { MOCK_USERS } from '../users/mockUsers';

/**
 * Offline fallback for sign-in — used only when the real API is unreachable (see the
 * NetworkError catch in AuthContext.login). Unlike every other module's mock store, Auth has
 * no prior real session to fall back FROM mid-flow, so this is deliberately simple: any
 * non-empty password is accepted for one of the 10 seeded demo accounts in
 * features/users/mockUsers.ts (the same records the Users module and every StaffId picker
 * use), as long as the selected "Sign in as" role matches that account's `loginType` — the
 * same rule HMS.Modules.Identity.Application.LoginTypes.RoleMatches enforces server-side.
 */
export function mockLogin(request: LoginRequest): LoginResponse {
  const account = MOCK_USERS.find((u) => u.username === request.username);

  if (!account || !request.password.trim() || account.loginType !== request.loginType) {
    throw new ApiError(401, {
      errorCode: 'AUTH.INVALID_LOGIN',
      message: 'Invalid username or password.',
      correlationId: 'demo-mode',
      timestamp: new Date().toISOString(),
    });
  }

  return {
    token: `demo-token-${account.id}`,
    expiresIn: 60 * 60 * 8,
    user: {
      id: account.id,
      username: account.username,
      firstName: account.firstName,
      lastName: account.lastName,
      email: account.email,
      roleId: account.roleId,
      roleName: account.roleName,
      loginType: account.loginType,
      profilePhotoUrl: null,
    },
  };
}
