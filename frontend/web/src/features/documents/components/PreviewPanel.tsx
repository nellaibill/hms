import { useEffect, useState } from 'react';
import { Archive, Download, FileQuestion, Loader2, Trash2, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { cn } from '@/lib/utils';
import { documentsApi } from '../../../services/apiClient';
import { EntityTypeBadge } from './EntityTypeBadge';
import { FileTypeIcon } from './FileTypeIcon';
import { StatusBadge } from './StatusBadge';
import { getFileKind } from '../utils/fileKind';
import { formatDateTime, formatFileSize } from '../utils/format';
import type { HmsDocument } from '../types';

interface DetailRowProps {
  label: string;
  children: React.ReactNode;
}

function DetailRow({ label, children }: DetailRowProps) {
  return (
    <div className="flex items-center justify-between gap-3 py-1.5 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="min-w-0 truncate text-right font-medium text-foreground">{children}</span>
    </div>
  );
}

/** Image/PDF content is fetched on demand from the authenticated .../content endpoint — the
 * backend never hands out a plain URL for it (see downloadDocument's own doc comment), so
 * there's no synchronous src to point an <img>/<iframe> at. */
function useDocumentContentUrl(doc: HmsDocument, enabled: boolean) {
  const [state, setState] = useState<{ url: string | null; status: 'loading' | 'ready' | 'error' }>({
    url: null,
    status: 'loading',
  });

  useEffect(() => {
    if (!enabled) return;
    let cancelled = false;
    let objectUrl: string | null = null;
    setState({ url: null, status: 'loading' });

    documentsApi
      .getDocumentContent(doc.id)
      .then((blob) => {
        if (cancelled) return;
        objectUrl = URL.createObjectURL(blob);
        setState({ url: objectUrl, status: 'ready' });
      })
      .catch(() => {
        if (!cancelled) setState({ url: null, status: 'error' });
      });

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [doc.id, enabled]);

  return state;
}

function PreviewArea({ doc }: { doc: HmsDocument }) {
  const kind = getFileKind(doc.mimeType);
  const previewable = kind === 'image' || kind === 'pdf';
  const { url, status } = useDocumentContentUrl(doc, previewable);

  if (previewable && status === 'loading') {
    return (
      <div className="flex items-center justify-center rounded-lg border border-border bg-muted/30 py-16">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" aria-hidden="true" />
      </div>
    );
  }

  if (kind === 'image' && url) {
    return (
      <div className="flex items-center justify-center overflow-hidden rounded-lg border border-border bg-muted/30 p-2">
        <img src={url} alt={doc.originalFileName} className="max-h-72 w-auto rounded object-contain" />
      </div>
    );
  }

  if (kind === 'pdf' && url) {
    return (
      <div className="overflow-hidden rounded-lg border border-border bg-muted/30">
        <iframe src={url} title={doc.originalFileName} className="h-72 w-full" />
      </div>
    );
  }

  if (kind === 'docx' || kind === 'xlsx') {
    return (
      <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border bg-muted/30 px-6 py-10 text-center">
        <FileTypeIcon mimeType={doc.mimeType} className="h-10 w-10" />
        <div>
          <p className="text-sm font-medium text-foreground">This file type doesn&rsquo;t support inline preview.</p>
          <p className="mt-0.5 text-sm text-muted-foreground">Download the file to view its contents.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border bg-muted/30 px-6 py-10 text-center">
      <FileQuestion className="h-10 w-10 text-muted-foreground" aria-hidden="true" />
      <div>
        <p className="text-sm font-medium text-foreground">Preview not available.</p>
        <p className="mt-0.5 text-sm text-muted-foreground">Download to view.</p>
      </div>
    </div>
  );
}

interface PreviewPanelProps {
  doc: HmsDocument;
  onClose?: () => void;
  onDownload: (doc: HmsDocument) => void;
  onArchive: (doc: HmsDocument) => void;
  onDelete: (doc: HmsDocument) => void;
  getEntityLabel: (doc: HmsDocument) => string;
  getUploaderLabel: (doc: HmsDocument) => string;
  className?: string;
}

export function PreviewPanel({ doc, onClose, onDownload, onArchive, onDelete, getEntityLabel, getUploaderLabel, className }: PreviewPanelProps) {
  const { hasPermission } = useAuth();
  const canEdit = hasPermission('records-compliance.edit');
  const canDelete = hasPermission('records-compliance.delete');
  return (
    <div className={cn('flex h-full flex-col', className)}>
      <div className="flex items-start justify-between gap-2 border-b border-border p-4">
        <div className="flex min-w-0 items-start gap-2.5">
          <FileTypeIcon mimeType={doc.mimeType} className="mt-0.5" />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-foreground">{doc.originalFileName}</p>
            <p className="mt-0.5 text-xs text-muted-foreground">{doc.documentType}</p>
          </div>
        </div>
        {onClose && (
          <Button variant="ghost" size="icon" className="h-8 w-8 shrink-0" aria-label="Close preview" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      <div className="flex flex-1 flex-col gap-5 overflow-y-auto p-4">
        <PreviewArea doc={doc} />

        <section className="flex flex-col gap-1 rounded-lg border border-border p-3">
          <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">Metadata</h3>
          <DetailRow label="Document Type">{doc.documentType}</DetailRow>
          <DetailRow label="Entity Type">
            <EntityTypeBadge entityType={doc.entityType} />
          </DetailRow>
          <DetailRow label="Entity Name">{getEntityLabel(doc)}</DetailRow>
          <DetailRow label="Uploaded By">{getUploaderLabel(doc)}</DetailRow>
          <DetailRow label="Uploaded Date">{formatDateTime(doc.uploadedAt)}</DetailRow>
          <DetailRow label="Mime Type">
            <span className="font-mono text-xs">{doc.mimeType}</span>
          </DetailRow>
          <DetailRow label="File Size">{formatFileSize(doc.fileSize)}</DetailRow>
          <DetailRow label="Status">
            <StatusBadge isArchived={doc.isArchived} />
          </DetailRow>
        </section>
      </div>

      <div className="flex items-center justify-end gap-2 border-t border-border p-4">
        {!doc.isArchived && canEdit && (
          <Button variant="outline" onClick={() => onArchive(doc)}>
            <Archive className="h-4 w-4" />
            Archive
          </Button>
        )}
        {canDelete && (
          <Button variant="outline" className="text-destructive hover:text-destructive" onClick={() => onDelete(doc)}>
            <Trash2 className="h-4 w-4" />
            Delete
          </Button>
        )}
        <Button onClick={() => onDownload(doc)}>
          <Download className="h-4 w-4" />
          Download
        </Button>
      </div>
    </div>
  );
}
