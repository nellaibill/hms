import { Boxes, FileSpreadsheet, type LucideIcon, PackagePlus, ScrollText, Settings2, Truck } from 'lucide-react';

export interface PharmacySubLink {
  label: string;
  to: string;
  icon?: LucideIcon;
}

/** Second-level links shared by every screen in a section of the pharmacy menu. */
export const PURCHASE_LINKS: PharmacySubLink[] = [{ label: 'Purchase invoices', to: '/pharmacy/purchases', icon: PackagePlus }];

export const STOCK_LINKS: PharmacySubLink[] = [
  { label: 'Current stock', to: '/pharmacy', icon: Boxes },
  { label: 'Stock ledger', to: '/pharmacy/stock-ledger', icon: ScrollText },
];

export const SETUP_LINKS: PharmacySubLink[] = [
  { label: 'Setup', to: '/pharmacy/setup', icon: Settings2 },
  { label: 'Stores', to: '/pharmacy/stores', icon: Truck },
  { label: 'Import', to: '/pharmacy/import', icon: FileSpreadsheet },
];
