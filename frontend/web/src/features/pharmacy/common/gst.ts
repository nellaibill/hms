import { round2 } from './format';

/**
 * The same GST arithmetic the server does (HMS.Modules.Pharmacy.Application.GstMath), so the
 * totals a pharmacist sees while entering a bill or a purchase are the ones that get saved.
 * The server's figures are the record; these are the preview.
 */
export interface TaxedAmount {
  gross: number;
  discount: number;
  taxable: number;
  tax: number;
  total: number;
}

/** Purchase side: the rate is before tax. Discount comes off first, then GST is added. */
export function taxExclusive(gross: number, discountPercent: number, gstRatePercent: number): TaxedAmount {
  const g = round2(gross);
  const discount = round2((g * discountPercent) / 100);
  const taxable = round2(g - discount);
  const tax = round2((taxable * gstRatePercent) / 100);
  return { gross: g, discount, taxable, tax, total: round2(taxable + tax) };
}

/** Sale side: MRP includes tax. Discount comes off, and the tax is the GST inside what is left. */
export function taxInclusive(gross: number, discountAmount: number, gstRatePercent: number): TaxedAmount {
  const g = round2(gross);
  const discount = Math.min(round2(discountAmount), g);
  const total = round2(g - discount);
  const taxable = round2(total / (1 + gstRatePercent / 100));
  return { gross: g, discount, taxable, tax: round2(total - taxable), total };
}

/** Bills settle in whole rupees; the paise are shown as round-off. */
export function roundToRupee(total: number): { roundOff: number; net: number } {
  const net = Math.round(total + Number.EPSILON);
  return { roundOff: round2(net - total), net };
}
