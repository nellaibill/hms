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

/** Converts an "H S% L%" triple to a "#rrggbb" hex string — for <input type="color"> in the Theme & Branding form. */
export function hslTripleToHex(triple: string): string {
  const { h, s, l } = parseHsl(triple);
  const sNorm = s / 100;
  const lNorm = l / 100;
  const c = (1 - Math.abs(2 * lNorm - 1)) * sNorm;
  const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
  const m = lNorm - c / 2;

  let r = 0;
  let g = 0;
  let b = 0;
  if (h < 60) [r, g, b] = [c, x, 0];
  else if (h < 120) [r, g, b] = [x, c, 0];
  else if (h < 180) [r, g, b] = [0, c, x];
  else if (h < 240) [r, g, b] = [0, x, c];
  else if (h < 300) [r, g, b] = [x, 0, c];
  else [r, g, b] = [c, 0, x];

  const toHex = (channel: number) =>
    Math.round((channel + m) * 255)
      .toString(16)
      .padStart(2, '0');

  return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

/** Converts a "#rrggbb" hex string to an "H S% L%" triple — the inverse of hslTripleToHex. */
export function hexToHslTriple(hex: string): string {
  const normalized = hex.replace('#', '');
  const r = Number.parseInt(normalized.slice(0, 2), 16) / 255;
  const g = Number.parseInt(normalized.slice(2, 4), 16) / 255;
  const b = Number.parseInt(normalized.slice(4, 6), 16) / 255;

  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const l = (max + min) / 2;
  const delta = max - min;

  let h = 0;
  let s = 0;
  if (delta !== 0) {
    s = delta / (1 - Math.abs(2 * l - 1));
    switch (max) {
      case r:
        h = 60 * (((g - b) / delta) % 6);
        break;
      case g:
        h = 60 * ((b - r) / delta + 2);
        break;
      default:
        h = 60 * ((r - g) / delta + 4);
    }
  }
  if (h < 0) h += 360;

  return toHslString({ h: Math.round(h), s: Math.round(s * 100), l: Math.round(l * 100) });
}
