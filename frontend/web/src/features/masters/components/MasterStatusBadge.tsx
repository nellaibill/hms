import { Badge } from '@/components/ui/badge';

interface MasterStatusBadgeProps {
  isActive: boolean;
}

export function MasterStatusBadge({ isActive }: MasterStatusBadgeProps) {
  return <Badge variant={isActive ? 'success' : 'secondary'}>{isActive ? 'Active' : 'Inactive'}</Badge>;
}
