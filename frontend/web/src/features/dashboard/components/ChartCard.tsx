import type { ReactNode } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

interface ChartCardProps {
  title: string;
  description?: string;
  legend?: ReactNode;
  children: ReactNode;
}

export function ChartCard({ title, description, legend, children }: ChartCardProps) {
  return (
    <Card className="transition-shadow hover:shadow-soft-md">
      <CardHeader className="flex-row items-start justify-between space-y-0 pb-2">
        <div>
          <CardTitle className="text-base">{title}</CardTitle>
          {description && <CardDescription className="mt-0.5">{description}</CardDescription>}
        </div>
        {legend}
      </CardHeader>
      <CardContent className="pt-2">
        <div className="h-72 w-full">{children}</div>
      </CardContent>
    </Card>
  );
}
