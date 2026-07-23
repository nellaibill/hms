/** Small HSL-triple helpers so brand tints (hover/active backgrounds, rings) can
 * be derived from a single configured primary color instead of hand-picked
 * per shade — see lib/apply-branding.ts. */

export interface HslTriple {
  h: number;
  s: number;
  l: number;
}

/** Parses "H S% L%" (the format every color token in index.css uses). */
export function parseHsl(triple: string): HslTriple {
  const [h, s, l] = triple
    .trim()
    .split(/\s+/)
    .map((part) => Number.parseFloat(part));
  return { h, s, l };
}

export function toHslString({ h, s, l }: HslTriple): string {
  return `${h} ${s}% ${l}%`;
}

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

/** Moves lightness toward 100 (tints) or 0 (shades) by `amount` percentage points. */
export function adjustLightness(triple: string, amount: number): string {
  const hsl = parseHsl(triple);
  return toHslString({ ...hsl, l: clamp(hsl.l + amount, 0, 100) });
}

/**
 * Sets lightness to an absolute value regardless of the input's current
 * lightness. Used for accessibility-critical pairs (e.g. sidebar active
 * text/background) where a *relative* offset can't guarantee a minimum
 * contrast ratio — saturated hues need a much bigger lightness gap than
 * grayscale to hit the same perceptual contrast, so a fixed target is more
 * reliable than "primary's lightness ± N".
 */
export function setLightness(triple: string, targetL: number): string {
  const hsl = parseHsl(triple);
  return toHslString({ ...hsl, l: clamp(targetL, 0, 100) });
}

export function withSaturation(triple: string, saturation: number): string {
  const hsl = parseHsl(triple);
  return toHslString({ ...hsl, s: clamp(saturation, 0, 100) });
}

/** Picks readable foreground text (near-black or near-white) for a given HSL background. */
export function contrastForeground(triple: string, darkText: string, lightText: string): string {
  const { l } = parseHsl(triple);
  return l > 55 ? darkText : lightText;
}
