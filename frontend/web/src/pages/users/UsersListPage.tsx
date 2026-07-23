import type { User } from '@hms/shared';
import { Loader2, Users as UsersIcon } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import {
  DeleteUserDialog,
  Pagination,
  UserListToolbar,
  UserTable,
  useActivateUserMutation,
  useDeactivateUserMutation,
  useDeleteUserMutation,
  useUsersQuery,
} from '../../features/users';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';

export default function UsersListPage() {
  const [search, setSearch] = useState('');
  const [isActive, setIsActive] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-createdAt');
  const [userPendingDelete, setUserPendingDelete] = useState<User | null>(null);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useUsersQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
    isActive,
  });

  const deleteMutation = useDeleteUserMutation();
  const activateMutation = useActivateUserMutation();
  const deactivateMutation = useDeactivateUserMutation();

  const isTogglingId = activateMutation.isPending
    ? (activateMutation.variables as string | undefined)
    : deactivateMutation.isPending
      ? (deactivateMutation.variables as string | undefined)
      : undefined;

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleIsActiveChange(value: boolean | undefined) {
    setIsActive(value);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  function handleToggleActive(user: User) {
    if (user.isActive) {
      deactivateMutation.mutate(user.id);
    } else {
      activateMutation.mutate(user.id);
    }
  }

  function handleConfirmDelete() {
    if (!userPendingDelete) {
      return;
    }
    deleteMutation.mutate(userPendingDelete.id, {
      onSuccess: () => setUserPendingDelete(null),
    });
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div className="flex items-start gap-3 border-b border-border pb-4">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
          <UsersIcon className="h-5 w-5" />
        </span>
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-foreground">Users</h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Manage system accounts — the Identity reference module, connected live to the HMS.Api backend.
          </p>
        </div>
      </div>

      <UserListToolbar
        search={search}
        onSearchChange={handleSearchChange}
        isActive={isActive}
        onIsActiveChange={handleIsActiveChange}
      />

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading users…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error instanceof Error ? error.message : 'Failed to load users.'}
        </p>
      )}

      {!isPending && !isError && data && data.items.length === 0 && (
        <Card className="border-dashed">
          <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
            <p className="text-sm font-medium text-foreground">No users found</p>
            <p className="text-sm text-muted-foreground">
              {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Create the first user account to get started.'}
            </p>
          </CardContent>
        </Card>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <div className="flex flex-col gap-3">
          <UserTable
            users={data.items}
            sort={sort}
            onSortChange={handleSortChange}
            onDeleteRequested={setUserPendingDelete}
            onToggleActive={handleToggleActive}
            isTogglingId={isTogglingId}
          />
          <Pagination meta={data.meta} onPageChange={setPage} />
        </div>
      )}

      {userPendingDelete && (
        <DeleteUserDialog
          user={userPendingDelete}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleConfirmDelete}
          onCancel={() => setUserPendingDelete(null)}
        />
      )}
    </div>
  );
}
