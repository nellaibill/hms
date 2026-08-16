import { useMemo, useState } from 'react';
import { FolderKanban, RefreshCw, UploadCloud } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent } from '@/components/ui/sheet';
import { useToast } from '@/components/ui/toast-context';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import {
  ArchiveConfirmDialog,
  createEmptyFilters,
  DeleteConfirmDialog,
  DocumentTable,
  downloadDocument,
  EmptyState,
  FilterBar,
  FilterBarSkeleton,
  getMockDocumentSummary,
  getMockUploaders,
  PreviewPanel,
  PreviewPanelSkeleton,
  SummaryCards,
  SummaryCardsSkeleton,
  TableSkeleton,
  UploadDocumentModal,
  useArchiveDocumentMutation,
  useDeleteDocumentMutation,
  useDocumentsQuery,
} from '@/features/documents';
import type { DocumentFilters, HmsDocument } from '@/features/documents';
import { useAuth } from '@/features/auth/AuthContext';

export default function DocumentManagementPage() {
  const { hasPermission } = useAuth();
  const [filters, setFilters] = useState<DocumentFilters>(createEmptyFilters());
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [archiveTarget, setArchiveTarget] = useState<HmsDocument | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<HmsDocument | null>(null);

  const { toast } = useToast();
  const isDesktop = useMediaQuery('(min-width: 1024px)');

  const documentsQuery = useDocumentsQuery(filters);
  const archiveMutation = useArchiveDocumentMutation();
  const deleteMutation = useDeleteDocumentMutation();

  const documents = useMemo(() => documentsQuery.data ?? [], [documentsQuery.data]);
  const selectedDoc = useMemo(() => documents.find((d) => d.id === selectedId) ?? null, [documents, selectedId]);
  const summary = getMockDocumentSummary();
  const uploaders = getMockUploaders();

  function handleDownload(doc: HmsDocument) {
    const started = downloadDocument(doc);
    if (started) {
      toast({ title: 'Download started', description: doc.originalFileName });
    } else {
      toast({
        title: 'No file content available',
        description: 'This demo document has no downloadable file attached.',
        variant: 'warning',
      });
    }
  }

  function handleConfirmArchive() {
    if (!archiveTarget) return;
    const target = archiveTarget;
    archiveMutation.archive(target.id, () => {
      toast({ title: 'Document archived', description: `${target.originalFileName} is now hidden from active searches.` });
      setArchiveTarget(null);
    });
  }

  function handleConfirmDelete() {
    if (!deleteTarget) return;
    const target = deleteTarget;
    deleteMutation.remove(target.id, () => {
      toast({ title: 'Document deleted', description: target.originalFileName, variant: 'success' });
      if (selectedId === target.id) setSelectedId(null);
      setDeleteTarget(null);
    });
  }

  const isPending = documentsQuery.isPending;

  return (
    <div className="flex flex-1 flex-col">
      <div className="flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <FolderKanban className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Document Management</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Store and manage documents across all HMS modules.</p>
      </div>

      <div className="flex flex-1 flex-col gap-5 p-6 lg:p-8">
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" onClick={() => documentsQuery.refetch()} disabled={documentsQuery.isFetching}>
            <RefreshCw className={documentsQuery.isFetching ? 'h-4 w-4 animate-spin' : 'h-4 w-4'} />
            Refresh
          </Button>
          {hasPermission('records-compliance.create') && (
            <Button onClick={() => setUploadOpen(true)}>
              <UploadCloud className="h-4 w-4" />
              Upload Document
            </Button>
          )}
        </div>

        {isPending ? <SummaryCardsSkeleton /> : <SummaryCards stats={summary} />}
        {isPending ? <FilterBarSkeleton /> : <FilterBar filters={filters} onApply={setFilters} onReset={() => setFilters(createEmptyFilters())} uploaders={uploaders} />}

        <div className="flex flex-1 gap-5">
          <div className="min-w-0 flex-1">
            {isPending ? (
              <TableSkeleton />
            ) : documents.length === 0 ? (
              <EmptyState onUpload={() => setUploadOpen(true)} />
            ) : (
              <DocumentTable
                documents={documents}
                selectedId={selectedId}
                onSelectRow={(doc) => setSelectedId(doc.id)}
                onDownload={handleDownload}
                onArchive={setArchiveTarget}
                onDelete={setDeleteTarget}
              />
            )}
          </div>

          {isDesktop && (isPending || selectedDoc) && (
            <div className="hidden w-[380px] shrink-0 rounded-lg border border-border bg-card shadow-soft lg:block">
              {isPending ? (
                <PreviewPanelSkeleton />
              ) : (
                selectedDoc && (
                  <PreviewPanel
                    doc={selectedDoc}
                    onClose={() => setSelectedId(null)}
                    onDownload={handleDownload}
                    onArchive={setArchiveTarget}
                    onDelete={setDeleteTarget}
                    className="h-full"
                  />
                )
              )}
            </div>
          )}
        </div>
      </div>

      {/* Tablet / mobile: preview slides over as a right-side sheet instead of an inline column. */}
      <Sheet open={!isDesktop && !!selectedDoc} onOpenChange={(open) => !open && setSelectedId(null)}>
        <SheetContent side="right" className="w-full gap-0 p-0 sm:max-w-md">
          {selectedDoc && (
            <PreviewPanel
              doc={selectedDoc}
              onClose={() => setSelectedId(null)}
              onDownload={handleDownload}
              onArchive={setArchiveTarget}
              onDelete={setDeleteTarget}
              className="h-full"
            />
          )}
        </SheetContent>
      </Sheet>

      <UploadDocumentModal
        open={uploadOpen}
        onClose={() => setUploadOpen(false)}
        onUploaded={(doc) => setSelectedId(doc.id)}
      />

      {archiveTarget && (
        <ArchiveConfirmDialog
          doc={archiveTarget}
          isArchiving={archiveMutation.isPending}
          onConfirm={handleConfirmArchive}
          onCancel={() => setArchiveTarget(null)}
        />
      )}

      {deleteTarget && (
        <DeleteConfirmDialog
          doc={deleteTarget}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleConfirmDelete}
          onCancel={() => setDeleteTarget(null)}
        />
      )}
    </div>
  );
}
