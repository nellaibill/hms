import { CheckCircle2, XCircle } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

export function ResultBadge({ isSuccess }: { isSuccess: boolean }) {
  return isSuccess ? (
    <Badge variant="success" className="gap-1 px-2 py-0.5">
      <CheckCircle2 className="h-3 w-3" />
      Success
    </Badge>
  ) : (
    <Badge variant="destructive" className="gap-1 px-2 py-0.5">
      <XCircle className="h-3 w-3" />
      Failed
    </Badge>
  );
}
