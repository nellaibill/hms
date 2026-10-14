import { useBrandingQuery } from '../hooks/useBrandingQuery';
import { printPageCss, resolvePrintPageSetup } from '../printPageSetup';
import type { PrintDocument } from '../types';

/**
 * Applies the hospital's print settings (Theme & Branding → Print) as an `@page` rule.
 * AppLayout renders one with no `document`, so every printout gets the default page. A
 * printout with its own setting renders another one with its `document` key. That one comes
 * later in the page, so it wins, the same way the pharmacy counter bill's own `@page` does.
 */
export function PrintPageStyle({ document }: { document?: PrintDocument }) {
  const { data } = useBrandingQuery();
  return <style>{printPageCss(resolvePrintPageSetup(data?.printSettings, document))}</style>;
}
