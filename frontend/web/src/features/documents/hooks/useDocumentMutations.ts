import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useRef, useState } from 'react';
import { archiveMockDocument, createMockDocument, deleteMockDocument, DocumentValidationError } from '../mockDocumentsStore';
import type { DocumentUploadFormValues, HmsDocument } from '../types';

function invalidateDocuments(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: ['documents'] });
}

export type UploadStatus = 'idle' | 'uploading' | 'completed' | 'error';

/**
 * Not a plain react-query mutation — the mock store's create call is synchronous, but the
 * spec calls for an animated progress bar (Uploading/Completed/Error), so progress is
 * simulated here with a timer and the resulting document is committed to the store once it
 * reaches 100%, then the shared 'documents' query cache is invalidated so the table refreshes.
 */
export function useUploadDocumentMutation() {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<UploadStatus>('idle');
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const reset = useCallback(() => {
    if (timerRef.current) clearInterval(timerRef.current);
    setStatus('idle');
    setProgress(0);
    setError(null);
  }, []);

  const upload = useCallback(
    (values: DocumentUploadFormValues, onSuccess: (doc: HmsDocument) => void) => {
      setStatus('uploading');
      setProgress(0);
      setError(null);

      // Side effects (creating the document, invalidating queries, the caller's onSuccess)
      // must not live inside the setProgress updater above — a setState updater is expected
      // to be a pure function of prev state, and nesting further setState calls in there
      // triggers "Cannot update a component while rendering a different component". Track
      // the running total in a plain local instead and only call setProgress with plain values.
      let current = 0;
      timerRef.current = setInterval(() => {
        current = Math.min(current + Math.random() * 22 + 10, 100);
        setProgress(current);

        if (current >= 100) {
          if (timerRef.current) clearInterval(timerRef.current);
          try {
            const created = createMockDocument(values);
            setStatus('completed');
            invalidateDocuments(queryClient);
            onSuccess(created);
          } catch (err) {
            setStatus('error');
            setError(err instanceof DocumentValidationError ? err.message : 'Upload failed. Please try again.');
          }
        }
      }, 220);
    },
    [queryClient],
  );

  return { status, progress, error, upload, reset };
}

export function useArchiveDocumentMutation() {
  const queryClient = useQueryClient();
  const [isPending, setIsPending] = useState(false);

  const archive = useCallback(
    (id: string, onSuccess: () => void) => {
      setIsPending(true);
      Promise.resolve(archiveMockDocument(id)).then(() => {
        setIsPending(false);
        invalidateDocuments(queryClient);
        onSuccess();
      });
    },
    [queryClient],
  );

  return { archive, isPending };
}

export function useDeleteDocumentMutation() {
  const queryClient = useQueryClient();
  const [isPending, setIsPending] = useState(false);

  const remove = useCallback(
    (id: string, onSuccess: () => void) => {
      setIsPending(true);
      Promise.resolve(deleteMockDocument(id)).then(() => {
        setIsPending(false);
        invalidateDocuments(queryClient);
        onSuccess();
      });
    },
    [queryClient],
  );

  return { remove, isPending };
}
