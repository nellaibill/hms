import { AlertTriangle } from 'lucide-react';

/** Shown wherever a receptionist works with a patient whose record still has
 * placeholder data (Patient.requiresDataVerification) — bulk-imported patients routinely have
 * a synthetic phone number, a sentinel date of birth, "Unknown" for missing name/address
 * fields, etc. Saving any edit through the Edit Patient screen clears the flag automatically,
 * so this disappears the next time the page loads once someone's fixed it up. */
export function DataVerificationBanner() {
  return (
    <div className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2.5 text-sm text-foreground">
      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
      <p>
        <strong>This patient's record was imported from legacy data</strong> and may contain placeholder details (e.g.
        date of birth, phone number, address, or emergency contact). Please confirm these with the patient and update
        the record if anything's wrong — this message won't show again once you save an edit.
      </p>
    </div>
  );
}
