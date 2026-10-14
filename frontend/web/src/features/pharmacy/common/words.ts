const ONES = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten', 'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen', 'Seventeen', 'Eighteen', 'Nineteen'];
const TENS = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];

function belowThousand(value: number): string {
  const hundreds = Math.floor(value / 100);
  const rest = value % 100;
  const words = rest < 20 ? ONES[rest] : `${TENS[Math.floor(rest / 10)]}${rest % 10 ? ` ${ONES[rest % 10]}` : ''}`;
  return [hundreds ? `${ONES[hundreds]} Hundred` : '', words].filter(Boolean).join(' ');
}

/** A whole-rupee amount in words, in the Indian system (lakh, crore) — as bills state their total. */
export function rupeesInWords(amount: number): string {
  let value = Math.round(Math.abs(amount));
  if (value === 0) return 'Zero rupees only';

  const parts: string[] = [];
  const crore = Math.floor(value / 10_000_000);
  value %= 10_000_000;
  const lakh = Math.floor(value / 100_000);
  value %= 100_000;
  const thousand = Math.floor(value / 1000);
  value %= 1000;

  if (crore) parts.push(`${belowThousand(crore)} Crore`);
  if (lakh) parts.push(`${belowThousand(lakh)} Lakh`);
  if (thousand) parts.push(`${belowThousand(thousand)} Thousand`);
  if (value) parts.push(belowThousand(value));
  return `Rupees ${parts.join(' ')} only`;
}
