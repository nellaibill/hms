import type { DocumentResponse, DocumentStatus } from '@hms/shared';
import { Download, Loader2, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { Badge, type BadgeProps } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/AuthContext';
import { documentsApi } from '@/services/apiClient';
import { useDeleteEmployeeDocumentMutation } from '../hooks/useEmployeeDocumentMutations';
import { useEmployeeDocumentsQuery } from '../hooks/useEmployeeDocumentsQuery';
import { downloadBlob } from '../utils/downloadBlob';
import { UploadEmployeeDocumentDialog } from './UploadEmployeeDocumentDialog';

const STATUS_VARIANT: Record<DocumentStatus, NonNullable<BadgeProps['variant']>> = {
  Pending: 'warning',
  Available: 'success',
  Quarantined: 'destructive',
};

interface EmployeeDocumentsPanelProps {
  employeeId: string;
}

/**
 * Real-API-backed employee documents panel (Hospital HR Management MVP) — the Documents
 * module's endpoints require records-compliance.{view|create|delete} permissions, NOT
 * workforce-admin (see DocumentsController.cs), so every action here is gated on those
 * instead of the workforce-admin checks the rest of the Employee module uses.
 */
export function EmployeeDocumentsPanel({ employeeId }: EmployeeDocumentsPanelProps) {
  const { hasPermission } = useAuth();
  const canView = hasPermission('records-compliance.view');
  const canCreate = hasPermission('records-compliance.create');
  const canDelete = hasPermission('records-compliance.delete');

  const documentsQuery = useEmployeeDocumentsQuery(canView ? employeeId : undefined);
  const deleteMutation = useDeleteEmployeeDocumentMutation(employeeId);

  const [showUpload, setShowUpload] = useState(false);
  const [pendingDelete, setPendingDelete] = useState<DocumentResponse | null>(null);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  if (!canView) {
    return <p className="py-8 text-center text-sm text-muted-foreground">You don't have permission to view this employee's documents.</p>;
  }

  async function handleDownload(doc: DocumentResponse) {
    setDownloadingId(doc.id);
    try {
      const blob = await documentsApi.getDocumentContent(doc.id);
      downloadBlob(blob, doc.originalFileName);
    } finally {
      setDownloadingId(null);
    }
  }

  function handleConfirmDelete() {
    if (!pendingDelete) return;
    deleteMutation.mutate(pendingDelete.id, { onSuccess: () => setPendingDelete(null) });
  }

  return (
    <div className="flex flex-col gap-4">
      {canCreate && (
        <Button variant="outline" size="sm" className="gap-1.5 self-start" onClick={() => setShowUpload(true)}>
          <Plus className="h-4 w-4" />
          Upload Document
        </Button>
      )}

      {documentsQuery.isPending && (
        <div className="flex items-center justify-center gap-2 py-8 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading documents…
        </div>
      )}

      {documentsQuery.isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load documents.
        </p>
      )}

      {documentsQuery.data && documentsQuery.data.length === 0 && (
        <p className="py-8 text-center text-sm text-muted-foreground">No documents uploaded yet.</p>
      )}

      {documentsQuery.data && documentsQuery.data.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-4 py-2.5">Type</th>
                <th className="px-4 py-2.5">File Name</th>
                <th className="px-4 py-2.5">Uploaded</th>
                <th className="px-4 py-2.5">Expiry</th>
                <th className="px-4 py-2.5">Status</th>
                <th className="px-4 py-2.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {documentsQuery.data.map((doc) => (
                <tr key={doc.id} className="hover:bg-muted/30">
                  <td className="px-4 py-3 text-muted-foreground">{doc.documentType}</td>
                  <td className="px-4 py-3 font-medium text-foreground">{doc.originalFileName}</td>
                  <td className="px-4 py-3 text-muted-foreground">{new Date(doc.createdAt).toLocaleDateString('en-IN')}</td>
                  <td className="px-4 py-3 text-muted-foreground">{doc.expiryDate ?? '—'}</td>
                  <td className="px-4 py-3">
                    <Badge variant={STATUS_VARIANT[doc.status]}>{doc.status}</Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-1.5">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1"
                        disabled={doc.status !== 'Available' || downloadingId === doc.id}
                        onClick={() => handleDownload(doc)}
                      >
                        <Download className="h-3.5 w-3.5" />
                        {downloadingId === doc.id ? 'Downloading…' : 'Download'}
                      </Button>
                      {canDelete && (
                        <Button variant="ghost" size="sm" className="gap-1 text-destructive hover:text-destructive" onClick={() => setPendingDelete(doc)}>
                          <Trash2 className="h-3.5 w-3.5" />
                          Delete
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showUpload && <UploadEmployeeDocumentDialog employeeId={employeeId} onClose={() => setShowUpload(false)} />}

      {pendingDelete && (
        <Dialog open onOpenChange={(open) => !open && setPendingDelete(null)}>
          <DialogContent role="alertdialog" aria-labelledby="delete-document-title">
            <DialogHeader>
              <DialogTitle id="delete-document-title">Delete document?</DialogTitle>
              <DialogDescription>
                This will permanently delete <strong className="text-foreground">{pendingDelete.originalFileName}</strong>.
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <Button variant="outline" onClick={() => setPendingDelete(null)} disabled={deleteMutation.isPending}>
                Cancel
              </Button>
              <Button variant="destructive" onClick={handleConfirmDelete} disabled={deleteMutation.isPending}>
                {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
