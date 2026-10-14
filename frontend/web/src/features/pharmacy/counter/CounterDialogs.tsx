import type { CounterItemResponse, HeldBillResponse, SaleResponse } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Loader2, Printer, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { pharmacyApi } from '@/services/apiClient';
import { dateTimeLabel, expiryLabel, money, quantity } from '../common/format';

interface SubstitutesDialogProps {
  /** The item to find substitutes for; null keeps the dialog closed. */
  item: CounterItemResponse | null;
  storeId: string;
  onChoose: (substitute: CounterItemResponse) => void;
  onClose: () => void;
}

/** In-stock products with the same generic as an item the store cannot supply. */
export function SubstitutesDialog({ item, storeId, onChoose, onClose }: SubstitutesDialogProps) {
  const { data, isPending } = useQuery({
    queryKey: ['pharmacy', 'counter', 'substitutes', item?.productId, storeId],
    queryFn: () => pharmacyApi.getSubstitutes(item!.productId, storeId || undefined),
    enabled: Boolean(item),
  });

  return (
    <Dialog open={Boolean(item)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Substitutes for {item?.productName}</DialogTitle>
          <DialogDescription>{item?.genericName ? `Same composition: ${item.genericName}` : 'This product has no generic name recorded, so substitutes cannot be matched.'}</DialogDescription>
        </DialogHeader>

        {isPending && item && (
          <p className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Looking for substitutes…
          </p>
        )}

        {data && data.length === 0 && <p className="py-6 text-sm text-muted-foreground">Nothing with the same composition is in stock.</p>}

        {data && data.length > 0 && (
          <ul className="max-h-80 divide-y divide-border overflow-y-auto rounded-md border border-border">
            {data.map((substitute) => (
              <li key={substitute.productId}>
                <button type="button" onClick={() => onChoose(substitute)} className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm hover:bg-accent">
                  <span className="min-w-0">
                    <span className="block font-medium text-foreground">{substitute.productName}</span>
                    <span className="block text-xs text-muted-foreground">
                      {quantity(substitute.availableQuantity)} in stock
                      {substitute.batches[0] ? ` · exp ${expiryLabel(substitute.batches[0].expiryDate)}` : ''}
                      {substitute.rackLocation ? ` · rack ${substitute.rackLocation}` : ''}
                    </span>
                  </span>
                  <span className="shrink-0 tabular-nums text-foreground">₹{money(substitute.unitPrice)} / unit</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </DialogContent>
    </Dialog>
  );
}

interface HeldBillsDialogProps {
  open: boolean;
  bills: HeldBillResponse[];
  onRecall: (bill: HeldBillResponse) => void;
  onDiscard: (bill: HeldBillResponse) => void;
  onClose: () => void;
}

/** Bills parked at this counter, to pick up again or throw away. */
export function HeldBillsDialog({ open, bills, onRecall, onDiscard, onClose }: HeldBillsDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Held bills</DialogTitle>
          <DialogDescription>Recalling a bill checks every item against stock as it is now.</DialogDescription>
        </DialogHeader>

        {bills.length === 0 ? (
          <p className="py-6 text-sm text-muted-foreground">No bills are on hold.</p>
        ) : (
          <ul className="max-h-80 divide-y divide-border overflow-y-auto rounded-md border border-border">
            {bills.map((bill) => (
              <li key={bill.id} className="flex items-center gap-2 px-3 py-2 text-sm">
                <button type="button" onClick={() => onRecall(bill)} className="flex min-w-0 flex-1 items-center justify-between gap-3 text-left hover:text-primary">
                  <span className="min-w-0">
                    <span className="block truncate font-medium">{bill.label}</span>
                    <span className="block text-xs text-muted-foreground">
                      {bill.itemCount} {bill.itemCount === 1 ? 'item' : 'items'} · held {dateTimeLabel(bill.createdAt)}
                    </span>
                  </span>
                  <span className="shrink-0 tabular-nums">₹{money(bill.amount)}</span>
                </button>
                <button type="button" onClick={() => onDiscard(bill)} aria-label={`Discard held bill for ${bill.label}`} className="rounded p-1.5 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                  <Trash2 className="h-4 w-4" />
                </button>
              </li>
            ))}
          </ul>
        )}
      </DialogContent>
    </Dialog>
  );
}

interface BillSavedDialogProps {
  /** The bill just made; null keeps the dialog closed. */
  sale: SaleResponse | null;
  onPrint: () => void;
  onNewBill: () => void;
}

/** Shown the moment a bill is saved: the number, and above all the change to hand back. */
export function BillSavedDialog({ sale, onPrint, onNewBill }: BillSavedDialogProps) {
  return (
    <Dialog open={Boolean(sale)} onOpenChange={(open) => !open && onNewBill()}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Bill {sale?.billNo} saved</DialogTitle>
          <DialogDescription>{sale?.customerName}</DialogDescription>
        </DialogHeader>

        {sale && (
          <dl className="text-sm">
            <div className="flex justify-between py-0.5">
              <dt className="text-muted-foreground">Bill amount</dt>
              <dd className="tabular-nums">₹{money(sale.netAmount)}</dd>
            </div>
            <div className="flex justify-between py-0.5">
              <dt className="text-muted-foreground">Received</dt>
              <dd className="tabular-nums">₹{money(sale.paidAmount + sale.changeGiven)}</dd>
            </div>
            {sale.chargedToAdmission && (
              <div className="flex justify-between py-0.5">
                <dt className="text-muted-foreground">Charged to admission</dt>
                <dd className="tabular-nums">₹{money(sale.netAmount - sale.paidAmount)}</dd>
              </div>
            )}
            {sale.balanceDue > 0 && (
              <div className="flex justify-between py-0.5 font-medium text-warning">
                <dt>Balance due</dt>
                <dd className="tabular-nums">₹{money(sale.balanceDue)}</dd>
              </div>
            )}
            <div className="mt-2 flex items-baseline justify-between rounded-md bg-success/10 px-3 py-2">
              <dt className="font-medium text-foreground">Change to return</dt>
              <dd className="text-2xl font-semibold tabular-nums text-success">₹{money(sale.changeGiven)}</dd>
            </div>
          </dl>
        )}

        <DialogFooter className="gap-2 sm:gap-2">
          <Button variant="outline" onClick={onNewBill}>
            New bill
          </Button>
          <Button onClick={onPrint} autoFocus>
            <Printer className="h-4 w-4" />
            Print bill
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
