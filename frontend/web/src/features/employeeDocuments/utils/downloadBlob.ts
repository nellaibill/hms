/** Triggers a browser download of an in-memory Blob — the mock features/documents module's
 * downloadDocument helper only works against its mockDocumentsStore-backed fake file paths,
 * so this feature (real /api/v1/documents content) needs its own, generic version. */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}
