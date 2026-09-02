/** Triggers a browser download of an in-memory Blob — mirrors features/employeeDocuments'
 * copy of the same small helper (kept local rather than shared; it's a five-line utility). */
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
