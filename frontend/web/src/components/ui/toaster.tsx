import { AlertTriangle, CheckCircle2, Info, X, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useToast, type ToastItem, type ToastVariant } from './toast-context';

const VARIANT_STYLES: Record<ToastVariant, { icon: typeof Info; classes: string; iconClasses: string }> = {
  default: { icon: Info, classes: 'border-border bg-card text-card-foreground', iconClasses: 'text-info' },
  success: { icon: CheckCircle2, classes: 'border-success/30 bg-success/10 text-foreground', iconClasses: 'text-success' },
  error: { icon: XCircle, classes: 'border-destructive/30 bg-destructive/10 text-foreground', iconClasses: 'text-destructive' },
  warning: { icon: AlertTriangle, classes: 'border-warning/30 bg-warning/10 text-foreground', iconClasses: 'text-warning' },
};

function ToastCard({ item }: { item: ToastItem }) {
  const { dismiss, pause, resume } = useToast();
  const { icon: Icon, classes, iconClasses } = VARIANT_STYLES[item.variant];

  return (
    <div
      role="status"
      aria-live={item.variant === 'error' ? 'assertive' : 'polite'}
      onMouseEnter={() => pause(item.id)}
      onMouseLeave={() => resume(item.id)}
      onFocus={() => pause(item.id)}
      onBlur={() => resume(item.id)}
      tabIndex={0}
      className={cn(
        'pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-lg border p-4 shadow-soft-lg animate-in slide-in-from-bottom-2',
        classes,
      )}
    >
      <Icon className={cn('mt-0.5 h-5 w-5 shrink-0', iconClasses)} aria-hidden="true" />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold">{item.title}</p>
        {item.description && <p className="mt-0.5 text-sm text-muted-foreground">{item.description}</p>}
      </div>
      <button
        type="button"
        onClick={() => dismiss(item.id)}
        aria-label="Dismiss notification"
        className="shrink-0 rounded-sm p-0.5 text-muted-foreground opacity-70 transition-opacity hover:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        <X className="h-4 w-4" />
      </button>
    </div>
  );
}

export function Toaster() {
  const { toasts } = useToast();

  return (
    <div className="pointer-events-none fixed inset-x-4 bottom-4 z-[1500] flex flex-col items-center gap-2 sm:inset-x-auto sm:right-4 sm:items-end">
      {toasts.map((item) => (
        <ToastCard key={item.id} item={item} />
      ))}
    </div>
  );
}
