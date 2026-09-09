/** Mirrors HMS.Modules.DischargeSummary.Contracts.DischargeSummaryEnums — serialized as strings (JsonStringEnumConverter). */
export const DISCHARGE_SUMMARY_STATUSES = ['Draft', 'Finalized'] as const;
export type DischargeSummaryStatus = (typeof DISCHARGE_SUMMARY_STATUSES)[number];

/** Whether a discharge medication line is taken before or after food. */
export const FOOD_INSTRUCTIONS = ['BeforeFood', 'AfterFood'] as const;
export type FoodInstruction = (typeof FOOD_INSTRUCTIONS)[number];
