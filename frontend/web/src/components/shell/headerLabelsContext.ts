import { createContext } from 'react';

/** Set inside the mobile header's More sheet, where there's room for every label regardless of
 * viewport width (the header row itself hides labels below xl). */
export const HeaderLabelsVisibleContext = createContext(false);
