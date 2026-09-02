import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useRef, useState } from 'react';
import { documentsApi } from '../../../services/apiClient';
import { DOCUMENT_TYPE_TO_API } from '../constants';
import { mapDocumentResponseToHmsDocument } from '../mapDocument';
import { validateUploadForm, isUploadFormValid } from '../validation';
import type { DocumentUploadFormValues, EntityType, HmsDocument } from '../types';

function invalidateDocuments(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: ['documents'] });
}

export type UploadStatus = 'idle' | 'uploading' | 'completed' | 'error';

/**
 * Wraps the real POST /api/v1/documents call. `fetch` (the shared HttpClient's transport) has
 * no cross-browser upload-progress event, so the percentage shown while `status === 'uploading'`
 * is a synthetic animation rather than a true byte count — the completion/error outcome itself
 * is always the real API response.
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
      const errors = validateUploadForm(values);
      if (!isUploadFormValid(errors)) {
        setStatus('error');
        setError(Object.values(errors)[0] ?? 'Invalid upload.');
        return;
      }

      setStatus('uploading');
      setProgress(0);
      setError(null);

      // Animate progress up to 90% while the real request is in flight, then jump to 100% on
      // success — avoids a bar that either stalls at 0 for the whole request or falsely implies
      // byte-level progress that fetch() can't report.
      timerRef.current = setInterval(() => {
        setProgress((current) => Math.min(current + Math.random() * 12 + 4, 90));
      }, 220);

      documentsApi
        .uploadDocument(values.file as File, {
          ownerType: values.entityType as EntityType,
          ownerId: values.entityId,
          documentType: DOCUMENT_TYPE_TO_API[values.documentType as Exclude<DocumentUploadFormValues['documentType'], ''>],
        })
        .then((response) => {
          if (timerRef.current) clearInterval(timerRef.current);
          setProgress(100);
          setStatus('completed');
          invalidateDocuments(queryClient);
          onSuccess(mapDocumentResponseToHmsDocument(response));
        })
        .catch((err: unknown) => {
          if (timerRef.current) clearInterval(timerRef.current);
          setStatus('error');
          setError(err instanceof Error ? err.message : 'Upload failed. Please try again.');
        });
    },
    [queryClient],
  );

  return { status, progress, error, upload, reset };
}

export function useArchiveDocumentMutation() {
  const queryClient = useQueryClient();
  const [isPending, setIsPending] = useState(false);

  const archive = useCallback(
    (id: string, onSuccess: () => void, onError?: (message: string) => void) => {
      setIsPending(true);
      documentsApi
        .archiveDocument(id)
        .then(() => {
          invalidateDocuments(queryClient);
          onSuccess();
        })
        .catch((err: unknown) => onError?.(err instanceof Error ? err.message : 'Failed to archive document.'))
        .finally(() => setIsPending(false));
    },
    [queryClient],
  );

  return { archive, isPending };
}

export function useDeleteDocumentMutation() {
  const queryClient = useQueryClient();
  const [isPending, setIsPending] = useState(false);

  const remove = useCallback(
    (id: string, onSuccess: () => void, onError?: (message: string) => void) => {
      setIsPending(true);
      documentsApi
        .deleteDocument(id)
        .then(() => {
          invalidateDocuments(queryClient);
          onSuccess();
        })
        .catch((err: unknown) => onError?.(err instanceof Error ? err.message : 'Failed to delete document.'))
        .finally(() => setIsPending(false));
    },
    [queryClient],
  );

  return { remove, isPending };
}
