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

/** Discriminates the two shapes `login`/`completeMfaLogin` can settle to — see
 * PlatformLoginResponse's own doc comment for why login is a two-step process for an
 * MFA-enabled account. */
export type PlatformLoginOutcome =
  | { mfaRequired: true; challengeToken: string }
  | { mfaRequired: false };

interface PlatformAuthContextValue {
  user: PlatformLoginUserResponse | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<PlatformLoginOutcome>;
  /** Second step of an MFA login — exchanges the challenge token `login` returned for a
   * real session once the authenticator code checks out. */
  completeMfaLogin: (challengeToken: string, code: string) => Promise<void>;
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

// Initializes the module-level HTTP token holder synchronously (outside React state) so
// the very first render's queries carry a restored token, not just renders after an effect.
const initialSession = readStoredSession();
setPlatformAuthToken(initialSession?.token ?? null);

export function PlatformAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<PlatformLoginUserResponse | null>(initialSession?.user ?? null);

  function establishSession(response: PlatformLoginResponse) {
    // Only ever called with the completed-login shape (mfaRequired false) — see
    // PlatformLoginResponse's own doc comment.
    const session: StoredSession = {
      token: response.token!,
      expiresAt: Date.now() + response.expiresIn * 1000,
      user: response.user!,
    };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    setPlatformAuthToken(session.token);
    setUser(session.user);
  }

  const login = async (email: string, password: string): Promise<PlatformLoginOutcome> => {
    const response = await platformAuthApi.login({ email, password });
    if (response.mfaRequired) {
      return { mfaRequired: true, challengeToken: response.mfaChallengeToken! };
    }
    establishSession(response);
    return { mfaRequired: false };
  };

  const completeMfaLogin = async (challengeToken: string, code: string) => {
    const response = await platformAuthApi.verifyMfa({ challengeToken, code });
    establishSession(response);
  };

  const logout = () => {
    // Best-effort, fire-and-forget: revokes the token server-side (see
    // JwtConfiguration/PlatformAuthController's OnTokenValidated/Logout) so a copy of it
    // elsewhere can't keep working after this logout. Called before clearing local state so
    // the request still carries the (still-current) Authorization header; a network failure
    // here must never block the user from logging out locally.
    void platformAuthApi.logout().catch(() => {});
    sessionStorage.removeItem(STORAGE_KEY);
    setPlatformAuthToken(null);
    setUser(null);
  };

  const value = useMemo(
    () => ({ user, isAuthenticated: user !== null, login, completeMfaLogin, logout }),
    [user],
  );

  return <PlatformAuthContext.Provider value={value}>{children}</PlatformAuthContext.Provider>;
}

export function usePlatformAuth() {
  const ctx = useContext(PlatformAuthContext);
  if (!ctx) throw new Error('usePlatformAuth must be used within a PlatformAuthProvider');
  return ctx;
}
