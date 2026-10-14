import type { CounterBatchResponse, CounterItemResponse } from '@hms/shared';
import { round2 } from '../common/format';
import { roundToRupee, taxInclusive } from '../common/gst';

/** A registered patient chosen at the counter. */
export interface CounterPatient {
  id: string;
  name: string;
  uhid: string;
  phone?: string;
  allergies: string[];
}

/** One line of the bill being built. Quantity and discount stay as typed until the bill is saved. */
export interface CartLine {
  key: string;
  item: CounterItemResponse;
  /** '' takes the earliest-expiring stock, across batches if one cannot cover the quantity. */
  batchId: string;
  quantity: string;
  /** '' uses the bill's discount. */
  discount: string;
}

export interface LinePreview {
  quantity: number;
  /** The batches this line will be taken from, and how much from each. */
  allocations: { batch: CounterBatchResponse; quantity: number }[];
  unitPrice: number;
  gross: number;
  discount: number;
  taxable: number;
  tax: number;
  total: number;
  /** Units asked for beyond what the store can sell. A bill cannot be made while any line is short. */
  shortBy: number;
}

export interface BillPreview {
  lines: LinePreview[];
  gross: number;
  discount: number;
  taxable: number;
  tax: number;
  roundOff: number;
  net: number;
  hasShortLine: boolean;
}

/**
 * Reads a quantity as a pharmacist types it: a plain number is units ("15" tablets), and a
 * number followed by s, p or * is whole packs ("2s" = two strips), turned into units by the
 * product's pack size.
 */
export function parseQuantity(text: string, unitsPerPack: number): number {
  const value = text.trim().toLowerCase();
  const packs = /^(\d+(?:\.\d+)?)\s*(s|p|\*|strip|strips|pack|packs)$/.exec(value);
  if (packs) return Number(packs[1]) * (unitsPerPack > 0 ? unitsPerPack : 1);
  const units = Number.parseFloat(value);
  return Number.isFinite(units) && units > 0 ? units : 0;
}

/** "2 strips + 5" for 25 tablets of a strip of 10 — how the quantity reads on the shelf. */
export function packBreakdown(quantity: number, unitsPerPack: number): string | null {
  if (unitsPerPack <= 1 || quantity <= 0) return null;
  const packs = Math.floor(quantity / unitsPerPack);
  const loose = round2(quantity - packs * unitsPerPack);
  if (packs === 0) return null;
  return loose > 0 ? `${packs} × ${unitsPerPack} + ${loose}` : `${packs} × ${unitsPerPack}`;
}

/**
 * Works a line out the way the server will (SalePlanner): earliest-expiring stock first,
 * each batch at its own price, discount off the MRP amount, GST found inside what is left.
 */
export function previewLine(line: CartLine, billDiscountPercent: number): LinePreview {
  const quantity = parseQuantity(line.quantity, line.item.unitsPerPack);
  const candidates = line.batchId ? line.item.batches.filter((batch) => batch.productBatchId === line.batchId) : line.item.batches;
  const discountPercent = line.discount.trim() === '' ? billDiscountPercent : Math.min(Math.max(Number.parseFloat(line.discount) || 0, 0), 100);

  const allocations: LinePreview['allocations'] = [];
  let needed = quantity;
  let gross = 0;
  let discount = 0;
  let taxable = 0;
  let tax = 0;

  for (const batch of candidates) {
    if (needed <= 0) break;
    const take = Math.min(needed, batch.quantityOnHand);
    if (take <= 0) continue;
    needed = round2(needed - take);
    allocations.push({ batch, quantity: take });

    const batchGross = take * batch.unitPrice;
    const amount = taxInclusive(batchGross, (batchGross * discountPercent) / 100, line.item.gstRatePercent);
    gross += amount.gross;
    discount += amount.discount;
    taxable += amount.taxable;
    tax += amount.tax;
  }

  return {
    quantity,
    allocations,
    unitPrice: allocations[0]?.batch.unitPrice ?? candidates[0]?.unitPrice ?? line.item.unitPrice,
    gross: round2(gross),
    discount: round2(discount),
    taxable: round2(taxable),
    tax: round2(tax),
    total: round2(taxable + tax),
    shortBy: Math.max(needed, 0),
  };
}

export function previewBill(lines: CartLine[], billDiscountPercent: number): BillPreview {
  const previews = lines.map((line) => previewLine(line, billDiscountPercent));
  const sum = (pick: (preview: LinePreview) => number) => round2(previews.reduce((total, preview) => total + pick(preview), 0));

  const taxable = sum((preview) => preview.taxable);
  const tax = sum((preview) => preview.tax);
  const { roundOff, net } = roundToRupee(round2(taxable + tax));

  return {
    lines: previews,
    gross: sum((preview) => preview.gross),
    discount: sum((preview) => preview.discount),
    taxable,
    tax,
    roundOff,
    net,
    hasShortLine: previews.some((preview) => preview.shortBy > 0 || preview.quantity <= 0),
  };
}

/** What a held bill stores: enough to rebuild the cart against fresh stock when recalled. */
export interface HeldCart {
  version: 1;
  patient: CounterPatient | null;
  customerName: string;
  customerPhone: string;
  doctorName: string;
  admissionId: string;
  discount: string;
  lines: { productId: string; batchId: string; quantity: string; discount: string }[];
}
