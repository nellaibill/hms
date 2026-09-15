import { ChevronDown } from 'lucide-react';
import type { ComponentType, ReactNode } from 'react';

interface HeaderIconLabelProps {
  icon: ComponentType<{ className?: string }>;
  label: string;
  /** Marks an item that opens a dropdown menu (Language, Tools, Tasks) vs a direct link
   * (Calendar, Documents) — matches the app's own top bar mockup. */
  showChevron?: boolean;
  /** A badge (e.g. an unread count) anchored to the icon's own corner, not the label below it. */
  badge?: ReactNode;
}

/** Icon-over-label content block shared by every top bar action (Language, Notifications,
 * Calendar, Tools, Tasks, Documents) — the label is always visible now rather than only on
 * hover, so this replaces each item's previous bare icon + Tooltip. */
export function HeaderIconLabel({ icon: Icon, label, showChevron, badge }: HeaderIconLabelProps) {
  return (
    <span className="flex flex-col items-center gap-0.5">
      <span className="relative inline-flex">
        <Icon className="!h-[var(--header-icon-size)] !w-[var(--header-icon-size)]" />
        {badge}
      </span>
      <span className="flex items-center gap-0.5 text-[11px] font-medium leading-none">
        {label}
        {showChevron && <ChevronDown className="h-3 w-3" />}
      </span>
    </span>
  );
}
