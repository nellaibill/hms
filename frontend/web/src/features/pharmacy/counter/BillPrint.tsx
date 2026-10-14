import type { PharmacySettings, SaleResponse } from '@hms/shared';
import { branding } from '@/config/branding';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import { dateTimeLabel, expiryLabel, money, quantity, round2 } from '../common/format';
import { SCHEDULE_BADGE } from '../common/labels';
import { rupeesInWords } from '../common/words';

const PAYMENT_LABELS = { Cash: 'Cash', Card: 'Card', Upi: 'UPI', BankTransfer: 'Bank transfer' } as const;

interface BillPrintProps {
  sale: SaleResponse;
  settings: PharmacySettings | undefined;
  /** Marks a reprint, so a second copy is never mistaken for a second bill. */
  duplicate?: boolean;
}

/**
 * The printed pharmacy bill: a GST tax invoice on A5/A4, or a narrow receipt on an 80 mm
 * thermal roll, as the pharmacy's settings say. It carries what the law wants on a retail
 * medicine bill — seller's GSTIN and drug licence, batch and expiry on every line, and the
 * tax broken out by rate. Rendered inside a `.print-target`, so it prints alone.
 */
export function BillPrint({ sale, settings, duplicate }: BillPrintProps) {
  const { data: brand } = useBrandingQuery();
  const name = settings?.pharmacyName || brand?.hospitalName || branding.hospitalName;
  const address = settings?.address || brand?.address;
  const phone = settings?.phone || brand?.phoneNumber;
  const thermal = settings?.printFormat === 'Thermal80';
  const pageSize = thermal ? '80mm auto' : settings?.printFormat === 'A4' ? 'A4' : 'A5';

  // GST summary: one row per rate, split into the CGST and SGST halves a retail bill shows.
  const byRate = new Map<number, { taxable: number; tax: number }>();
  for (const line of sale.lines) {
    const row = byRate.get(line.gstRatePercent) ?? { taxable: 0, tax: 0 };
    row.taxable += line.taxableAmount;
    row.tax += line.taxAmount;
    byRate.set(line.gstRatePercent, row);
  }
  const taxRows = [...byRate.entries()].sort(([a], [b]) => a - b);
  const paid = round2(sale.paidAmount - sale.refundedAmount);

  return (
    <div className={thermal ? 'mx-auto w-[72mm] font-mono text-[10px] leading-tight text-black' : 'text-[11px] leading-snug text-black'}>
      <style>{`@media print { @page { size: ${pageSize}; margin: ${thermal ? '3mm' : '8mm'}; } }`}</style>

      <header className="border-b border-black pb-1.5 text-center">
        <p className={thermal ? 'text-sm font-bold' : 'text-base font-bold'}>{name}</p>
        {address && <p>{address}</p>}
        <p>
          {phone && <>Ph: {phone}</>}
          {phone && settings?.gstin ? ' · ' : ''}
          {settings?.gstin && <>GSTIN: {settings.gstin}</>}
        </p>
        {settings?.drugLicenceNo && <p>D.L. No: {settings.drugLicenceNo}</p>}
        <p className="mt-1 font-semibold uppercase tracking-wide">
          {settings?.gstin ? 'Tax invoice' : 'Pharmacy bill'}
          {duplicate ? ' (duplicate)' : ''}
        </p>
      </header>

      <section className={thermal ? 'border-b border-black py-1' : 'grid grid-cols-2 gap-x-4 border-b border-black py-1.5'}>
        <div>
          <p>
            <span className="font-semibold">Patient:</span> {sale.customerName}
            {sale.patientUhid ? ` (${sale.patientUhid})` : ''}
          </p>
          {sale.customerPhone && <p>Ph: {sale.customerPhone}</p>}
          {sale.doctorName && <p>Doctor: {sale.doctorName}</p>}
        </div>
        <div className={thermal ? '' : 'text-right'}>
          <p>
            <span className="font-semibold">Bill no:</span> {sale.billNo}
          </p>
          <p>{dateTimeLabel(sale.saleDate)}</p>
          {sale.chargedToAdmission && <p className="font-semibold">IP credit</p>}
        </div>
      </section>

      {thermal ? (
        <table className="w-full border-b border-black">
          <thead>
            <tr className="border-b border-dashed border-black text-left">
              <th className="py-0.5">Item</th>
              <th className="py-0.5 text-right">Qty</th>
              <th className="py-0.5 text-right">Rate</th>
              <th className="py-0.5 text-right">Amt</th>
            </tr>
          </thead>
          <tbody>
            {sale.lines.map((line) => (
              <tr key={line.id} className="align-top">
                <td className="py-0.5">
                  {line.productName}
                  <br />
                  <span className="text-[9px]">
                    {line.batchNo} · {expiryLabel(line.expiryDate)}
                    {line.discountPercent > 0 ? ` · -${quantity(line.discountPercent)}%` : ''}
                  </span>
                </td>
                <td className="py-0.5 text-right">{quantity(line.quantity)}</td>
                <td className="py-0.5 text-right">{money(line.unitPrice)}</td>
                <td className="py-0.5 text-right">{money(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : (
        <table className="w-full border-b border-black">
          <thead>
            <tr className="border-b border-black text-left">
              <th className="py-1 pr-1">#</th>
              <th className="py-1 pr-1">Item</th>
              <th className="py-1 pr-1">HSN</th>
              <th className="py-1 pr-1">Batch</th>
              <th className="py-1 pr-1">Exp</th>
              <th className="py-1 pr-1 text-right">Qty</th>
              <th className="py-1 pr-1 text-right">MRP</th>
              <th className="py-1 pr-1 text-right">Disc</th>
              <th className="py-1 pr-1 text-right">GST%</th>
              <th className="py-1 text-right">Amount</th>
            </tr>
          </thead>
          <tbody>
            {sale.lines.map((line) => (
              <tr key={line.id} className="align-top">
                <td className="py-0.5 pr-1">{line.lineNo}</td>
                <td className="py-0.5 pr-1">
                  {line.productName}
                  {SCHEDULE_BADGE[line.schedule] ? ` [${SCHEDULE_BADGE[line.schedule]!.label}]` : ''}
                </td>
                <td className="py-0.5 pr-1">{line.hsnCode ?? ''}</td>
                <td className="py-0.5 pr-1">{line.batchNo}</td>
                <td className="py-0.5 pr-1">{expiryLabel(line.expiryDate)}</td>
                <td className="py-0.5 pr-1 text-right">{quantity(line.quantity)}</td>
                <td className="py-0.5 pr-1 text-right">{money(line.unitPrice)}</td>
                <td className="py-0.5 pr-1 text-right">{line.discountAmount > 0 ? money(line.discountAmount) : ''}</td>
                <td className="py-0.5 pr-1 text-right">{quantity(line.gstRatePercent)}</td>
                <td className="py-0.5 text-right">{money(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <section className={thermal ? 'py-1' : 'grid grid-cols-[1fr_auto] gap-x-6 py-1.5'}>
        <div>
          {!thermal && taxRows.length > 0 && (
            <table className="text-[10px]">
              <thead>
                <tr className="text-left">
                  <th className="pr-3 font-semibold">GST %</th>
                  <th className="pr-3 text-right font-semibold">Taxable</th>
                  <th className="pr-3 text-right font-semibold">CGST</th>
                  <th className="text-right font-semibold">SGST</th>
                </tr>
              </thead>
              <tbody>
                {taxRows.map(([rate, row]) => (
                  <tr key={rate}>
                    <td className="pr-3">{quantity(rate)}%</td>
                    <td className="pr-3 text-right">{money(row.taxable)}</td>
                    <td className="pr-3 text-right">{money(row.tax / 2)}</td>
                    <td className="text-right">{money(row.tax / 2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <p className="mt-1">{rupeesInWords(sale.netAmount)}</p>
        </div>
        <table className={thermal ? 'w-full' : 'min-w-[44mm]'}>
          <tbody>
            <tr>
              <td>Total (MRP)</td>
              <td className="text-right">{money(sale.grossAmount)}</td>
            </tr>
            {sale.discountAmount > 0 && (
              <tr>
                <td>Discount</td>
                <td className="text-right">- {money(sale.discountAmount)}</td>
              </tr>
            )}
            <tr>
              <td>GST included</td>
              <td className="text-right">{money(sale.taxAmount)}</td>
            </tr>
            {sale.roundOff !== 0 && (
              <tr>
                <td>Round off</td>
                <td className="text-right">{money(sale.roundOff)}</td>
              </tr>
            )}
            <tr className={thermal ? 'border-y border-black text-sm font-bold' : 'border-y border-black text-sm font-bold'}>
              <td className="py-0.5">Net amount</td>
              <td className="py-0.5 text-right">₹{money(sale.netAmount)}</td>
            </tr>
            {sale.payments.map((payment) => (
              <tr key={payment.id}>
                <td>
                  {PAYMENT_LABELS[payment.method]}
                  {payment.referenceNo ? ` (${payment.referenceNo})` : ''}
                </td>
                <td className="text-right">{money(payment.amount)}</td>
              </tr>
            ))}
            {sale.returnedAmount > 0 && (
              <tr>
                <td>Returned</td>
                <td className="text-right">- {money(sale.returnedAmount)}</td>
              </tr>
            )}
            {sale.chargedToAdmission ? (
              <tr className="font-semibold">
                <td>On IP bill</td>
                <td className="text-right">{money(sale.netAmount - sale.returnedAmount - paid)}</td>
              </tr>
            ) : (
              sale.balanceDue > 0 && (
                <tr className="font-semibold">
                  <td>Balance due</td>
                  <td className="text-right">{money(sale.balanceDue)}</td>
                </tr>
              )
            )}
          </tbody>
        </table>
      </section>

      <footer className="border-t border-black pt-1 text-[10px]">
        {settings?.billFooter && <p>{settings.billFooter}</p>}
        <div className={thermal ? 'mt-2 text-center' : 'mt-1 flex items-end justify-between'}>
          <p>Prices include GST. Get well soon.</p>
          {!thermal && <p className="pt-6">Pharmacist's signature</p>}
        </div>
      </footer>
    </div>
  );
}
