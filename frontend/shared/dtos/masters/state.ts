/** Mirrors HMS.Modules.Masters.Contracts.StateResponse. Read-only reference data — India is
 * the only supported country, so there is no Country type above this. */
export interface State {
  id: string;
  name: string;
}

/** Mirrors HMS.Modules.Masters.Contracts.DistrictResponse. */
export interface District {
  id: string;
  name: string;
  stateId: string;
}
