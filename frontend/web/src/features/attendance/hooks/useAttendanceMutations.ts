import type { CheckInRequest, CheckOutRequest, CreateAttendanceRequest, UpdateAttendanceRequest } from '@hms/shared';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { attendanceApi } from '../../../services/apiClient';

function useInvalidateAttendance() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: ['attendance'] });
}

export function useCreateAttendanceMutation() {
  const invalidate = useInvalidateAttendance();
  return useMutation({
    mutationFn: (request: CreateAttendanceRequest) => attendanceApi.createAttendance(request),
    onSuccess: invalidate,
  });
}

export function useUpdateAttendanceMutation() {
  const invalidate = useInvalidateAttendance();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateAttendanceRequest }) => attendanceApi.updateAttendance(id, request),
    onSuccess: invalidate,
  });
}

export function useCheckInMutation() {
  const invalidate = useInvalidateAttendance();
  return useMutation({
    mutationFn: (request: CheckInRequest) => attendanceApi.checkIn(request),
    onSuccess: invalidate,
  });
}

export function useCheckOutMutation() {
  const invalidate = useInvalidateAttendance();
  return useMutation({
    mutationFn: (request: CheckOutRequest) => attendanceApi.checkOut(request),
    onSuccess: invalidate,
  });
}
