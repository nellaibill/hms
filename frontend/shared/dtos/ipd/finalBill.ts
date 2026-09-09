/** Mirrors HMS.Modules.IPD.Contracts.GenerateFinalBillResponse — just enough to link straight
 * to the real Invoice; the full invoice content is fetched from Billing's own API. */
export interface GenerateFinalBillResponse {
  invoiceId: string;
  invoiceNumber: string;
  totalAmount: number;
}
