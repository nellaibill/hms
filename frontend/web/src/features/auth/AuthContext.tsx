import { createContext, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import type { LoginResponse } from '@hms/shared';
import { brandingQueryKey } from '../branding/hooks/useBrandingQuery';
import { authApi, setAuthToken, setUnauthorizedHandler } from '../../services/apiClient';
import { markSessionExpired } from '../../lib/sessionExpiry';
import { isConsultantRoleName } from '../../lib/isConsultantRoleName';
import type { AuthUser, Role } from './types';

const STORAGE_KEY = 'hms-session';

interface StoredSession {
  token: string;
  /** Epoch ms — derived from LoginResponse.expiresIn at login time. */
  expiresAt: number;
  user: AuthUser;
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (hospitalCode: string, role: Role, username: string, password: string) => Promise<void>;
  logout: () => void;
  /** UI-only hint mirroring the backend's [RequirePermission(key)] checks — hides/disables
   * actions the user's role doesn't have, so they don't fill in a form only to hit a 403. */
  hasPermission: (key: string) => boolean;
  /** UI-only hint mirroring the backend's [RequireFeature(key)] checks — hides/disables
   * nav/actions for a module the tenant doesn't have. See AuthUser.featureKeys's own doc
   * comment for why the backend never trusts this snapshot for enforcement. */
  hasFeature: (key: string) => boolean;
  /** Called after a successful change-password submission — clears the forced-change flag
   * on the in-memory and stored session without a full re-login. */
  clearMustChangePassword: () => void;
  /** Non-null only when this user's Role is Consultant/Doctor AND they're linked to a
   * consultant record — mirrors the backend's ClaimsPrincipalExtensions.GetScopedConsultantId
   * exactly, so a Super Admin/Admin who happens to also be linked isn't restricted. Clinical
   * list screens (OPD, Admissions, etc.) use this to lock their Department/Consultant filters
   * to this user's own consultant. UI convenience only — the backend enforces the real
   * restriction regardless of what this value is. */
  scopedConsultantId: string | null;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredSession(): StoredSession | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as StoredSession;
    if (!parsed.token || parsed.expiresAt <= Date.now()) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

function toAuthUser(response: LoginResponse): AuthUser {
  const { user } = response;
  return {
    id: user.id,
    name: `${user.firstName} ${user.lastName}`.trim(),
    role: user.loginType as Role,
    username: user.username,
    email: user.email,
    roleName: user.roleName,
    permissionKeys: user.permissionKeys,
    featureKeys: user.featureKeys,
    mustChangePassword: user.mustChangePassword,
    consultantId: user.consultantId,
  };
}

// Initializes the module-level HTTP token holder synchronously (outside React state) so
// the very first render's queries carry a restored token, not just renders after an effect.
const initialSession = readStoredSession();
setAuthToken(initialSession?.token ?? null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(initialSession?.user ?? null);
  const queryClient = useQueryClient();

  const login = async (hospitalCode: string, role: Role, username: string, password: string) => {
    const response = await authApi.login(hospitalCode, { loginType: role, username, password });
    const session: StoredSession = {
      token: response.token,
      expiresAt: Date.now() + response.expiresIn * 1000,
      user: toAuthUser(response),
    };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    setAuthToken(session.token);
    setUser(session.user);
    // Branding is now tenant-scoped server-side (see ADR referenced in BrandingModule.cs) —
    // any cached response from a *different* hospital's session in this same browser tab
    // (or the anonymous pre-login fallback) must be thrown away so ThemeProvider re-fetches
    // this hospital's own branding, not whatever was cached under the shared queryKey.
    void queryClient.invalidateQueries({ queryKey: brandingQueryKey });
  };

  const logout = () => {
    sessionStorage.removeItem(STORAGE_KEY);
    setAuthToken(null);
    setUser(null);
    void queryClient.invalidateQueries({ queryKey: brandingQueryKey });
  };

  // Lets the shared HttpClient force a logout the moment any request comes back 401 (an
  // expired or otherwise invalid token) instead of leaving every subsequent request on the
  // page fail silently. Refs (rather than listing `user`/`logout` as effect deps) avoid
  // re-registering the handler on every render while still always calling the latest
  // versions. Guarded on `userRef.current` — GET /branding is deliberately called with no
  // token at all on the pre-login screen and always 401s there by design (see
  // useBrandingQuery's own doc comment), which must never be mistaken for a real session
  // expiring.
  const userRef = useRef(user);
  userRef.current = user;
  const logoutRef = useRef(logout);
  logoutRef.current = logout;
  useEffect(() => {
    setUnauthorizedHandler(() => {
      if (!userRef.current) return;
      markSessionExpired('hospital');
      logoutRef.current();
    });
    return () => setUnauthorizedHandler(null);
  }, []);

  const hasPermission = (key: string) => user?.permissionKeys.includes(key) ?? false;
  const hasFeature = (key: string) => user?.featureKeys.includes(key) ?? false;

  const clearMustChangePassword = () => {
    setUser((current) => {
      if (!current) return current;
      const updated = { ...current, mustChangePassword: false };
      const stored = readStoredSession();
      if (stored) {
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ ...stored, user: updated }));
      }
      return updated;
    });
  };

  const scopedConsultantId =
    user?.consultantId && isConsultantRoleName(user.roleName) ? user.consultantId : null;

  const value = useMemo(
    () => ({ user, isAuthenticated: user !== null, login, logout, hasPermission, hasFeature, clearMustChangePassword, scopedConsultantId }),
    [user, scopedConsultantId],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
