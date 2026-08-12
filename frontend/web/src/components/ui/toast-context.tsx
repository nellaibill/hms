import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';

export type ToastVariant = 'default' | 'success' | 'error' | 'warning';

export interface ToastAction {
  label: string;
  onClick: () => void;
}

export interface ToastItem {
  id: string;
  title: string;
  description?: string;
  variant: ToastVariant;
  durationMs: number;
  /** An inline recovery action (e.g. "Retry") rendered alongside the dismiss button — for a
   * failure the user can act on without navigating away from the toast. */
  action?: ToastAction;
}

export interface ToastOptions {
  title: string;
  description?: string;
  variant?: ToastVariant;
  /** Per docs/DesignSystem.md §16, informational toasts auto-dismiss no sooner than 8s. */
  durationMs?: number;
  action?: ToastAction;
}

interface ToastContextValue {
  toasts: ToastItem[];
  toast: (options: ToastOptions) => void;
  dismiss: (id: string) => void;
  pause: (id: string) => void;
  resume: (id: string) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

let seq = 0;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const timers = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const dismiss = useCallback((id: string) => {
    const timer = timers.current.get(id);
    if (timer) clearTimeout(timer);
    timers.current.delete(id);
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const schedule = useCallback(
    (id: string, durationMs: number) => {
      const timer = setTimeout(() => dismiss(id), durationMs);
      timers.current.set(id, timer);
    },
    [dismiss],
  );

  const toast = useCallback(
    (options: ToastOptions) => {
      const id = `toast-${++seq}`;
      const durationMs = options.durationMs ?? 8000;
      const item: ToastItem = {
        id,
        title: options.title,
        description: options.description,
        variant: options.variant ?? 'default',
        durationMs,
        action: options.action,
      };
      // Cap the visible stack at 3 (docs/DesignSystem.md §16) — oldest drops off first.
      setToasts((prev) => [...prev, item].slice(-3));
      schedule(id, durationMs);
    },
    [schedule],
  );

  const pause = useCallback((id: string) => {
    const timer = timers.current.get(id);
    if (timer) clearTimeout(timer);
  }, []);

  const resume = useCallback(
    (id: string) => {
      const item = toasts.find((t) => t.id === id);
      if (item) schedule(id, item.durationMs);
    },
    [toasts, schedule],
  );

  const value = useMemo(() => ({ toasts, toast, dismiss, pause, resume }), [toasts, toast, dismiss, pause, resume]);

  return <ToastContext.Provider value={value}>{children}</ToastContext.Provider>;
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within a ToastProvider');
  return ctx;
}
