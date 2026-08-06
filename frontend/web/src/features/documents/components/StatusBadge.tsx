import { Badge } from '@/components/ui/badge';

interface StatusBadgeProps {
  isArchived: boolean;
}

export function StatusBadge({ isArchived }: StatusBadgeProps) {
  return isArchived ? (
    <Badge variant="secondary" className="bg-muted text-muted-foreground">
      Archived
    </Badge>
  ) : (
    <Badge variant="success">Active</Badge>
  );
}
