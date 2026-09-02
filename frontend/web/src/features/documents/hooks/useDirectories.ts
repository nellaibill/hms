import { useQuery } from '@tanstack/react-query';
import { employeesApi, patientsApi, usersApi } from '../../../services/apiClient';
import type { SearchableSelectOption } from '@/components/ui/searchable-select';
import type { EntityType } from '../types';

export interface DirectoryEntry {
  id: string;
  label: string;
}

/**
 * These three directories back every name lookup on the Document Management page (the "Entity"
 * column/picker for Patient & Staff owners, and the "Uploaded By" column/filter) — the
 * Documents API only ever returns raw ids (OwnerId, UploadedByUserId), never a display name.
 * Each is capped at 100 rows, matching the "search-first picker, not a full browse" convention
 * already used by UsersController.GetDirectory (see its own doc comment) — a real deployment
 * with more records than that would need a searchable/paged picker instead.
 */

export function usePatientDirectory() {
  return useQuery({
    queryKey: ['documents', 'directory', 'patients'],
    queryFn: async (): Promise<DirectoryEntry[]> => {
      const { items } = await patientsApi.getPatients({ pageSize: 100 });
      return items.map((p) => ({ id: p.id, label: `${p.firstName} ${p.lastName} (${p.uhid})` }));
    },
    staleTime: 60_000,
  });
}

export function useStaffDirectory() {
  return useQuery({
    queryKey: ['documents', 'directory', 'staff'],
    queryFn: async (): Promise<DirectoryEntry[]> => {
      const { items } = await employeesApi.getEmployees({ pageSize: 100 });
      return items.map((e) => ({ id: e.id, label: `${e.firstName} ${e.lastName} (${e.employeeCode})` }));
    },
    staleTime: 60_000,
  });
}

/** Who uploaded a document is a login identity (Users), distinct from the "Staff" owner type
 * above (an HR Employee record) — resolved via the same low-sensitivity staff picker the
 * messaging module uses (UsersController.GetDirectory), since no admin permission is needed
 * just to see who uploaded a file. */
export function useUploaderDirectory() {
  return useQuery({
    queryKey: ['documents', 'directory', 'uploaders'],
    queryFn: async (): Promise<DirectoryEntry[]> => {
      const entries = await usersApi.getStaffDirectory();
      return entries.map((u) => ({ id: u.id, label: `${u.firstName} ${u.lastName}` }));
    },
    staleTime: 60_000,
  });
}

/** Only Patient and Staff owner types have a real backend list to pick from today — the other
 * eight (Doctor, Appointment, Admission, Lab, Radiology, Billing, Asset, Vendor) have no
 * lookup/search endpoint, so their Entity field falls back to a free-typed id (see FilterBar
 * and UploadDocumentModal). */
export function isEntityPickerSupported(entityType: EntityType | undefined | ''): boolean {
  return entityType === 'Patient' || entityType === 'Staff';
}

export function buildEntityOptions(
  entityType: EntityType | undefined | '',
  patients: DirectoryEntry[],
  staff: DirectoryEntry[],
): SearchableSelectOption[] {
  const list = entityType === 'Patient' ? patients : entityType === 'Staff' ? staff : [];
  return list.map((entry) => ({ value: entry.id, label: entry.label }));
}

export function resolveEntityLabel(
  entityType: EntityType,
  entityId: string,
  patients: DirectoryEntry[],
  staff: DirectoryEntry[],
): string {
  const list = entityType === 'Patient' ? patients : entityType === 'Staff' ? staff : [];
  return list.find((entry) => entry.id === entityId)?.label ?? entityId;
}

export function resolveUploaderLabel(userId: string, uploaders: DirectoryEntry[]): string {
  if (!userId) return 'Unknown';
  return uploaders.find((entry) => entry.id === userId)?.label ?? userId;
}
