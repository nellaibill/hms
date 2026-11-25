// The hospital clock (ADR-147) lives in @hms/shared, so the web app, the shared form validation
// and the mobile app hold one copy of the server's clock. Re-exported for the web's own imports.
export {
  HOSPITAL_TIME_ZONE,
  addDaysToDate,
  fromHospitalDateTimeInput,
  hospitalDateOf,
  hospitalDateParts,
  hospitalDayEnd,
  hospitalDayStart,
  hospitalToday,
  installHospitalTimeDisplay,
  serverClockOffsetMs,
  serverNow,
  syncServerClock,
  toHospitalDateTimeInput,
  yearsBeforeDate,
} from '@hms/shared';
