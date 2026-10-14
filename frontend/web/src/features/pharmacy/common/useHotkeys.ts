import { useEffect, useRef } from 'react';

/** Key names as KeyboardEvent.key reports them ('F2', 'Escape'), optionally prefixed with
 * 'ctrl+' or 'alt+' ('ctrl+s'). */
export type HotkeyMap = Record<string, (event: KeyboardEvent) => void>;

/**
 * Screen-wide keyboard shortcuts for the counter and the entry screens — the function keys a
 * billing clerk works from without reaching for the mouse. Handlers are read through a ref,
 * so a screen can pass a fresh object every render without re-binding the listener.
 */
export function useHotkeys(hotkeys: HotkeyMap, enabled = true): void {
  const latest = useRef(hotkeys);
  latest.current = hotkeys;

  useEffect(() => {
    if (!enabled) return;

    function onKeyDown(event: KeyboardEvent) {
      const parts: string[] = [];
      if (event.ctrlKey || event.metaKey) parts.push('ctrl');
      if (event.altKey) parts.push('alt');
      parts.push(event.key.length === 1 ? event.key.toLowerCase() : event.key);

      const handler = latest.current[parts.join('+')];
      if (handler) {
        // Function keys all have browser meanings (F5 reload, F6 address bar); on these
        // screens they belong to the pharmacy.
        event.preventDefault();
        handler(event);
      }
    }

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [enabled]);
}
