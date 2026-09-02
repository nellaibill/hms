import { documentsApi } from '../../../services/apiClient';
import type { HmsDocument } from '../types';

/** Streams the document's bytes via the authenticated GET .../content endpoint — the backend
 * deliberately never serves document content through a plain URL (see DocumentsController's
 * own doc comment), so this can't be a plain anchor href the way the old mock demo data was.
 * Returns false (and does nothing) when the content isn't available yet (still being scanned,
 * quarantined, or the caller can no longer see it) — callers should surface a toast. */
export async function downloadDocument(doc: HmsDocument): Promise<boolean> {
  try {
    const blob = await documentsApi.getDocumentContent(doc.id);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = doc.originalFileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
    return true;
  } catch {
    return false;
  }
}
