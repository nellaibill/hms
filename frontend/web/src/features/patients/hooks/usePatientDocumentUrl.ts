import type { DocumentType } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { documentsApi } from '../../../services/apiClient';

/** Query key prefix shared with the upload mutations (usePatientMutations.ts) — invalidating
 * ['patient-documents', patientId] there refreshes every document type for that patient,
 * since this hook's own key extends that prefix with the specific documentType. */
export function patientDocumentsQueryKey(patientId: string, documentType?: DocumentType) {
  return documentType ? (['patient-documents', patientId, documentType] as const) : (['patient-documents', patientId] as const);
}

/**
 * The most recent, scan-cleared document of one type for a patient (e.g. their photo or ID
 * proof), fetched as a displayable/openable blob URL — the Documents module's content
 * endpoint requires an Authorization header (see DocumentsController.GetContent), so a plain
 * `<img src>`/`<a href>` pointed straight at the API can't be used; the bytes have to be
 * fetched through the authenticated client first. Returns null while loading, once there's
 * nothing to show, or if the only upload(s) on file are still being virus-scanned (Pending) —
 * callers don't need to distinguish those cases, a photo that isn't ready yet just isn't shown.
 */
export function usePatientDocumentUrl(patientId: string, documentType: DocumentType): string | null {
  const { data: documents } = useQuery({
    queryKey: patientDocumentsQueryKey(patientId, documentType),
    queryFn: () => documentsApi.listDocuments({ ownerType: 'Patient', ownerId: patientId, documentType }),
    // A freshly uploaded document sits at Pending until the background scan clears it (see
    // DocumentScanBackgroundService) — usually near-instant, but not guaranteed to have
    // finished by the time the upload's own success handler invalidates this query. Poll
    // briefly while anything is still Pending so the photo/ID-proof appears on its own once
    // scanning catches up, instead of only after the next unrelated refetch.
    refetchInterval: (query) => (query.state.data?.some((doc) => doc.status === 'Pending') ? 1000 : false),
  });

  const latest = documents
    ?.filter((doc) => doc.status === 'Available' && !doc.isArchived)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];

  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!latest) {
      setUrl(null);
      return;
    }
    let cancelled = false;
    let objectUrl: string | null = null;
    documentsApi.getDocumentContent(latest.id).then((blob) => {
      if (cancelled) return;
      objectUrl = URL.createObjectURL(blob);
      setUrl(objectUrl);
    });
    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- re-fetch content only when which document is latest actually changes, not on every `documents` array identity change.
  }, [latest?.id]);

  return url;
}
