/** Triggers a browser download of an in-memory Blob — kept as its own generic helper here
 * rather than reusing features/documents' downloadDocument, which takes an HmsDocument and
 * fetches its content itself; this feature already has the Blob in hand. */
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
