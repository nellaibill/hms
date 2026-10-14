import type { BillPrintFormat } from '../../enums';

/** Mirrors HMS.Modules.Pharmacy.Contracts.PharmacySettingsResponse — the particulars printed on
 * every pharmacy bill, and the counter's rules. */
export interface PharmacySettings {
  /** Printed at the head of the bill. Null prints the hospital's name. */
  pharmacyName?: string | null;
  gstin?: string | null;
  drugLicenceNo?: string | null;
  address?: string | null;
  phone?: string | null;
  billFooter?: string | null;
  printFormat: BillPrintFormat;
  /** The most discount the counter may give. 100 is "no limit". */
  maxDiscountPercent: number;
  /** How many days ahead a batch counts as expiring soon. */
  expiryAlertDays: number;
}

export type UpdatePharmacySettingsRequest = PharmacySettings;
