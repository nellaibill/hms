/** Masks all but the last 4 digits of an ID proof number for display, e.g. "234567893210" ->
 * "XXXX XXXX 3210" — the full number is still in the API response, just not shown on screen. */
export function maskIdNumber(value: string): string {
  const digits = value.replace(/\s+/g, '');
  if (digits.length <= 4) return value;

  const last4 = digits.slice(-4);
  const masked = 'X'.repeat(digits.length - 4).match(/.{1,4}/g)?.join(' ') ?? '';
  return `${masked} ${last4}`.trim();
}
