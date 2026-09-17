import type { AdmissionStatus, InvoicePaymentStatus, LabOrderItemStatus, OpdConsultationStatus } from '@hms/shared';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Activity, BedDouble, FlaskConical, Stethoscope, Users } from 'lucide-react';
import { useEffect, useState } from 'react';
import { PageBanner } from '@/components/PageBanner';
import { useAuth } from '@/features/auth/AuthContext';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { exportReportToCsv, type ReportSection } from '@/features/reports';
import {
  OpdAdmissionsListTable,
  OpdConsultationListTable,
  OpdFilterBar,
  OpdInvestigationsListTable,
  OpdPatientListTable,
  OpdProceduresListTable,
  emptyOpdFilters,
  todayIsoDate,
  toRangeEnd,
  toRangeStart,
  type OpdFilterValues,
  type OpdTab,
} from '@/features/opd';
import { admissionsApi, billingApi, consultantsApi, laboratoryApi, opdApi } from '@/services/apiClient';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';

const EXPORT_PAGE_SIZE = 1000;

export default function OpdPage() {
  const queryClient = useQueryClient();
  const { scopedConsultantId } = useAuth();
  const [tab, setTab] = useState<OpdTab>('patients');
  const [filters, setFilters] = useState<OpdFilterValues>(emptyOpdFilters);
  const [page, setPage] = useState(1);

  // A Consultant/Doctor user only ever sees their own queue here — the backend forces this
  // regardless (see OpdController.GetPatientList's own comment), this just locks the
  // Department/Consultant filters to match so the UI isn't misleadingly showing pickers that
  // can't actually change what comes back. Needs one lookup to learn their own department,
  // since ConsultantSelect (like the rest of the app) requires a department to show consultant
  // options against.
  const { data: ownConsultant } = useQuery({
    queryKey: ['consultants', scopedConsultantId],
    queryFn: () => consultantsApi.getConsultantById(scopedConsultantId as string),
    enabled: Boolean(scopedConsultantId),
  });

  useEffect(() => {
    if (!scopedConsultantId || !ownConsultant) {
      return;
    }

    setFilters((prev) =>
      prev.departmentId === ownConsultant.departmentId && prev.consultantId === scopedConsultantId
        ? prev
        : { ...prev, departmentId: ownConsultant.departmentId ?? undefined, consultantId: scopedConsultantId },
    );
  }, [scopedConsultantId, ownConsultant]);

  // The filter bar's Search field updates `filters.search` immediately (so the input feels
  // responsive), but every tab's query below reads the debounced value instead — otherwise
  // each keystroke would refire that tab's request, exactly the concern useDebouncedValue
  // already solves for the other list pages (see AdmissionsListPage/LabWorklistPage).
  const debouncedSearch = useDebouncedValue(filters.search);
  const queryFilters: OpdFilterValues = { ...filters, search: debouncedSearch };

  function handleTabChange(value: string) {
    setTab(value as OpdTab);
    setPage(1);
  }

  function handleFiltersChange(next: OpdFilterValues) {
    setFilters(next);
    setPage(1);
  }

  function handleRefresh() {
    // The Admissions tab reuses IPD's own admissions query (key prefix ['ipd', 'admissions']),
    // shared with the IPD module's own Admissions List page — every other tab's query key is
    // prefixed ['opd', ...].
    queryClient.invalidateQueries({ queryKey: tab === 'admissions' ? ['ipd', 'admissions'] : ['opd'] });
  }

  /** Re-fetches up to EXPORT_PAGE_SIZE rows matching the active tab's current filters
   * (independent of on-screen pagination — an export should cover the whole filtered set,
   * not just the visible page) and downloads them as CSV. Each tab's table component owns its
   * own on-screen query; this is a separate, one-off fetch rather than lifting that state up
   * to the page just for this. */
  async function handleExport() {
    const commonFrom = toRangeStart(filters.from);
    const commonTo = toRangeEnd(filters.to);
    let section: ReportSection;

    switch (tab) {
      case 'patients': {
        const result = await opdApi.getPatientList({
          page: 1,
          pageSize: EXPORT_PAGE_SIZE,
          from: commonFrom,
          to: commonTo,
          departmentId: filters.departmentId,
          consultantId: filters.consultantId,
          status: filters.status as OpdConsultationStatus | undefined,
          search: filters.search || undefined,
        });
        section = {
          heading: 'OPD Patients',
          headers: ['Patient Name', 'UHID', 'Age/Gender', 'Appointment Time', 'Appointment Type', 'Consultant', 'Department', 'Status'],
          rows: result.items.map((row) => [
            row.patientName,
            row.uhid,
            `${row.age} Years / ${row.gender[0]}`,
            new Date(row.appointmentTime).toLocaleString('en-IN'),
            row.appointmentTypeName ?? '',
            row.consultantName,
            row.departmentName,
            row.status,
          ]),
        };
        break;
      }
      case 'consultations': {
        const result = await opdApi.getConsultationSummary({
          from: commonFrom,
          to: commonTo,
          departmentId: filters.departmentId,
          consultantId: filters.consultantId,
        });
        section = {
          heading: 'OPD Consultations',
          headers: ['Consultant', 'Department', 'Total Patients', 'Waiting', 'In Consultation', 'Completed'],
          rows: result.map((row) => [row.consultantName, row.departmentName, row.totalPatients, row.waiting, row.inConsultation, row.completed]),
        };
        break;
      }
      case 'investigations': {
        const result = await laboratoryApi.getOrders({
          page: 1,
          pageSize: EXPORT_PAGE_SIZE,
          source: 'OP',
          dateFrom: commonFrom,
          dateTo: commonTo,
          departmentId: filters.departmentId,
          consultantId: filters.consultantId,
          status: filters.status as LabOrderItemStatus | undefined,
          search: filters.search || undefined,
        });
        const rows = result.items.flatMap((order) => order.items.map((item) => ({ order, item })));
        section = {
          heading: 'OPD Investigations',
          headers: ['Patient', 'UHID', 'Investigation', 'Type', 'Date/Time', 'Status'],
          rows: rows.map(({ order, item }) => [
            order.patientName,
            order.patientUhid,
            item.testName,
            item.serviceType ?? '',
            new Date(order.createdAt).toLocaleString('en-IN'),
            item.status,
          ]),
        };
        break;
      }
      case 'procedures': {
        const result = await billingApi.getProcedureLineItems({
          page: 1,
          pageSize: EXPORT_PAGE_SIZE,
          from: commonFrom,
          to: commonTo,
          paymentStatus: filters.status as InvoicePaymentStatus | undefined,
          search: filters.search || undefined,
        });
        section = {
          heading: 'OPD Procedures',
          headers: ['Patient', 'UHID', 'Procedure', 'Consultant', 'Department', 'Date/Time', 'Status', 'Charges'],
          rows: result.items.map((item) => [
            item.patientName,
            item.patientUhid,
            item.serviceId ?? '',
            item.consultantId ?? '',
            item.departmentId ?? '',
            new Date(item.createdAt).toLocaleString('en-IN'),
            item.paymentStatus,
            item.total,
          ]),
        };
        break;
      }
      case 'admissions': {
        const result = await admissionsApi.getAdmissions({
          page: 1,
          pageSize: EXPORT_PAGE_SIZE,
          status: (filters.status as AdmissionStatus | undefined) ?? 'Requested',
          departmentId: filters.departmentId,
          consultantId: filters.consultantId,
          search: filters.search || undefined,
        });
        section = {
          heading: 'OPD Admission Requests',
          headers: ['Patient', 'UHID', 'Age/Gender', 'Consultant', 'Admission Type', 'Requested Date/Time', 'Status'],
          rows: result.items.map((admission) => [
            admission.patientName,
            admission.uhid,
            `${admission.age} / ${admission.gender}`,
            admission.consultantName,
            admission.admissionType,
            new Date(admission.admissionDateTime).toLocaleString('en-IN'),
            admission.status,
          ]),
        };
        break;
      }
    }

    exportReportToCsv(`opd-${tab}-${todayIsoDate()}.csv`, [section]);
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Stethoscope}
        title="Out Patient Department (OPD)"
        subtitle="Outpatient consultant queues, consultations, prescriptions, and investigation orders."
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <OpdFilterBar
          tab={tab}
          filters={filters}
          onChange={handleFiltersChange}
          onRefresh={handleRefresh}
          onExport={handleExport}
          lockDepartmentAndConsultant={Boolean(scopedConsultantId)}
        />

        <Tabs value={tab} onValueChange={handleTabChange}>
          <TabsList>
            <TabsTrigger value="patients" className="gap-1.5">
              <Users className="h-4 w-4" />
              Patient List
            </TabsTrigger>
            <TabsTrigger value="consultations" className="gap-1.5">
              <Stethoscope className="h-4 w-4" />
              Consultation List
            </TabsTrigger>
            <TabsTrigger value="investigations" className="gap-1.5">
              <FlaskConical className="h-4 w-4" />
              Investigations List
            </TabsTrigger>
            <TabsTrigger value="procedures" className="gap-1.5">
              <Activity className="h-4 w-4" />
              Procedures List
            </TabsTrigger>
            <TabsTrigger value="admissions" className="gap-1.5">
              <BedDouble className="h-4 w-4" />
              Admissions List
            </TabsTrigger>
          </TabsList>

          <TabsContent value="patients">
            <OpdPatientListTable filters={queryFilters} page={page} onPageChange={setPage} />
          </TabsContent>

          <TabsContent value="consultations">
            <OpdConsultationListTable
              filters={queryFilters}
              onViewPatients={(consultantId) => {
                setFilters((prev) => ({ ...prev, consultantId }));
                setTab('patients');
                setPage(1);
              }}
            />
          </TabsContent>

          <TabsContent value="investigations">
            <OpdInvestigationsListTable filters={queryFilters} page={page} onPageChange={setPage} />
          </TabsContent>

          <TabsContent value="procedures">
            <OpdProceduresListTable filters={queryFilters} page={page} onPageChange={setPage} />
          </TabsContent>

          <TabsContent value="admissions">
            <OpdAdmissionsListTable filters={queryFilters} page={page} onPageChange={setPage} />
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
