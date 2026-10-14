import {
  BarChart3,
  Boxes,
  ClipboardList,
  type LucideIcon,
  PackagePlus,
  ReceiptText,
  Settings2,
  ShieldCheck,
  Stethoscope,
  Undo2,
} from 'lucide-react';
import type { ReactNode } from 'react';
import { Link, NavLink, useLocation } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthContext';
import { cn } from '@/lib/utils';
import type { PharmacySubLink } from './links';

interface PharmacySection {
  key: string;
  label: string;
  icon: LucideIcon;
  to: string;
  /** Paths (prefixes) that belong to this section, beyond `to` itself. */
  also?: string[];
  /** Exact-match only — for the stock hub at '/pharmacy', which is a prefix of everything. */
  exact?: boolean;
  permission?: string;
}

/** The pharmacy's own menu, in the order a working day uses it: sell, fulfil, buy, look after
 * stock, then the paperwork. */
const SECTIONS: PharmacySection[] = [
  { key: 'counter', label: 'Billing counter', icon: ReceiptText, to: '/pharmacy/counter', permission: 'pharmacy.create' },
  { key: 'sales', label: 'Sales & returns', icon: Undo2, to: '/pharmacy/sales' },
  { key: 'prescriptions', label: 'Prescriptions', icon: Stethoscope, to: '/pharmacy/prescriptions' },
  { key: 'indents', label: 'IP indents', icon: ClipboardList, to: '/pharmacy/indents' },
  { key: 'purchases', label: 'Purchases', icon: PackagePlus, to: '/pharmacy/purchases', also: ['/pharmacy/purchase-orders', '/pharmacy/purchase-returns', '/pharmacy/suppliers', '/pharmacy/reorder'] },
  { key: 'stock', label: 'Stock', icon: Boxes, to: '/pharmacy', exact: true, also: ['/pharmacy/stock-ledger', '/pharmacy/transfers', '/pharmacy/adjustments', '/pharmacy/stock-counts', '/pharmacy/expiry'] },
  { key: 'registers', label: 'Registers', icon: ShieldCheck, to: '/pharmacy/registers' },
  { key: 'reports', label: 'Reports', icon: BarChart3, to: '/pharmacy/reports' },
  { key: 'setup', label: 'Setup', icon: Settings2, to: '/pharmacy/setup', also: ['/pharmacy/stores', '/pharmacy/import', '/pharmacy/settings'] },
];

/** Sections whose screens exist. The menu lists only these, so it never offers a dead link
 * while the module is being built out phase by phase. */
const LIVE_SECTIONS = new Set(['counter', 'sales', 'prescriptions', 'indents', 'purchases', 'stock', 'setup']);

interface PharmacyPageProps {
  title: string;
  subtitle?: ReactNode;
  /** Buttons on the right of the title row. */
  actions?: ReactNode;
  /** Second-level links for the current section (Purchases: invoices, orders, returns…). */
  subLinks?: PharmacySubLink[];
  /** Drops the page padding — for the counter, which fills the screen edge to edge. */
  flush?: boolean;
  children: ReactNode;
}

/**
 * The frame every pharmacy screen sits in: the module's own menu across the top, a compact
 * title row, then the screen. It replaces the tall page banner used elsewhere in the app —
 * a billing or purchase-entry screen needs its height for lines, not for a heading.
 */
export function PharmacyPage({ title, subtitle, actions, subLinks, flush, children }: PharmacyPageProps) {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();

  // Ward staff reach the indent screen without pharmacy rights; they get no pharmacy menu.
  const sections = hasPermission('pharmacy.view')
    ? SECTIONS.filter((section) => LIVE_SECTIONS.has(section.key) && (!section.permission || hasPermission(section.permission)))
    : [];

  function isActive(section: PharmacySection): boolean {
    const own = section.exact ? pathname === section.to : pathname === section.to || pathname.startsWith(`${section.to}/`);
    return own || (section.also ?? []).some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col bg-muted/20">
      <nav aria-label="Pharmacy" className={cn('shrink-0 items-center gap-1 overflow-x-auto border-b border-border bg-background px-3 py-1.5 print:hidden', sections.length > 0 ? 'flex' : 'hidden')}>
        {sections.map((section) => {
          const Icon = section.icon;
          const active = isActive(section);
          return (
            <NavLink
              key={section.key}
              to={section.to}
              className={cn(
                'flex shrink-0 items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
                active ? 'bg-primary text-primary-foreground shadow-soft' : 'text-muted-foreground hover:bg-accent hover:text-foreground',
              )}
            >
              <Icon className="h-4 w-4" />
              {section.label}
            </NavLink>
          );
        })}
      </nav>

      <header className="flex shrink-0 flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b border-border bg-background px-4 py-2.5 print:hidden">
        <div className="min-w-0">
          <h1 className="truncate text-lg font-semibold leading-tight text-foreground">{title}</h1>
          {subtitle && <p className="truncate text-xs text-muted-foreground">{subtitle}</p>}
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {subLinks?.map((link) => {
            const Icon = link.icon;
            const active = pathname === link.to;
            return (
              <Link
                key={link.to}
                to={link.to}
                className={cn(
                  'flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs font-medium transition-colors',
                  active ? 'border-primary/40 bg-primary/10 text-primary' : 'border-border text-muted-foreground hover:bg-accent hover:text-foreground',
                )}
              >
                {Icon && <Icon className="h-3.5 w-3.5" />}
                {link.label}
              </Link>
            );
          })}
          {actions}
        </div>
      </header>

      <div className={cn('flex min-h-0 flex-1 flex-col', flush ? '' : 'gap-4 p-4')}>{children}</div>
    </div>
  );
}

/** A function-key hint, as shown along the foot of the entry screens. */
export function KeyHint({ keys, children }: { keys: string; children: ReactNode }) {
  return (
    <span className="flex items-center gap-1.5 whitespace-nowrap">
      <kbd className="rounded border border-border bg-background px-1.5 py-0.5 font-mono text-[11px] font-medium text-foreground shadow-sm">{keys}</kbd>
      {children}
    </span>
  );
}
