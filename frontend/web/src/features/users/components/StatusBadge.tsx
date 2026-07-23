interface StatusBadgeProps {
  isActive: boolean;
}

export function StatusBadge({ isActive }: StatusBadgeProps) {
  return (
    <span className={isActive ? 'badge badge-active' : 'badge badge-inactive'}>
      {isActive ? 'Active' : 'Inactive'}
    </span>
  );
}
