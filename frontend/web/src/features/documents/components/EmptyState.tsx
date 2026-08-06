import { FolderOpen, UploadCloud } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface EmptyStateProps {
  onUpload: () => void;
}

export function EmptyState({ onUpload }: EmptyStateProps) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-border bg-card px-6 py-20 text-center">
      <span className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10 text-primary">
        <FolderOpen className="h-8 w-8" aria-hidden="true" />
      </span>
      <p className="text-base font-medium text-foreground">No documents found</p>
      <p className="max-w-sm text-sm text-muted-foreground">
        Upload your first document to start building this entity&rsquo;s document trail.
      </p>
      <Button onClick={onUpload} className="mt-2">
        <UploadCloud className="h-4 w-4" />
        Upload Document
      </Button>
    </div>
  );
}
