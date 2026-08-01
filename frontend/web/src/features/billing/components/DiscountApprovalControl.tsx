import { useAuth } from '@/features/auth/AuthContext';
import { ShieldAlert, ShieldCheck } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import type { Role } from '@/features/auth/types';

/** Roles trusted to approve their own discounts directly. Everyone else (Receptionist, in the common case) has to go through the supervisor-override flow below. */
const SELF_APPROVE_ROLES: readonly Role[] = ['admin', 'accounts', 'superAdmin'];

interface DiscountApprovalControlProps {
  id: string;
  approved: boolean;
  approvedBy: string;
  discount: number;
  onChange: (approved: boolean, approvedBy: string) => void;
}

/**
 * A "Discount approved" checkbox with nothing behind it is just decoration — anyone at the
 * keyboard can tick it. This ties it to the signed-in role: Hospital Administrator / Accounts
 * Officer / Super Admin can self-approve; every other role (a Receptionist, day to day) has
 * to record who authorized the discount via an inline supervisor-override step, so
 * `discountApprovedBy` always names someone with actual authority.
 */
export function DiscountApprovalControl({ id, approved, approvedBy, discount, onChange }: DiscountApprovalControlProps) {
  const { user } = useAuth();
  const canSelfApprove = user ? SELF_APPROVE_ROLES.includes(user.role) : false;
  const [overrideOpen, setOverrideOpen] = useState(false);
  const [overrideName, setOverrideName] = useState('');

  // Nothing to approve when there's no discount — keep the control out of the way entirely.
  if (discount <= 0) return null;

  if (approved) {
    return (
      <div className="flex items-center gap-1.5 pb-2.5 text-sm text-success">
        <ShieldCheck className="h-4 w-4 shrink-0" />
        <span>Discount approved{approvedBy ? ` — ${approvedBy}` : ''}</span>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-6 px-1.5 text-xs text-muted-foreground"
          onClick={() => onChange(false, '')}
        >
          Undo
        </Button>
      </div>
    );
  }

  if (canSelfApprove) {
    return (
      <div className="flex items-center gap-2 pb-2.5">
        <input
          id={id}
          type="checkbox"
          className="h-4 w-4 rounded border-input"
          checked={approved}
          onChange={(e) => onChange(e.target.checked, e.target.checked ? user!.name : '')}
        />
        <label htmlFor={id} className="cursor-pointer text-sm font-normal text-foreground">
          Discount approved
        </label>
      </div>
    );
  }

  if (overrideOpen) {
    return (
      <div className="flex flex-wrap items-end gap-2 pb-2.5">
        <div className="flex flex-col gap-1">
          <label htmlFor={`${id}-override-name`} className="text-xs font-medium text-muted-foreground">
            Supervisor name
          </label>
          <Input
            id={`${id}-override-name`}
            value={overrideName}
            onChange={(e) => setOverrideName(e.target.value)}
            placeholder="e.g. Muthuraman Pillai"
            className="h-8 w-44 text-sm"
            autoFocus
          />
        </div>
        <Button
          type="button"
          size="sm"
          className="h-8"
          disabled={!overrideName.trim()}
          onClick={() => {
            onChange(true, overrideName.trim());
            setOverrideOpen(false);
            setOverrideName('');
          }}
        >
          Approve
        </Button>
        <Button type="button" variant="ghost" size="sm" className="h-8" onClick={() => setOverrideOpen(false)}>
          Cancel
        </Button>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2 pb-2.5">
      <ShieldAlert className="h-4 w-4 shrink-0 text-warning" />
      <span className="text-sm text-muted-foreground">Needs supervisor approval.</span>
      <Button type="button" variant="outline" size="sm" className="h-7 text-xs" onClick={() => setOverrideOpen(true)}>
        Get approval
      </Button>
    </div>
  );
}
