import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import type { PlatformLoginResponse, PlatformLoginUserResponse } from '@hms/shared';
import { platformAuthApi, setPlatformAuthToken } from '../../services/apiClient';

const STORAGE_KEY = 'hms-platform-session';

interface StoredSession {
  token: string;
  /** Epoch ms — derived from PlatformLoginResponse.expiresIn at login time. */
  expiresAt: number;
  user: PlatformLoginUserResponse;
}

interface PlatformAuthContextValue {
  user: PlatformLoginUserResponse | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const PlatformAuthContext = createContext<PlatformAuthContextValue | undefined>(undefined);

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

function toStoredUser(response: PlatformLoginResponse): PlatformLoginUserResponse {
  return response.user;
}

// Initializes the module-level HTTP token holder synchronously (outside React state) so
// the very first render's queries carry a restored token, not just renders after an effect.
const initialSession = readStoredSession();
setPlatformAuthToken(initialSession?.token ?? null);

export function PlatformAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<PlatformLoginUserResponse | null>(initialSession?.user ?? null);

  const login = async (email: string, password: string) => {
    const response = await platformAuthApi.login({ email, password });
    const session: StoredSession = {
      token: response.token,
      expiresAt: Date.now() + response.expiresIn * 1000,
      user: toStoredUser(response),
    };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    setPlatformAuthToken(session.token);
    setUser(session.user);
  };

  const logout = () => {
    sessionStorage.removeItem(STORAGE_KEY);
    setPlatformAuthToken(null);
    setUser(null);
  };

  const value = useMemo(() => ({ user, isAuthenticated: user !== null, login, logout }), [user]);

  return <PlatformAuthContext.Provider value={value}>{children}</PlatformAuthContext.Provider>;
}

export function usePlatformAuth() {
  const ctx = useContext(PlatformAuthContext);
  if (!ctx) throw new Error('usePlatformAuth must be used within a PlatformAuthProvider');
  return ctx;
}
