import { ApiError, type DocumentType } from '@hms/shared';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { FileChooserButton } from '@/components/ui/file-chooser-button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useUploadEmployeeDocumentMutation } from '../hooks/useEmployeeDocumentMutations';

const DOCUMENT_TYPE_OPTIONS: Array<{ value: DocumentType; label: string }> = [
  { value: 'IdProof', label: 'ID Proof' },
  { value: 'Certificate', label: 'Certificate' },
  { value: 'ConsentForm', label: 'Consent Form' },
  { value: 'Insurance', label: 'Insurance' },
  { value: 'Prescription', label: 'Prescription' },
  { value: 'Report', label: 'Report' },
  { value: 'Invoice', label: 'Invoice' },
  { value: 'Other', label: 'Other' },
];

interface UploadEmployeeDocumentDialogProps {
  employeeId: string;
  onClose: () => void;
}

export function UploadEmployeeDocumentDialog({ employeeId, onClose }: UploadEmployeeDocumentDialogProps) {
  const [file, setFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState<DocumentType>('IdProof');
  const [expiryDate, setExpiryDate] = useState('');
  const mutation = useUploadEmployeeDocumentMutation(employeeId);

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!file) return;
    mutation.mutate({ file, documentType, expiryDate: expiryDate || null }, { onSuccess: onClose });
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent aria-labelledby="upload-employee-document-title">
        <DialogHeader>
          <DialogTitle id="upload-employee-document-title">Upload Document</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          {apiError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {apiError.message}
            </p>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="doc-file">File</Label>
            <FileChooserButton id="doc-file" accept="*" onFileSelected={setFile} status={file?.name} />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="doc-type">Document Type</Label>
            <Select value={documentType} onValueChange={(value) => setDocumentType(value as DocumentType)}>
              <SelectTrigger id="doc-type" aria-label="Document type">
                <SelectValue placeholder="Select document type" />
              </SelectTrigger>
              <SelectContent>
                {DOCUMENT_TYPE_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="doc-expiryDate">Expiry Date (optional)</Label>
            <Input id="doc-expiryDate" type="date" value={expiryDate} onChange={(event) => setExpiryDate(event.target.value)} />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" disabled={!file || mutation.isPending}>
              {mutation.isPending ? 'Uploading…' : 'Upload'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
