import type { CreateHospitalRequest } from '@hms/shared';
import { useMutation } from '@tanstack/react-query';
import { useRef } from 'react';
import { generateClientId } from '@/lib/id';
import { platformHospitalsApi } from '../../../services/apiClient';

export function useCreateHospitalMutation() {
  // Generated once per hook instance (i.e. once per time the create-hospital page mounts),
  // not per mutate() call — the backend's Idempotency-Key protection only works if retries
  // of the *same* submission attempt (e.g. after a perceived timeout) reuse the same key. A
  // fresh key is picked up automatically the next time this page mounts for a new attempt.
  const idempotencyKeyRef = useRef<string>(generateClientId());

  return useMutation({
    mutationFn: (request: CreateHospitalRequest) => platformHospitalsApi.createHospital(request, idempotencyKeyRef.current),
  });
}
