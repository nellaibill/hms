import { useEffect, useRef } from 'react';
import { useBlocker } from 'react-router-dom';

/**
 * Blocks navigation (in-app and tab close/refresh) while a form has unsaved changes — lifted
 * from InvoiceCreatePage.tsx's original ad-hoc implementation into a reusable hook so every
 * form that needs this doesn't reimplement the same isDirtyRef/useBlocker/beforeunload
 * plumbing. Covers the two ways a user can leave a page: in-app navigation (react-router's
 * own blocker, only reachable via the RouterProvider/data-router setup this app uses) and
 * actual tab close/refresh/URL-bar navigation (react-router's blocker can't see these, only
 * the browser's native beforeunload prompt can).
 *
 * `isDirty` is read via a ref, not directly, so the blocker's `shouldBlock` callback (called
 * outside React's render cycle) always sees the latest value synchronously rather than a
 * stale closure from whichever render last created it.
 *
 * Returns `markSaved()` for the one case the `isDirty` prop alone can't handle correctly: a
 * caller that calls `navigate()` itself right after a successful save (rather than letting
 * this hook's own confirm dialog do it). React Hook Form's `formState.isDirty` doesn't reset
 * until a `reset()` call takes effect on a later render, so a `navigate()` fired immediately
 * after a successful submit can still see the pre-save `isDirty=true` and get wrongly
 * blocked. `markSaved()` writes straight to the ref, taking effect for the very next
 * `shouldBlock` check with no render/effect round-trip to wait for — call it synchronously,
 * right before `navigate()`, once a save has actually succeeded.
 */
export function useUnsavedChangesGuard(isDirty: boolean) {
  const isDirtyRef = useRef(isDirty);
  useEffect(() => {
    isDirtyRef.current = isDirty;
  }, [isDirty]);

  const blocker = useBlocker(() => isDirtyRef.current);

  function markSaved() {
    isDirtyRef.current = false;
  }

  useEffect(() => {
    if (!isDirty) return;
    function handleBeforeUnload(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = '';
    }
    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [isDirty]);

  const showUnsavedDialog = blocker.state === 'blocked';

  function confirmDiscard() {
    if (blocker.state === 'blocked') {
      blocker.proceed();
    }
  }

  function cancelDiscard() {
    if (blocker.state === 'blocked') {
      blocker.reset();
    }
  }

  return { showUnsavedDialog, confirmDiscard, cancelDiscard, markSaved };
}
