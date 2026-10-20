/** A raster image ready for jsPDF's addImage, with its natural pixel size for aspect-ratio math. */
export interface PdfImage {
  dataUrl: string;
  width: number;
  height: number;
}

/** Longest edge, in pixels, an image is rasterized to — sharp at letterhead size, small enough to keep the PDF light. */
const MAX_EDGE_PX = 1200;

function readAsDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}

function decode(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error('Image failed to decode'));
    image.src = src;
  });
}

/**
 * Loads any browser-renderable image (PNG/JPEG/SVG/WebP — a logo upload can be any of them)
 * and re-encodes it as a PNG data URL, since jsPDF's addImage can't take SVG or WebP. Uploads
 * are served by the API on another origin; its CORS policy runs before UseStaticFiles, so a
 * CORS fetch can read them. The fetch skips the HTTP cache because the header's own <img> may
 * have cached the file from a non-CORS request, and the image is decoded from a data URL so
 * the canvas is never tainted. Resolves to null on any failure so a missing logo never blocks
 * the PDF itself.
 */
export async function loadImageForPdf(url: string): Promise<PdfImage | null> {
  try {
    const src = url.startsWith('data:') ? url : await fetch(url, { mode: 'cors', cache: 'no-store' }).then((response) => {
      if (!response.ok) throw new Error(`Image request failed: ${response.status}`);
      return response.blob().then(readAsDataUrl);
    });
    const image = await decode(src);
    const naturalWidth = image.naturalWidth || image.width;
    const naturalHeight = image.naturalHeight || image.height;
    if (!naturalWidth || !naturalHeight) return null;

    const scale = Math.min(1, MAX_EDGE_PX / Math.max(naturalWidth, naturalHeight));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(naturalWidth * scale);
    canvas.height = Math.round(naturalHeight * scale);
    const context = canvas.getContext('2d');
    if (!context) return null;
    context.drawImage(image, 0, 0, canvas.width, canvas.height);
    return { dataUrl: canvas.toDataURL('image/png'), width: canvas.width, height: canvas.height };
  } catch {
    return null;
  }
}
