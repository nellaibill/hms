import * as React from 'react';
import { cn } from '@/lib/utils';

interface TabsContextValue {
  value: string;
  setValue: (value: string) => void;
}

const TabsContext = React.createContext<TabsContextValue | null>(null);

function useTabsContext(component: string) {
  const ctx = React.useContext(TabsContext);
  if (!ctx) throw new Error(`<${component} /> must be used within <Tabs>`);
  return ctx;
}

interface TabsProps {
  value?: string;
  defaultValue?: string;
  onValueChange?: (value: string) => void;
  className?: string;
  children: React.ReactNode;
}

/** Plain, dependency-free tabs (no Radix Tabs installed) — same controlled/uncontrolled API shape as shadcn's. */
function Tabs({ value, defaultValue, onValueChange, className, children }: TabsProps) {
  const [internalValue, setInternalValue] = React.useState(defaultValue ?? '');
  const isControlled = value !== undefined;
  const current = isControlled ? value : internalValue;
  const containerRef = React.useRef<HTMLDivElement>(null);

  const setValue = (next: string) => {
    if (!isControlled) setInternalValue(next);
    onValueChange?.(next);
    // Switching tabs (via a trigger click or Previous/Next) swaps the panel in place —
    // without this, a user scrolled down on a long tab lands on the new tab wherever
    // they happened to be scrolled to, possibly past its content entirely.
    // 'instant', not 'smooth': confirmed live (see PatientRegistrationForm's identical fix
    // for its Previous/Next buttons, which bypass this component's setValue entirely) that a
    // 'smooth' scrollIntoView call here can produce zero net movement — the animation gets
    // cancelled by the layout shift from the tab content swapping right as this runs.
    containerRef.current?.scrollIntoView({ behavior: 'instant', block: 'start' });
  };

  return (
    <TabsContext.Provider value={{ value: current, setValue }}>
      <div ref={containerRef} className={cn('scroll-mt-20', className)}>
        {children}
      </div>
    </TabsContext.Provider>
  );
}

const TabsList = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    role="tablist"
    className={cn('flex items-center gap-1 overflow-x-auto border-b border-border [&>*]:shrink-0', className)}
    {...props}
  />
));
TabsList.displayName = 'TabsList';

interface TabsTriggerProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  value: string;
  /** Shows a small red dot — this tab's fields have a validation error the user hasn't seen yet. */
  hasError?: boolean;
}

const TabsTrigger = React.forwardRef<HTMLButtonElement, TabsTriggerProps>(({ className, value, hasError, children, ...props }, ref) => {
  const { value: active, setValue } = useTabsContext('TabsTrigger');
  const isActive = active === value;

  return (
    <button
      ref={ref}
      type="button"
      role="tab"
      aria-selected={isActive}
      onClick={() => setValue(value)}
      className={cn(
        'relative inline-flex items-center gap-1.5 whitespace-nowrap border-b-2 px-3 py-2.5 text-sm font-medium transition-colors',
        isActive
          ? 'border-primary text-primary'
          : 'border-transparent text-muted-foreground hover:text-foreground',
        className,
      )}
      {...props}
    >
      {children}
      {hasError && <span aria-label="This section has errors" className="absolute right-0.5 top-1.5 h-1.5 w-1.5 rounded-full bg-destructive" />}
    </button>
  );
});
TabsTrigger.displayName = 'TabsTrigger';

interface TabsContentProps extends React.HTMLAttributes<HTMLDivElement> {
  value: string;
}

const TabsContent = React.forwardRef<HTMLDivElement, TabsContentProps>(({ className, value, children, ...props }, ref) => {
  const { value: active } = useTabsContext('TabsContent');
  if (active !== value) return null;

  return (
    <div ref={ref} role="tabpanel" className={cn('flex flex-col gap-4', className)} {...props}>
      {children}
    </div>
  );
});
TabsContent.displayName = 'TabsContent';

export { Tabs, TabsList, TabsTrigger, TabsContent };
