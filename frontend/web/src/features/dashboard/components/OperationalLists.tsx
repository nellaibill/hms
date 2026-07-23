import { Badge } from '@/components/ui/badge';
import type {
  AmbulanceRow,
  AppointmentRow,
  LabPendingRow,
  OtScheduleRow,
  PharmacyRow,
  RadiologyRow,
} from '../mockData';

function Row({ primary, secondary, trailing }: { primary: string; secondary: string; trailing?: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-3 py-2 first:pt-0 last:pb-0">
      <div className="min-w-0">
        <p className="truncate text-sm font-medium text-foreground">{primary}</p>
        <p className="truncate text-xs text-muted-foreground">{secondary}</p>
      </div>
      {trailing}
    </div>
  );
}

export function AppointmentsList({ rows }: { rows: AppointmentRow[] }) {
  return (
    <>
      {rows.map((row) => (
        <Row key={row.time + row.patient} primary={row.patient} secondary={`${row.doctor} · ${row.type}`} trailing={<span className="text-xs font-medium tabular-nums text-muted-foreground">{row.time}</span>} />
      ))}
    </>
  );
}

const priorityVariant = { Routine: 'secondary', Urgent: 'warning', Critical: 'destructive' } as const;

export function LabPendingList({ rows }: { rows: LabPendingRow[] }) {
  return (
    <>
      {rows.map((row) => (
        <Row
          key={row.test + row.patient}
          primary={row.test}
          secondary={row.patient}
          trailing={
            <Badge variant={priorityVariant[row.priority]} className="text-[10px]">
              {row.priority}
            </Badge>
          }
        />
      ))}
    </>
  );
}

export function RadiologyQueueList({ rows }: { rows: RadiologyRow[] }) {
  return (
    <>
      {rows.map((row) => (
        <Row
          key={row.study + row.patient}
          primary={row.study}
          secondary={row.patient}
          trailing={<Badge variant="outline" className="text-[10px]">{row.modality}</Badge>}
        />
      ))}
    </>
  );
}

export function OtScheduleList({ rows }: { rows: OtScheduleRow[] }) {
  return (
    <>
      {rows.map((row) => (
        <Row key={row.time + row.procedure} primary={row.procedure} secondary={`${row.surgeon} · ${row.room}`} trailing={<span className="text-xs font-medium tabular-nums text-muted-foreground">{row.time}</span>} />
      ))}
    </>
  );
}

const ambulanceVariant = { Available: 'success', 'On Trip': 'default', Returning: 'warning' } as const;

export function AmbulanceStatusList({ rows }: { rows: AmbulanceRow[] }) {
  return (
    <>
      {rows.map((row) => (
        <Row
          key={row.vehicle}
          primary={row.vehicle}
          secondary={row.location}
          trailing={
            <Badge variant={ambulanceVariant[row.status]} className="text-[10px]">
              {row.status}
            </Badge>
          }
        />
      ))}
    </>
  );
}

export function PharmacyQueueList({ rows }: { rows: PharmacyRow[] }) {
  return (
    <>
      {rows.map((row) => (
        <Row key={row.rx} primary={row.rx} secondary={row.patient} trailing={<span className="text-xs text-muted-foreground">{row.items} items</span>} />
      ))}
    </>
  );
}
