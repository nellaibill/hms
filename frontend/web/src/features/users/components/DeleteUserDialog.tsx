import type { User } from '@hms/shared';

interface DeleteUserDialogProps {
  user: User;
  isDeleting: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteUserDialog({ user, isDeleting, onConfirm, onCancel }: DeleteUserDialogProps) {
  return (
    <div className="dialog-backdrop" role="presentation" onClick={onCancel}>
      <div
        className="dialog"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="delete-user-title"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id="delete-user-title">Delete user?</h2>
        <p>
          This will remove <strong>{user.firstName} {user.lastName}</strong> ({user.email}) from active lists. The
          record is retained (soft delete) — see docs/DatabaseArchitecture.md §6.
        </p>
        <div className="dialog-actions">
          <button type="button" onClick={onCancel} disabled={isDeleting}>
            Cancel
          </button>
          <button type="button" onClick={onConfirm} disabled={isDeleting}>
            {isDeleting ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  );
}
