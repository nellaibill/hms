import { hasLiveContent } from '../mockDocumentsStore';
import type { HmsDocument } from '../types';

/** Returns false (and does nothing) when the document has no real backing content — seed/demo
 * rows and any real upload from a previous, now-reloaded session. Callers should surface a
 * toast in that case. */
export function downloadDocument(doc: HmsDocument): boolean {
  if (!hasLiveContent(doc.filePath)) {
    return false;
  }
  const anchor = document.createElement('a');
  anchor.href = doc.filePath;
  anchor.download = doc.originalFileName;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  return true;
}
