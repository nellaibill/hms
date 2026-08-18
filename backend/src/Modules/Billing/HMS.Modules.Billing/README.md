# HMS.Modules.Billing

Billing module — invoices, line items, and payment records.

Layout: `Domain/`, `Application/`, `Infrastructure/`, `Contracts/`, `Endpoints/`.

A unified invoice engine: one `Invoice` (header) plus `InvoiceLineItem`s carrying a
`BillingType` category (Consultation/Radiology/Laboratory/Procedure), rather than four
separate near-duplicate billing tables — see docs/DecisionLog.md's Billing ADR for why, and
docs/BusinessRequirementsAnalysis.md for the original "four parallel blocks" risk this
resolves. `Payment` records the real collection trail (amount, method, who, when) that the
frontend's earlier mock store never had.
