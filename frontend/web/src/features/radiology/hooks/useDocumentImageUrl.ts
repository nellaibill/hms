import { useEffect, useState } from 'react';
import { documentsApi } from '../../../services/apiClient';

/** Loads a stored image through the authenticated content endpoint (never a public URL) and
 * exposes it as an object URL, revoked when the component unmounts or the document changes. */
export function useDocumentImageUrl(documentId: string): { url: string | null; failed: boolean } {
  const [state, setState] = useState<{ url: string | null; failed: boolean }>({ url: null, failed: false });

  useEffect(() => {
    let objectUrl: string | null = null;
    let cancelled = false;
    setState({ url: null, failed: false });

    documentsApi
      .getDocumentContent(documentId)
      .then((blob) => {
        if (cancelled) return;
        objectUrl = URL.createObjectURL(blob);
        setState({ url: objectUrl, failed: false });
      })
      .catch(() => {
        if (!cancelled) setState({ url: null, failed: true });
      });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [documentId]);

  return state;
}
