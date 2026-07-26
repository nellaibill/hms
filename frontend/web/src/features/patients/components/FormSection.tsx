import type { ReactNode } from 'react';

interface FormSectionProps {
  id: string;
  title: string;
  description?: string;
  children: ReactNode;
}

/** Consistent card-styled `<fieldset>` used by every section of the registration/edit forms — keeps spacing, headings, and borders uniform. */
export function FormSection({ id, title, description, children }: FormSectionProps) {
  return (
    <fieldset id={id} className="scroll-mt-24 rounded-lg border border-border bg-card p-4 shadow-soft">
      <legend className="px-1 text-lg font-semibold text-foreground">{title}</legend>
      {description && <p className="mb-3 mt-0.5 text-xs text-muted-foreground">{description}</p>}
      <div className={description ? 'flex flex-col gap-3' : 'mt-3 flex flex-col gap-3'}>{children}</div>
    </fieldset>
  );
}

interface FieldProps {
  label: string;
  htmlFor: string;
  error?: string;
  className?: string;
  children: ReactNode;
}

/** Consistent Label + control + inline-error stack used throughout both forms. */
export function Field({ label, htmlFor, error, className, children }: FieldProps) {
  return (
    <div className={className ?? 'flex flex-col gap-1'}>
      <label htmlFor={htmlFor} className="text-sm font-medium leading-none text-foreground">
        {label}
      </label>
      {children}
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}
