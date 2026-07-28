import { BedDouble } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ProgressBar } from './ProgressBar';
import { wardOccupancy } from '../mockData';

function OccupancyRing({ pct }: { pct: number }) {
  const size = 96;
  const stroke = 9;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference * (1 - pct / 100);

  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} className="-rotate-90">
      <circle cx={size / 2} cy={size / 2} r={radius} fill="none" stroke="hsl(var(--muted))" strokeWidth={stroke} />
      <circle
        cx={size / 2}
        cy={size / 2}
        r={radius}
        fill="none"
        stroke="hsl(var(--primary))"
        strokeWidth={stroke}
        strokeDasharray={circumference}
        strokeDashoffset={offset}
        strokeLinecap="round"
      />
      <text x="50%" y="50%" textAnchor="middle" dominantBaseline="middle" className="rotate-90" style={{ transformOrigin: '50% 50%' }}>
        <tspan fill="hsl(var(--foreground))" fontSize="20" fontWeight="600">
          {pct}%
        </tspan>
      </text>
    </svg>
  );
}

export function BedOccupancyCard() {
  const totalOccupied = wardOccupancy.reduce((sum, w) => sum + w.occupied, 0);
  const totalBeds = wardOccupancy.reduce((sum, w) => sum + w.total, 0);
  const overallPct = Math.round((totalOccupied / totalBeds) * 100);

  return (
    <Card className="transition-shadow hover:shadow-soft-lg">
      <CardHeader className="flex-row items-center gap-2.5 space-y-0 pb-3">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-primary/10 text-primary">
          <BedDouble className="h-4 w-4" />
        </span>
        <div>
          <CardTitle className="text-base">Bed Occupancy</CardTitle>
          <CardDescription className="mt-0.5">
            {totalOccupied} of {totalBeds} beds occupied
          </CardDescription>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-5 pt-0">
        <div className="flex justify-center">
          <OccupancyRing pct={overallPct} />
        </div>
        <div className="flex flex-col gap-3">
          {wardOccupancy.map((ward) => (
            <div key={ward.ward}>
              <div className="mb-1 flex items-center justify-between text-xs">
                <span className="font-medium text-foreground">{ward.ward}</span>
                <span className="tabular-nums text-muted-foreground">
                  {ward.occupied}/{ward.total}
                </span>
              </div>
              <ProgressBar value={ward.occupied} max={ward.total} />
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
