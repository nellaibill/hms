import { Boxes, ClipboardList, FileSpreadsheet, type LucideIcon, ListChecks, PackagePlus, ReceiptText, ScrollText, Settings2, SlidersHorizontal, Truck, Undo2, Wallet } from 'lucide-react';

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

export const PURCHASE_LINKS: PharmacySubLink[] = [
  { label: 'Invoices', to: '/pharmacy/purchases', icon: PackagePlus },
  { label: 'Reorder', to: '/pharmacy/reorder', icon: ListChecks },
  { label: 'Orders', to: '/pharmacy/purchase-orders', icon: ClipboardList },
  { label: 'Returns', to: '/pharmacy/purchase-returns', icon: Undo2 },
  { label: 'Supplier accounts', to: '/pharmacy/suppliers', icon: Wallet },
];

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
