import { useState } from 'react';
import { Check } from 'lucide-react';
import type { ConversationType } from '@hms/shared';
import { ApiError } from '@hms/shared';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import { useAuth } from '@/features/auth/AuthContext';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { useCreateConversationMutation } from '../hooks/useConversationMutations';
import { useStaffDirectoryQuery } from '../hooks/useStaffDirectoryQuery';

interface NewConversationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: (conversationId: string) => void;
}

export function NewConversationDialog({ open, onOpenChange, onCreated }: NewConversationDialogProps) {
  const { user } = useAuth();
  const [type, setType] = useState<ConversationType>('OneToOne');
  const [title, setTitle] = useState('');
  const [search, setSearch] = useState('');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  const debouncedSearch = useDebouncedValue(search, 250);
  const directoryQuery = useStaffDirectoryQuery(debouncedSearch);
  const createMutation = useCreateConversationMutation();

  const candidates = (directoryQuery.data ?? []).filter((entry) => entry.id !== user?.id);

  function toggleSelected(id: string) {
    if (type === 'OneToOne') {
      setSelectedIds([id]);
      return;
    }
    setSelectedIds((current) => (current.includes(id) ? current.filter((x) => x !== id) : [...current, id]));
  }

  function reset() {
    setType('OneToOne');
    setTitle('');
    setSearch('');
    setSelectedIds([]);
    setError(null);
  }

  function handleSubmit() {
    setError(null);
    createMutation.mutate(
      { type, title: type === 'Group' ? title.trim() || null : null, participantUserIds: selectedIds },
      {
        onSuccess: (conversation) => {
          reset();
          onOpenChange(false);
          onCreated(conversation.id);
        },
        onError: (err) => {
          setError(err instanceof ApiError ? err.message : 'Failed to start the conversation.');
        },
      },
    );
  }

  const canSubmit =
    type === 'OneToOne' ? selectedIds.length === 1 : selectedIds.length >= 2 && title.trim().length > 0;

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) reset();
        onOpenChange(next);
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Start a conversation</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <Tabs
            value={type}
            onValueChange={(value) => {
              setType(value as ConversationType);
              setSelectedIds([]);
            }}
          >
            <TabsList>
              <TabsTrigger value="OneToOne">Direct message</TabsTrigger>
              <TabsTrigger value="Group">Group</TabsTrigger>
            </TabsList>
          </Tabs>

          {type === 'Group' && (
            <div className="flex flex-col gap-1.5">
              <label htmlFor="group-title" className="text-sm font-medium text-foreground">
                Group name
              </label>
              <Input id="group-title" value={title} onChange={(event) => setTitle(event.target.value)} placeholder="e.g. Ward 4 Team" />
            </div>
          )}

          <div className="flex flex-col gap-1.5">
            <label htmlFor="staff-search" className="text-sm font-medium text-foreground">
              {type === 'OneToOne' ? 'Colleague' : 'Add colleagues'}
            </label>
            <Input id="staff-search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search staff by name…" />
          </div>

          <div className="max-h-56 overflow-y-auto rounded-md border border-border">
            {directoryQuery.isPending ? (
              <p className="px-3 py-4 text-center text-sm text-muted-foreground">Loading staff…</p>
            ) : candidates.length === 0 ? (
              <p className="px-3 py-4 text-center text-sm text-muted-foreground">No matching staff found.</p>
            ) : (
              candidates.map((entry) => {
                const isSelected = selectedIds.includes(entry.id);
                return (
                  <button
                    key={entry.id}
                    type="button"
                    onClick={() => toggleSelected(entry.id)}
                    className={cn(
                      'flex w-full items-center justify-between gap-2 border-b border-border/60 px-3 py-2 text-left text-sm last:border-b-0 hover:bg-muted/60',
                      isSelected && 'bg-accent',
                    )}
                  >
                    <span>
                      <span className="font-medium text-foreground">
                        {entry.firstName} {entry.lastName}
                      </span>
                      <span className="ml-1.5 text-xs text-muted-foreground">{entry.roleName}</span>
                    </span>
                    {isSelected && <Check className="h-4 w-4 text-primary" />}
                  </button>
                );
              })
            )}
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!canSubmit || createMutation.isPending}>
            Start conversation
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
