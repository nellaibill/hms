import { Check, Info, KeyRound, Loader2 } from 'lucide-react';
import { useEffect, useRef } from 'react';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { PasswordInput } from '@/components/ui/password-input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { useAuth } from '@/features/auth/AuthContext';
import { isConsultantRoleName } from '@/lib/isConsultantRoleName';
import { type UsernameStatus } from '../hooks/useLoginAccess';
import { useRolesForSelect } from '../hooks/useRolesForSelect';
import {
  suggestUsername,
  type LoginAccessDraft,
  type LoginAccessErrors,
  type LoginAccessSubject,
} from '../loginAccess';
import { CardHeading } from './CardHeading';
import { LinkedLoginDetails } from './LinkedLoginDetails';

interface LoginAccessCardProps {
  subject: LoginAccessSubject;
  draft: LoginAccessDraft;
  onDraftChange: (draft: LoginAccessDraft) => void;
  errors: LoginAccessErrors;
  usernameStatus: UsernameStatus;
  /** The person's name as typed so far — only used to suggest a username. */
  personName: string;
  /** The login already linked to this record: shows it (with its own actions) instead of the toggle. */
  linkedUserId?: string | null;
  isLoadingLink?: boolean;
  /** A record's view page: shows the linked login, or says there's none, but can't add one. */
  readOnly?: boolean;
}

const COPY: Record<LoginAccessSubject, { noun: string; leaveOff: string; note: string }> = {
  employee: {
    noun: 'employee',
    leaveOff:
      'Leave this off for staff who never use HMS, such as drivers, housekeepers, electricians and watchmen.',
    note: 'Name, email and phone are taken from this form, so you don’t type them twice. The employee must change this password the first time they log in.',
  },
  consultant: {
    noun: 'consultant',
    leaveOff: 'Turn this off for a visiting doctor who never uses HMS.',
    note: 'The login is linked to this consultant automatically. They must change this password the first time they log in.',
  },
};

/**
 * One card for both the Employee and the Consultant forms (ADR-108): decide whether this
 * person logs in to HMS, and if so, only the things a login has that their own record
 * doesn't — username, role and password (plus email and phone for a consultant, whose
 * record has neither). Every person gets a login of their own; there's no picking an
 * existing one. Hidden from anyone who can't see Settings → Users.
 */
export function LoginAccessCard({
  subject,
  draft,
  onDraftChange,
  errors,
  usernameStatus,
  personName,
  linkedUserId,
  isLoadingLink,
  readOnly,
}: LoginAccessCardProps) {
  const { hasPermission } = useAuth();
  const canView = hasPermission('identity-administration.view');
  const canCreate = hasPermission('identity-administration.create');
  const copy = COPY[subject];
  const { data: roles } = useRolesForSelect();
  const showForm = !readOnly && !linkedUserId && !isLoadingLink;

  // Once someone types a username themselves, stop replacing it with the suggestion.
  const usernameEditedRef = useRef(false);

  // Fill in what can be guessed while login access is on: the username follows the name as
  // it's typed, and a consultant's login almost always gets the doctor role. One effect, so
  // the two never overwrite each other's change.
  useEffect(() => {
    if (!showForm || !draft.enabled) return;
    const changes: Partial<LoginAccessDraft> = {};
    const suggestion = suggestUsername(personName);
    if (!usernameEditedRef.current && suggestion !== draft.username) {
      changes.username = suggestion;
    }
    const consultantRole =
      subject === 'consultant' && !draft.roleId
        ? roles?.items.find((role) => isConsultantRoleName(role.name))
        : undefined;
    if (consultantRole) {
      changes.roleId = consultantRole.id;
    }
    if (Object.keys(changes).length > 0) {
      onDraftChange({ ...draft, ...changes });
    }
  }, [showForm, subject, draft, personName, roles, onDraftChange]);

  if (!canView) {
    return null;
  }

  function update(changes: Partial<LoginAccessDraft>) {
    onDraftChange({ ...draft, ...changes });
  }

  return (
    <Card>
      <CardHeader>
        <CardHeading
          icon={KeyRound}
          title="Login access"
          description={
            linkedUserId
              ? `This ${copy.noun} can log in to HMS.`
              : `Choose whether this ${copy.noun} can log in to HMS.`
          }
        />
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {isLoadingLink && (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Checking for a login…
          </p>
        )}

        {!isLoadingLink && linkedUserId && <LinkedLoginDetails userId={linkedUserId} />}

        {!isLoadingLink && !linkedUserId && readOnly && (
          <p className="text-sm text-muted-foreground">
            No login yet. Edit this {copy.noun} to give them one.
          </p>
        )}

        {showForm && (
          <>
            <div className="flex items-start gap-3">
              <Switch
                id="loginAccessEnabled"
                checked={draft.enabled}
                onCheckedChange={(enabled) => update({ enabled })}
                disabled={!canCreate}
                aria-label={`Allow this ${copy.noun} to log in`}
              />
              <div className="flex flex-col gap-0.5">
                <Label htmlFor="loginAccessEnabled" className="cursor-pointer">
                  Allow this {copy.noun} to log in to HMS
                </Label>
                <p className="text-xs text-muted-foreground">
                  {canCreate ? copy.leaveOff : 'You don’t have permission to create logins.'}
                </p>
              </div>
            </div>

            {draft.enabled && (
              <>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="loginUsername">Username</Label>
                    <Input
                      id="loginUsername"
                      autoComplete="off"
                      value={draft.username}
                      onChange={(event) => {
                        usernameEditedRef.current = true;
                        update({ username: event.target.value });
                      }}
                    />
                    {errors.username ? (
                      <p className="text-sm text-destructive">{errors.username}</p>
                    ) : usernameStatus === 'taken' ? (
                      <p className="text-sm text-destructive">This username is already taken.</p>
                    ) : usernameStatus === 'available' ? (
                      <p className="flex items-center gap-1 text-xs text-success">
                        <Check className="h-3.5 w-3.5" />
                        Available
                      </p>
                    ) : (
                      <p className="text-xs text-muted-foreground">
                        Suggested from the name. You can change it.
                      </p>
                    )}
                  </div>

                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="loginRole">Role</Label>
                    <Select value={draft.roleId} onValueChange={(roleId) => update({ roleId })}>
                      <SelectTrigger id="loginRole" aria-label="Login role">
                        <SelectValue placeholder="Select a role" />
                      </SelectTrigger>
                      <SelectContent>
                        {roles?.items.map((role) => (
                          <SelectItem key={role.id} value={role.id}>
                            {role.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {errors.roleId && <p className="text-sm text-destructive">{errors.roleId}</p>}
                  </div>

                  {subject === 'consultant' && (
                    <>
                      <div className="flex flex-col gap-1.5">
                        <Label htmlFor="loginEmail">Email</Label>
                        <Input
                          id="loginEmail"
                          type="email"
                          autoComplete="off"
                          value={draft.email}
                          onChange={(event) => update({ email: event.target.value })}
                        />
                        {errors.email && <p className="text-sm text-destructive">{errors.email}</p>}
                      </div>
                      <div className="flex flex-col gap-1.5">
                        <Label htmlFor="loginPhone">Mobile number</Label>
                        <Input
                          id="loginPhone"
                          inputMode="numeric"
                          autoComplete="off"
                          value={draft.phoneNumber}
                          // Digits only, capped here rather than with maxLength, which would
                          // count a typed space or dash before it's stripped.
                          onChange={(event) =>
                            update({
                              phoneNumber: event.target.value.replace(/\D/g, '').slice(0, 10),
                            })
                          }
                        />
                        {errors.phoneNumber && (
                          <p className="text-sm text-destructive">{errors.phoneNumber}</p>
                        )}
                      </div>
                    </>
                  )}

                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="loginPassword">Password</Label>
                    <PasswordInput
                      id="loginPassword"
                      autoComplete="new-password"
                      value={draft.password}
                      onChange={(event) => update({ password: event.target.value })}
                    />
                    {errors.password && (
                      <p className="text-sm text-destructive">{errors.password}</p>
                    )}
                  </div>

                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="loginConfirmPassword">Confirm password</Label>
                    <PasswordInput
                      id="loginConfirmPassword"
                      autoComplete="new-password"
                      value={draft.confirmPassword}
                      onChange={(event) => update({ confirmPassword: event.target.value })}
                    />
                    {errors.confirmPassword && (
                      <p className="text-sm text-destructive">{errors.confirmPassword}</p>
                    )}
                  </div>
                </div>

                <p className="flex items-start gap-2 rounded-md bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
                  <Info className="mt-0.5 h-3.5 w-3.5 shrink-0" />
                  {subject === 'consultant'
                    ? 'A consultant record has no email or phone, so the login needs its own. ' +
                      copy.note
                    : copy.note}
                </p>
              </>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
