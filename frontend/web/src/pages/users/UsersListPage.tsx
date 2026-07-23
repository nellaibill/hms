import type { User } from '@hms/shared';
import { useState } from 'react';
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
    <section>
      <h1>Users</h1>

      <UserListToolbar
        search={search}
        onSearchChange={handleSearchChange}
        isActive={isActive}
        onIsActiveChange={handleIsActiveChange}
      />

      {isPending && <p>Loading users…</p>}

      {isError && <p className="form-error-banner">{error instanceof Error ? error.message : 'Failed to load users.'}</p>}

      {!isPending && !isError && data && data.items.length === 0 && (
        <div className="empty-state">
          <p>No users found{debouncedSearch ? ` for "${debouncedSearch}"` : ''}.</p>
        </div>
      )}

      {!isPending && !isError && data && data.items.length > 0 && (
        <>
          <UserTable
            users={data.items}
            sort={sort}
            onSortChange={handleSortChange}
            onDeleteRequested={setUserPendingDelete}
            onToggleActive={handleToggleActive}
            isTogglingId={isTogglingId}
          />
          <Pagination meta={data.meta} onPageChange={setPage} />
        </>
      )}

      {userPendingDelete && (
        <DeleteUserDialog
          user={userPendingDelete}
          isDeleting={deleteMutation.isPending}
          onConfirm={handleConfirmDelete}
          onCancel={() => setUserPendingDelete(null)}
        />
      )}
    </section>
  );
}
