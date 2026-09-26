import { documentsApi } from '../../services/apiClient';

/** Opens a stored document in a new tab, or downloads it. The content endpoint needs the
 * Authorization header (DocumentsController.GetContent), so the bytes are fetched through the
 * authenticated client and handed over as a blob URL rather than linking to the API directly. */
export async function openDocument(document: { id: string; originalFileName: string }, mode: 'view' | 'download') {
  const blob = await documentsApi.getDocumentContent(document.id);
  const url = URL.createObjectURL(blob);
  if (mode === 'view') {
    window.open(url, '_blank', 'noopener,noreferrer');
    return;
  }
  const link = window.document.createElement('a');
  link.href = url;
  link.download = document.originalFileName;
  link.click();
  URL.revokeObjectURL(url);
}
