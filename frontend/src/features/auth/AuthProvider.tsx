import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type { PropsWithChildren } from 'react';
import { loginRequest } from './login-api';
import { readSession } from './session';
import type { Credentials, Session } from './types';

interface AuthContextValue {
  session: Session | null;
  login: (credentials: Credentials) => Promise<void>;
  logout: () => void;
  request: (path: string, init?: RequestInit) => Promise<Response>;
}
const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<Session | null>(null);
  const generation = useRef(0);
  const pendingLogin = useRef<AbortController | null>(null);
  const logout = useCallback(() => {
    generation.current++;
    pendingLogin.current?.abort();
    pendingLogin.current = null;
    setSession(null);
  }, []);

  const login = useCallback(async (credentials: Credentials) => {
    const attempt = ++generation.current;
    pendingLogin.current?.abort();
    const controller = new AbortController();
    pendingLogin.current = controller;
    try {
      const response = await loginRequest(credentials, controller.signal);
      const next = readSession(response);
      if (attempt !== generation.current || controller.signal.aborted) throw new DOMException('Solicitud cancelada', 'AbortError');
      setSession(next);
    } finally {
      if (attempt === generation.current) pendingLogin.current = null;
    }
  }, []);

  useEffect(() => {
    if (!session) return;
    const expire = () => { if (Date.now() >= session.expiresAt) logout(); };
    const timer = window.setTimeout(expire, Math.min(Math.max(session.expiresAt - Date.now(), 0), 2_147_483_647));
    window.addEventListener('focus', expire);
    document.addEventListener('visibilitychange', expire);
    return () => {
      clearTimeout(timer);
      window.removeEventListener('focus', expire);
      document.removeEventListener('visibilitychange', expire);
    };
  }, [session, logout]);

  useEffect(() => () => {
    generation.current++;
    pendingLogin.current?.abort();
  }, []);

  const request = useCallback(async (path: string, init?: RequestInit) => {
    if (!path.startsWith('/api/')) throw new Error('Las peticiones autenticadas deben dirigirse al Gateway en /api/.');
    if (!session || Date.now() >= session.expiresAt) {
      logout();
      throw new Error('Tu sesión ha finalizado. Inicia sesión de nuevo.');
    }
    const attempt = generation.current;
    const headers = new Headers(init?.headers);
    headers.set('Authorization', `Bearer ${session.accessToken}`);
    const response = await fetch(path, { ...init, headers, credentials: 'omit', cache: 'no-store' });
    if (response.status === 401 && generation.current === attempt) logout();
    return response;
  }, [session, logout]);

  const value = useMemo(() => ({ session, login, logout, request }), [session, login, logout, request]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth debe utilizarse dentro de AuthProvider.');
  return context;
}
