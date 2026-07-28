import { Stethoscope } from 'lucide-react';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import { doctorAvailability, type DoctorAvailabilityRow } from '../mockData';

const statusDot: Record<DoctorAvailabilityRow['status'], string> = {
  Available: 'bg-success',
  'In Consultation': 'bg-warning',
  'On Leave': 'bg-muted-foreground/40',
};

function initialsOf(name: string) {
  return name
    .replace('Dr. ', '')
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();
}

export function DoctorAvailabilityCard() {
  return (
    <Card className="transition-shadow hover:shadow-soft-lg">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <Stethoscope className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Doctor Availability</CardTitle>
          <CardDescription className="mt-0.5">Live consultant status</CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col divide-y divide-border pt-0">
        {doctorAvailability.map((doctor) => (
          <div key={doctor.name} className="flex items-center gap-3 py-2.5 first:pt-0 last:pb-0">
            <Avatar className="h-9 w-9">
              <AvatarFallback className="bg-secondary text-xs">{initialsOf(doctor.name)}</AvatarFallback>
            </Avatar>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-foreground">{doctor.name}</p>
              <p className="truncate text-xs text-muted-foreground">{doctor.specialty}</p>
            </div>
            <span className="flex items-center gap-1.5 whitespace-nowrap text-xs text-muted-foreground">
              <span className={cn('h-2 w-2 shrink-0 rounded-full', statusDot[doctor.status])} />
              {doctor.status}
            </span>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
