export type FileKind = 'pdf' | 'image' | 'docx' | 'xlsx' | 'other';

export function getFileKind(mimeType: string): FileKind {
  if (mimeType === 'application/pdf') return 'pdf';
  if (mimeType.startsWith('image/')) return 'image';
  if (mimeType.includes('wordprocessingml')) return 'docx';
  if (mimeType.includes('spreadsheetml')) return 'xlsx';
  return 'other';
}
