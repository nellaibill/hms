/**
 * Client-side file validation for upload inputs — fast feedback before a file ever leaves the
 * browser. This is a convenience layer, not the security boundary: the backend
 * (HMS.Modules.Documents.Application.Validation.FileSignatureValidator) re-checks the same
 * magic bytes server-side and is what actually protects the system against a spoofed
 * extension or a bypassed client, since a browser check is trivially skippable by anyone
 * calling the API directly. Mirrors that same signature table so the two can't drift apart in
 * what they consider valid.
 */

export type SniffedFileKind = 'jpeg' | 'png' | 'pdf';

const SIGNATURES: { kind: SniffedFileKind; bytes: number[] }[] = [
  { kind: 'jpeg', bytes: [0xff, 0xd8, 0xff] },
  { kind: 'png', bytes: [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a] },
  { kind: 'pdf', bytes: [0x25, 0x50, 0x44, 0x46] }, // "%PDF"
];

/** Reads the file's leading bytes and matches them against the known signature table — never
 * trusts `file.type` (a browser-supplied, spoofable label), only the actual content. */
async function sniffFileKind(file: File): Promise<SniffedFileKind | null> {
  const header = new Uint8Array(await file.slice(0, 8).arrayBuffer());
  const match = SIGNATURES.find((sig) => sig.bytes.every((byte, index) => header[index] === byte));
  return match?.kind ?? null;
}

const KIND_LABELS: Record<SniffedFileKind, string> = { jpeg: 'JPG', png: 'PNG', pdf: 'PDF' };

/** Validates a selected file against an allowed set of real (signature-sniffed) kinds and a
 * max size — returns an error message to show inline, or null if the file passes. */
export async function validateUploadFile(file: File, allowedKinds: SniffedFileKind[], maxSizeBytes: number): Promise<string | null> {
  const maxSizeMb = maxSizeBytes / (1024 * 1024);
  if (file.size > maxSizeBytes) {
    return `${file.name} is too large — the maximum is ${maxSizeMb}MB.`;
  }
  if (file.size === 0) {
    return `${file.name} is empty.`;
  }

  const kind = await sniffFileKind(file);
  if (!kind || !allowedKinds.includes(kind)) {
    const allowedLabels = allowedKinds.map((k) => KIND_LABELS[k]).join('/');
    return `${file.name} doesn't look like a valid ${allowedLabels} file.`;
  }

  return null;
}
