import { useMemo, useRef, useState } from 'react';
import { AlertCircle, CheckCircle2, UploadCloud, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/toast-context';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { ACCEPT_ATTRIBUTE, ACCEPTED_EXTENSIONS_LABEL, DOCUMENT_TYPE_LABELS, ENTITY_TYPE_META, MAX_FILE_SIZE_MB } from '../constants';
import { useUploadDocumentMutation } from '../hooks/useDocumentMutations';
import { ENTITY_OPTIONS } from '../mockEntities';
import { DOCUMENT_TYPES, ENTITY_TYPES } from '../types';
import { validateUploadForm, isUploadFormValid } from '../validation';
import { FileTypeIcon } from './FileTypeIcon';
import { formatFileSize } from '../utils/format';
import type { DocumentUploadFormValues, EntityType, HmsDocument } from '../types';

const EMPTY_VALUES: DocumentUploadFormValues = { entityType: '', entityId: '', documentType: '', file: null };

interface UploadDocumentModalProps {
  open: boolean;
  onClose: () => void;
  onUploaded: (doc: HmsDocument) => void;
  defaultEntityType?: EntityType;
  defaultEntityId?: string;
}

export function UploadDocumentModal({ open, onClose, onUploaded, defaultEntityType, defaultEntityId }: UploadDocumentModalProps) {
  const [values, setValues] = useState<DocumentUploadFormValues>({
    ...EMPTY_VALUES,
    entityType: defaultEntityType ?? '',
    entityId: defaultEntityId ?? '',
  });
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { status, progress, error, upload, reset } = useUploadDocumentMutation();
  const { toast } = useToast();
  // Below `sm`, present as a bottom sheet instead of a centered modal (see responsive spec).
  const isMobile = useMediaQuery('(max-width: 639px)');

  const errors = useMemo(() => validateUploadForm(values), [values]);
  const valid = isUploadFormValid(errors);
  const busy = status === 'uploading';

  const entityOptions = values.entityType ? ENTITY_OPTIONS[values.entityType].map((e) => ({ value: e.id, label: e.label })) : [];

  function handleClose() {
    if (busy) return;
    setValues({ ...EMPTY_VALUES, entityType: defaultEntityType ?? '', entityId: defaultEntityId ?? '' });
    reset();
    onClose();
  }

  function handleFileSelected(file: File) {
    setValues((prev) => ({ ...prev, file }));
  }

  function handleDrop(e: React.DragEvent<HTMLDivElement>) {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files?.[0];
    if (file) handleFileSelected(file);
  }

  function handleUpload() {
    if (!valid) return;
    upload(values, (doc) => {
      toast({ title: 'Document uploaded', description: `${doc.originalFileName} was uploaded successfully.`, variant: 'success' });
      setTimeout(() => {
        onUploaded(doc);
        handleClose();
      }, 500);
    });
  }

  return (
    <Dialog open={open} onOpenChange={(next) => !next && handleClose()}>
      <DialogContent
        className={cn(
          'max-h-[85vh] overflow-y-auto',
          isMobile
            ? 'inset-x-0 bottom-0 left-0 top-auto w-full max-w-none translate-x-0 translate-y-0 rounded-b-none rounded-t-2xl'
            : 'max-w-2xl',
        )}
      >
        <DialogHeader>
          <DialogTitle>Upload Document</DialogTitle>
          <DialogDescription>Attach a document to any record across the HMS by entity type and entity.</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-5 py-2">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="upload-entity-type">
                Entity Type <span className="font-normal text-muted-foreground">(required)</span>
              </Label>
              <Select
                value={values.entityType}
                disabled={busy}
                onValueChange={(value) => setValues((prev) => ({ ...prev, entityType: value as EntityType, entityId: '' }))}
              >
                <SelectTrigger id="upload-entity-type" aria-invalid={!!errors.entityType}>
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {ENTITY_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {ENTITY_TYPE_META[type].label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.entityType && (
                <p role="alert" className="text-xs text-destructive">
                  {errors.entityType}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="upload-entity">
                Entity <span className="font-normal text-muted-foreground">(required)</span>
              </Label>
              <SearchableSelect
                id="upload-entity"
                value={values.entityId}
                onValueChange={(value) => setValues((prev) => ({ ...prev, entityId: value }))}
                options={entityOptions}
                placeholder={values.entityType ? 'Search & select' : 'Select entity type first'}
                searchPlaceholder="Search entity…"
                ariaLabel="Entity"
                disabled={!values.entityType || busy}
              />
              {errors.entity && (
                <p role="alert" className="text-xs text-destructive">
                  {errors.entity}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="upload-document-type">
                Document Type <span className="font-normal text-muted-foreground">(required)</span>
              </Label>
              <Select
                value={values.documentType}
                disabled={busy}
                onValueChange={(value) => setValues((prev) => ({ ...prev, documentType: value as DocumentUploadFormValues['documentType'] }))}
              >
                <SelectTrigger id="upload-document-type" aria-invalid={!!errors.documentType}>
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {DOCUMENT_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {DOCUMENT_TYPE_LABELS[type]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.documentType && (
                <p role="alert" className="text-xs text-destructive">
                  {errors.documentType}
                </p>
              )}
            </div>
          </div>

          <div>
            <Label>
              File <span className="font-normal text-muted-foreground">(required)</span>
            </Label>

            <input
              ref={fileInputRef}
              type="file"
              accept={ACCEPT_ATTRIBUTE}
              className="sr-only"
              aria-label="Choose file to upload"
              disabled={busy}
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) handleFileSelected(file);
                e.target.value = '';
              }}
            />

            {!values.file ? (
              <div
                role="button"
                tabIndex={0}
                aria-label="Drag and drop a file here, or press Enter to browse files"
                onClick={() => fileInputRef.current?.click()}
                onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && fileInputRef.current?.click()}
                onDragOver={(e) => {
                  e.preventDefault();
                  setIsDragging(true);
                }}
                onDragLeave={() => setIsDragging(false)}
                onDrop={handleDrop}
                className={cn(
                  'mt-1.5 flex cursor-pointer flex-col items-center gap-2 rounded-lg border-2 border-dashed px-6 py-10 text-center transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                  isDragging ? 'border-primary bg-primary/5' : 'border-border bg-muted/30 hover:bg-muted/50',
                  errors.file && 'border-destructive/50',
                )}
              >
                <UploadCloud className={cn('h-8 w-8', isDragging ? 'text-primary' : 'text-muted-foreground')} aria-hidden="true" />
                <p className="text-sm font-medium text-foreground">
                  Drag &amp; drop a file here, or{' '}
                  <span className="text-primary underline underline-offset-2">browse</span>
                </p>
                <p className="text-xs text-muted-foreground">Supported formats: {ACCEPTED_EXTENSIONS_LABEL}</p>
                <p className="text-xs text-muted-foreground">Maximum file size: {MAX_FILE_SIZE_MB} MB</p>
              </div>
            ) : (
              <div className="mt-1.5 flex items-center gap-3 rounded-lg border border-border bg-muted/30 px-4 py-3">
                <FileTypeIcon mimeType={values.file.type} />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-foreground">{values.file.name}</p>
                  <p className="text-xs text-muted-foreground">{formatFileSize(values.file.size)}</p>
                </div>
                {!busy && status !== 'completed' && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 shrink-0"
                    aria-label="Remove selected file"
                    onClick={() => setValues((prev) => ({ ...prev, file: null }))}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </div>
            )}

            {errors.file && (
              <p role="alert" className="mt-1.5 text-xs text-destructive">
                {errors.file}
              </p>
            )}
          </div>

          {status !== 'idle' && (
            <div className="flex flex-col gap-2 rounded-lg border border-border p-4">
              {status === 'uploading' && (
                <>
                  <div className="flex items-center justify-between text-sm">
                    <span className="font-medium text-foreground">Uploading…</span>
                    <span className="tabular-nums text-muted-foreground">{Math.round(progress)}%</span>
                  </div>
                  <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      className="h-full rounded-full bg-primary transition-[width] duration-200 ease-out"
                      style={{ width: `${progress}%` }}
                    />
                  </div>
                </>
              )}
              {status === 'completed' && (
                <div className="flex items-center gap-2 text-sm font-medium text-success">
                  <CheckCircle2 className="h-4 w-4" />
                  Upload complete
                </div>
              )}
              {status === 'error' && (
                <div className="flex items-center gap-2 text-sm font-medium text-destructive">
                  <AlertCircle className="h-4 w-4" />
                  {error}
                </div>
              )}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose} disabled={busy}>
            Cancel
          </Button>
          <Button onClick={handleUpload} disabled={!valid || busy}>
            {busy ? 'Uploading…' : 'Upload'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
