import type { AdjustmentReason, DrugSchedule, IndentStatus, PurchaseOrderStatus, PurchaseReturnReason, SalePaymentMethod, StockCountStatus, StockSourceType, SupplierPaymentMethod, TransactionType } from '@hms/shared';

/** How each kind of stock movement reads on screen, and which way it moves stock. */
export const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  Receipt: 'Purchase',
  Dispense: 'Dispense',
  OpeningStock: 'Opening stock',
  PurchaseReturn: 'Purchase return',
  Sale: 'Sale',
  SaleReturn: 'Sale return',
  TransferOut: 'Transfer out',
  TransferIn: 'Transfer in',
  AdjustmentIn: 'Adjustment (+)',
  AdjustmentOut: 'Adjustment (−)',
};

const INWARD: ReadonlySet<TransactionType> = new Set(['Receipt', 'OpeningStock', 'SaleReturn', 'TransferIn', 'AdjustmentIn']);

export function isInward(type: TransactionType): boolean {
  return INWARD.has(type);
}

/** Where a movement's source document opens, when it has a screen of its own. */
export function sourceDocumentPath(sourceType: StockSourceType | null | undefined, sourceId: string | null | undefined): string | null {
  if (!sourceType || !sourceId) return null;
  switch (sourceType) {
    case 'GoodsReceipt':
      return `/pharmacy/purchases/${sourceId}`;
    case 'Sale':
      return `/pharmacy/sales/${sourceId}`;
    case 'StockCount':
      return `/pharmacy/stock-counts/${sourceId}`;
    default:
      return null;
  }
}

/** Schedules that need a prescription, and so a doctor's name on the bill. */
export const PRESCRIPTION_SCHEDULES: ReadonlySet<DrugSchedule> = new Set(['H', 'H1', 'X', 'Narcotic']);

/** Schedules whose every sale goes into a statutory register. */
export const REGISTER_SCHEDULES: ReadonlySet<DrugSchedule> = new Set(['H1', 'X', 'Narcotic']);

export const SCHEDULE_BADGE: Record<DrugSchedule, { label: string; className: string } | null> = {
  None: null,
  G: { label: 'G', className: 'bg-muted text-muted-foreground' },
  H: { label: 'H', className: 'bg-warning/15 text-warning' },
  H1: { label: 'H1', className: 'bg-destructive/15 text-destructive' },
  X: { label: 'X', className: 'bg-destructive/15 text-destructive' },
  Narcotic: { label: 'NDPS', className: 'bg-destructive/15 text-destructive' },
};

type BadgeVariant = 'success' | 'warning' | 'destructive' | 'secondary';

export const ORDER_STATUS: Record<PurchaseOrderStatus, { label: string; variant: BadgeVariant }> = {
  Draft: { label: 'Draft', variant: 'secondary' },
  Ordered: { label: 'Ordered', variant: 'warning' },
  PartlyReceived: { label: 'Partly received', variant: 'warning' },
  Received: { label: 'Received', variant: 'success' },
  Cancelled: { label: 'Cancelled', variant: 'destructive' },
};

export const RETURN_REASON_LABELS: Record<PurchaseReturnReason, string> = {
  Expired: 'Expired',
  NearExpiry: 'Near expiry',
  Damaged: 'Damaged',
  Excess: 'Excess stock',
  WrongItem: 'Wrong item supplied',
  RateDifference: 'Rate difference',
  Other: 'Other',
};

export const PAYMENT_METHOD_LABELS: Record<SalePaymentMethod, string> = {
  Cash: 'Cash',
  Card: 'Card',
  Upi: 'UPI',
  BankTransfer: 'Bank transfer',
};

export const SUPPLIER_PAYMENT_LABELS: Record<SupplierPaymentMethod, string> = {
  Cash: 'Cash',
  Cheque: 'Cheque',
  BankTransfer: 'Bank transfer',
  Upi: 'UPI',
};

export const ADJUSTMENT_REASON_LABELS: Record<AdjustmentReason, string> = {
  Expired: 'Expired',
  Damaged: 'Damaged or broken',
  Shortage: 'Shortage',
  Excess: 'Excess found',
  FreeIssue: 'Free issue',
  StockCount: 'Stock count',
  Correction: 'Correction',
  Other: 'Other',
};

export const COUNT_STATUS: Record<StockCountStatus, { label: string; variant: BadgeVariant }> = {
  Open: { label: 'Counting', variant: 'warning' },
  Posted: { label: 'Posted', variant: 'success' },
  Cancelled: { label: 'Cancelled', variant: 'secondary' },
};

export const INDENT_STATUS: Record<IndentStatus, { label: string; variant: BadgeVariant }> = {
  Pending: { label: 'To issue', variant: 'warning' },
  PartlyIssued: { label: 'Partly issued', variant: 'warning' },
  Issued: { label: 'Issued', variant: 'success' },
  Cancelled: { label: 'Cancelled', variant: 'secondary' },
};
