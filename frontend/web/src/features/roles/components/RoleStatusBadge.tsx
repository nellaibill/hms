import { Badge } from '@/components/ui/badge';
import type { RoleStatus } from '../types';

interface RoleStatusBadgeProps {
  status: RoleStatus;
}

export function RoleStatusBadge({ status }: RoleStatusBadgeProps) {
  return <Badge variant={status === 'Active' ? 'success' : 'secondary'}>{status}</Badge>;
}
