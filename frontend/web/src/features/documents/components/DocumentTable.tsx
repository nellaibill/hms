import { cn } from '@/lib/utils';
import { EntityTypeBadge } from './EntityTypeBadge';
import { FileTypeIcon } from './FileTypeIcon';
import { RowActionsMenu } from './RowActionsMenu';
import { StatusBadge } from './StatusBadge';
import { getEntityLabel } from '../mockEntities';
import { formatDate, formatFileSize } from '../utils/format';
import type { HmsDocument } from '../types';

interface DocumentTableProps {
  documents: HmsDocument[];
  selectedId: string | null;
  onSelectRow: (doc: HmsDocument) => void;
  onDownload: (doc: HmsDocument) => void;
  onArchive: (doc: HmsDocument) => void;
  onDelete: (doc: HmsDocument) => void;
}

export function DocumentTable({ documents, selectedId, onSelectRow, onDownload, onArchive, onDelete }: DocumentTableProps) {
  return (
    <>
      {/* Desktop / tablet data grid */}
      <div className="hidden overflow-x-auto rounded-lg border border-border bg-card shadow-soft md:block">
        <table className="w-full min-w-[960px] border-collapse text-sm">
          <thead>
            <tr className="border-b border-border bg-muted/40 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              <th scope="col" className="w-10 px-3 py-3"></th>
              <th scope="col" className="px-3 py-3">File Name</th>
              <th scope="col" className="px-3 py-3">Original File Name</th>
              <th scope="col" className="px-3 py-3">Entity Type</th>
              <th scope="col" className="px-3 py-3">Entity</th>
              <th scope="col" className="px-3 py-3">Document Type</th>
              <th scope="col" className="px-3 py-3 text-right">Size</th>
              <th scope="col" className="px-3 py-3">Uploaded By</th>
              <th scope="col" className="px-3 py-3">Uploaded Date</th>
              <th scope="col" className="px-3 py-3">Status</th>
              <th scope="col" className="px-3 py-3 text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {documents.map((doc) => {
              const isSelected = doc.id === selectedId;
              return (
                <tr
                  key={doc.id}
                  onClick={() => onSelectRow(doc)}
                  aria-selected={isSelected}
                  tabIndex={0}
                  onKeyDown={(e) => e.key === 'Enter' && onSelectRow(doc)}
                  className={cn(
                    'cursor-pointer border-b border-border last:border-b-0 transition-colors hover:bg-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset',
                    isSelected && 'bg-primary/5 hover:bg-primary/10',
                  )}
                >
                  <td className="px-3 py-3">
                    <FileTypeIcon mimeType={doc.mimeType} />
                  </td>
                  <td className="max-w-[200px] px-3 py-3 font-medium text-foreground">
                    <span className="block truncate">{doc.fileName}</span>
                  </td>
                  <td className="max-w-[200px] px-3 py-3 text-muted-foreground">
                    <span className="block truncate">{doc.originalFileName}</span>
                  </td>
                  <td className="px-3 py-3">
                    <EntityTypeBadge entityType={doc.entityType} />
                  </td>
                  <td className="max-w-[180px] px-3 py-3 text-muted-foreground">
                    <span className="block truncate">{getEntityLabel(doc.entityType, doc.entityId)}</span>
                  </td>
                  <td className="px-3 py-3 text-foreground">{doc.documentType}</td>
                  <td className="whitespace-nowrap px-3 py-3 text-right font-mono text-xs text-muted-foreground">
                    {formatFileSize(doc.fileSize)}
                  </td>
                  <td className="px-3 py-3 text-muted-foreground">{doc.uploadedBy}</td>
                  <td className="whitespace-nowrap px-3 py-3 text-muted-foreground">{formatDate(doc.uploadedAt)}</td>
                  <td className="px-3 py-3">
                    <StatusBadge isArchived={doc.isArchived} />
                  </td>
                  <td className="px-3 py-3" onClick={(e) => e.stopPropagation()}>
                    <RowActionsMenu doc={doc} onPreview={onSelectRow} onDownload={onDownload} onArchive={onArchive} onDelete={onDelete} />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Mobile card layout */}
      <div className="flex flex-col gap-3 md:hidden">
        {documents.map((doc) => {
          const isSelected = doc.id === selectedId;
          return (
            <div
              key={doc.id}
              onClick={() => onSelectRow(doc)}
              className={cn(
                'flex flex-col gap-3 rounded-lg border border-border bg-card p-4 shadow-soft transition-colors',
                isSelected && 'border-primary/40 bg-primary/5',
              )}
            >
              <div className="flex items-start gap-3">
                <FileTypeIcon mimeType={doc.mimeType} className="mt-0.5" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-foreground">{doc.originalFileName}</p>
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    {doc.documentType} · {formatFileSize(doc.fileSize)}
                  </p>
                </div>
                <StatusBadge isArchived={doc.isArchived} />
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <EntityTypeBadge entityType={doc.entityType} />
                <span className="truncate text-xs text-muted-foreground">{getEntityLabel(doc.entityType, doc.entityId)}</span>
              </div>

              <div className="flex items-center justify-between border-t border-border pt-3">
                <span className="text-xs text-muted-foreground">
                  {doc.uploadedBy} · {formatDate(doc.uploadedAt)}
                </span>
                <div onClick={(e) => e.stopPropagation()}>
                  <RowActionsMenu doc={doc} onPreview={onSelectRow} onDownload={onDownload} onArchive={onArchive} onDelete={onDelete} />
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </>
  );
}
