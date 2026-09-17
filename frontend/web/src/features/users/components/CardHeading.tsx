import type { LucideIcon } from 'lucide-react';
import { CardDescription, CardTitle } from '@/components/ui/card';

interface CardHeadingProps {
  icon: LucideIcon;
  title: React.ReactNode;
  description?: React.ReactNode;
}

/** A colored icon chip beside the title/description — the same header shape every card on
 * the User Create/Edit/Details pages uses, so they read as one consistent design instead
 * of plain text headers on some cards and not others. */
export function CardHeading({ icon: Icon, title, description }: CardHeadingProps) {
  return (
    <div className="flex items-center gap-3">
      <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
        <Icon className="h-5 w-5" />
      </span>
      <div className="flex flex-col gap-0.5">
        <CardTitle>{title}</CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
      </div>
    </div>
  );
}
