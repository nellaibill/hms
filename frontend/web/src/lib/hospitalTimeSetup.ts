import { installHospitalTimeDisplay } from './hospitalTime';

// Imported first in main.tsx, so it runs before any other module's top-level code can format a
// date: every date and time on screen is India Standard Time, whatever the PC's zone (ADR-147).
installHospitalTimeDisplay();
