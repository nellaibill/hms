interface TabErrorSummaryProps {
  messages: string[];
}

/**
 * Bulleted list of every validation error on the current tab, shown once near the top of the
 * tab alongside the existing per-field inline messages — a tab's red-dot indicator says
 * *something* is wrong but not what, and on a long tab (Medical Information, Registration
 * Details) that means scrolling to hunt for it field by field. This surfaces all of it at once.
 */
export function TabErrorSummary({ messages }: TabErrorSummaryProps) {
  if (messages.length === 0) {
    return null;
  }

  return (
    <div role="alert" className="mb-4 rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
      <p className="font-medium">Please fix the following before continuing:</p>
      <ul className="mt-1 list-inside list-disc">
        {messages.map((message) => (
          <li key={message}>{message}</li>
        ))}
      </ul>
    </div>
  );
}
