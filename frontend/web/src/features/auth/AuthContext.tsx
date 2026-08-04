import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import type { LoginResponse } from '@hms/shared';
import { authApi, setAuthToken } from '../../services/apiClient';
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
  login: (role: Role, username: string, password: string) => Promise<void>;
  logout: () => void;
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
  };
}

// Initializes the module-level HTTP token holder synchronously (outside React state) so
// the very first render's queries carry a restored token, not just renders after an effect.
const initialSession = readStoredSession();
setAuthToken(initialSession?.token ?? null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(initialSession?.user ?? null);

  const login = async (role: Role, username: string, password: string) => {
    const response = await authApi.login({ loginType: role, username, password });
    const session: StoredSession = {
      token: response.token,
      expiresAt: Date.now() + response.expiresIn * 1000,
      user: toAuthUser(response),
    };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    setAuthToken(session.token);
    setUser(session.user);
  };

  const logout = () => {
    sessionStorage.removeItem(STORAGE_KEY);
    setAuthToken(null);
    setUser(null);
  };

  const value = useMemo(() => ({ user, isAuthenticated: user !== null, login, logout }), [user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
