import { Boxes, FileSpreadsheet, type LucideIcon, PackagePlus, ReceiptText, ScrollText, Settings2, SlidersHorizontal, Truck, Undo2 } from 'lucide-react';

export interface PharmacySubLink {
  label: string;
  to: string;
  icon?: LucideIcon;
}

/** Second-level links shared by every screen in a section of the pharmacy menu. */
export const SALES_LINKS: PharmacySubLink[] = [
  { label: 'Billing counter', to: '/pharmacy/counter', icon: ReceiptText },
  { label: 'Bills and returns', to: '/pharmacy/sales', icon: Undo2 },
];

export const PURCHASE_LINKS: PharmacySubLink[] = [{ label: 'Purchase invoices', to: '/pharmacy/purchases', icon: PackagePlus }];

export const STOCK_LINKS: PharmacySubLink[] = [
  { label: 'Current stock', to: '/pharmacy', icon: Boxes },
  { label: 'Stock ledger', to: '/pharmacy/stock-ledger', icon: ScrollText },
];

export const SETUP_LINKS: PharmacySubLink[] = [
  { label: 'Setup', to: '/pharmacy/setup', icon: Settings2 },
  { label: 'Bill settings', to: '/pharmacy/settings', icon: SlidersHorizontal },
  { label: 'Stores', to: '/pharmacy/stores', icon: Truck },
  { label: 'Import', to: '/pharmacy/import', icon: FileSpreadsheet },
];
