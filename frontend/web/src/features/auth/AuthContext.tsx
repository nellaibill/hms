import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { findMockUserByRole } from './mockUsers';
import type { MockUser, Role } from './types';

const STORAGE_KEY = 'hms-mock-session';

interface AuthContextValue {
  user: MockUser | null;
  isAuthenticated: boolean;
  /** Mock sign-in only — no credential verification, no backend call (per module scope). */
  login: (role: Role, username: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredUser(): MockUser | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as MockUser;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<MockUser | null>(readStoredUser);

  const login = (role: Role, username: string) => {
    const matched = findMockUserByRole(role);
    const nextUser: MockUser = matched
      ? { ...matched, username: username || matched.username }
      : { id: `mock-${role}`, name: username || 'Mock User', role, username: username || 'mock.user' };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(nextUser));
    setUser(nextUser);
  };

  const logout = () => {
    sessionStorage.removeItem(STORAGE_KEY);
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
