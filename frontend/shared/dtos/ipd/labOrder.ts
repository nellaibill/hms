/** Mirrors HMS.Modules.IPD.Contracts.PlaceLabOrderLineRequest — exactly one of
 * serviceId/packageId is expected per line. */
export interface PlaceLabOrderLineRequest {
  serviceId?: string | null;
  packageId?: string | null;
}

/** Mirrors HMS.Modules.IPD.Contracts.CreatePlaceLabOrderRequest — places a real
 * HMS.Modules.Laboratory LabOrder directly against an admission, no invoice required. */
export interface CreatePlaceLabOrderRequest {
  lines: PlaceLabOrderLineRequest[];
}
