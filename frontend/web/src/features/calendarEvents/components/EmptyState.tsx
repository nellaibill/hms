import { CalendarPlus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';

interface EmptyStateProps {
  onCreateEvent: () => void;
}

export function EmptyState({ onCreateEvent }: EmptyStateProps) {
  const { hasPermission } = useAuth();
  const canCreate = hasPermission('engagement.create');
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-3 px-6 py-20 text-center">
      <span className="flex h-16 w-16 items-center justify-center rounded-full bg-primary/10 text-primary">
        <CalendarPlus className="h-8 w-8" aria-hidden="true" />
      </span>
      <p className="text-base font-medium text-foreground">No events found.</p>
      <p className="max-w-sm text-sm text-muted-foreground">
        {canCreate
          ? 'Nothing matches the current filters. Try clearing them, or add the first event to this calendar.'
          : 'Nothing matches the current filters.'}
      </p>
      {canCreate && (
        <Button onClick={onCreateEvent} className="mt-2">
          Create First Event
        </Button>
      )}
    </div>
  );
}
