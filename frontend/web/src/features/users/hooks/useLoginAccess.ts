import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { useAuth } from '@/features/auth/AuthContext';
import { usersApi } from '../../../services/apiClient';
import {
  emptyLoginDraft,
  hasLoginErrors,
  splitPersonName,
  validateLoginDraft,
  type LoginAccessDraft,
  type LoginAccessErrors,
  type LoginAccessSubject,
} from '../loginAccess';

export type UsernameStatus = 'idle' | 'checking' | 'available' | 'taken';

/**
 * The Login access card's state, shared by the Employee form and the Consultant form so both
 * behave the same. `validate()` is called on Save alongside the form's own validation.
 * Login access starts on only when asked (a new consultant) and the user may create logins.
 */
export function useLoginAccessState(subject: LoginAccessSubject, startEnabled: boolean) {
  const { hasPermission } = useAuth();
  const [draft, setDraft] = useState(() =>
    emptyLoginDraft(
      startEnabled &&
        hasPermission('identity-administration.create') &&
        hasPermission('identity-administration.view'),
    ),
  );
  const [errors, setErrors] = useState<LoginAccessErrors>({});
  const usernameStatus = useUsernameAvailability(
    draft.username,
    draft.enabled && hasPermission('identity-administration.view'),
  );

  function onDraftChange(next: LoginAccessDraft) {
    // Clear only the errors of fields that just changed, so the rest stay visible.
    setErrors((current) => {
      const remaining = { ...current };
      for (const key of Object.keys(next) as (keyof LoginAccessDraft)[]) {
        if (next[key] !== draft[key]) delete remaining[key];
      }
      return remaining;
    });
    setDraft(next);
  }

  function validate(): boolean {
    const next = validateLoginDraft(draft, subject);
    if (draft.enabled && usernameStatus === 'taken') {
      next.username ??= 'This username is already taken.';
    }
    setErrors(next);
    return !hasLoginErrors(next);
  }

  return {
    draft,
    errors,
    setErrors,
    validate,
    cardProps: { subject, draft, onDraftChange, errors, usernameStatus },
  };
}

const usernamePattern = /^[a-zA-Z0-9._-]+$/;

/**
 * Live "is this username free?" for the Login access card, so a clash shows before Save —
 * which matters most for a consultant, whose login is saved after the consultant itself.
 * The server still has the final say.
 */
export function useUsernameAvailability(username: string, enabled: boolean): UsernameStatus {
  const trimmed = username.trim();
  const [debounced, setDebounced] = useState(trimmed);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(trimmed), 400);
    return () => clearTimeout(timer);
  }, [trimmed]);

  const canCheck = enabled && debounced.length > 0 && usernamePattern.test(debounced);
  const query = useQuery({
    queryKey: ['users', 'username-check', debounced.toLowerCase()],
    queryFn: () => usersApi.getUsers({ search: debounced, pageSize: 20 }),
    enabled: canCheck,
    staleTime: 10_000,
  });

  if (!canCheck) {
    return 'idle';
  }
  if (debounced !== trimmed || query.isPending) {
    return 'checking';
  }
  return query.data?.items.some((user) => user.username.toLowerCase() === debounced.toLowerCase())
    ? 'taken'
    : 'available';
}

/** The login linked to a consultant — the link lives on the user (User.ConsultantId), so it's looked up from that side. */
export function useConsultantLoginQuery(consultantId: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: ['users', 'by-consultant', consultantId],
    queryFn: async () => (await usersApi.getUsers({ consultantId, pageSize: 1 })).items[0] ?? null,
    enabled: Boolean(consultantId) && enabled,
  });
}

/**
 * A consultant's login is saved right after the consultant (Masters can't call Identity, so
 * there's no single request for both, unlike an employee's), with the consultant's id and name.
 */
export function useSaveConsultantLoginMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      consultantId,
      consultantName,
      draft,
    }: {
      consultantId: string;
      consultantName: string;
      draft: LoginAccessDraft;
    }) => {
      const { firstName, lastName } = splitPersonName(consultantName);
      return usersApi.createUser({
        username: draft.username.trim(),
        firstName,
        lastName,
        email: draft.email.trim(),
        phoneNumber: draft.phoneNumber.trim(),
        roleId: draft.roleId,
        consultantId,
        password: draft.password,
      });
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  });
}
