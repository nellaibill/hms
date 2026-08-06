import { ENTITY_OPTIONS } from './mockEntities';
import { todayIsoDate } from './utils/format';
import type { DocumentType, EntityType, HmsDocument } from './types';

interface SeedSpec {
  offsetDays: number;
  entityType: EntityType;
  entityIndex: number;
  documentType: DocumentType;
  fileName: string;
  mimeType: string;
  fileSize: number;
  uploadedBy: string;
  isArchived: boolean;
  /** Whether this seed entry has real, previewable content (image/PDF) — most don't, mirroring how
   * older catalog entries in a real deployment would have their files rotated off active storage. */
  hasRealContent?: boolean;
}

const PDF = 'application/pdf';
const JPG = 'image/jpeg';
const PNG = 'image/png';
const DOCX = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
const XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

// A tiny embedded PNG (1x1 transparent-free teal square) used so a couple of seed rows can
// demonstrate the real "Image preview" panel state without shipping a binary asset file.
const DEMO_IMAGE_DATA_URL =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAYAAABw4pVUAAAAI0lEQVR4nO3BAQ0AAADCoPdPbQ43oAAAAAAAAAAAAAAAAAA' +
  'HAxRuAAHmzAn0AAAAAElFTkSuQmCc';

const SEED: SeedSpec[] = [
  { offsetDays: -14, entityType: 'Patient', entityIndex: 0, documentType: 'ID Proof', fileName: 'meena-sundaram-aadhaar.pdf', mimeType: PDF, fileSize: 482_311, uploadedBy: 'Selvi N', isArchived: false },
  { offsetDays: -13, entityType: 'Patient', entityIndex: 0, documentType: 'Insurance', fileName: 'meena-sundaram-star-health.pdf', mimeType: PDF, fileSize: 891_004, uploadedBy: 'Selvi N', isArchived: false },
  { offsetDays: -12, entityType: 'Patient', entityIndex: 1, documentType: 'Consent Form', fileName: 'rajesh-pandian-surgery-consent.pdf', mimeType: PDF, fileSize: 210_552, uploadedBy: 'Revathi P', isArchived: false },
  { offsetDays: -11, entityType: 'Patient', entityIndex: 2, documentType: 'ID Proof', fileName: 'kavitha-raman-passport.jpg', mimeType: JPG, fileSize: 1_204_880, uploadedBy: 'Selvi N', isArchived: false, hasRealContent: true },
  { offsetDays: -10, entityType: 'Lab', entityIndex: 0, documentType: 'Report', fileName: 'cbc-result-lab6001.pdf', mimeType: PDF, fileSize: 156_204, uploadedBy: 'Muthu S', isArchived: false },
  { offsetDays: -10, entityType: 'Lab', entityIndex: 1, documentType: 'Report', fileName: 'lipid-profile-lab6002.pdf', mimeType: PDF, fileSize: 143_910, uploadedBy: 'Muthu S', isArchived: false },
  { offsetDays: -9, entityType: 'Radiology', entityIndex: 0, documentType: 'Report', fileName: 'chest-xray-rad7001.png', mimeType: PNG, fileSize: 2_310_400, uploadedBy: 'Anand V', isArchived: false, hasRealContent: true },
  { offsetDays: -9, entityType: 'Radiology', entityIndex: 1, documentType: 'Report', fileName: 'mri-brain-rad7002.pdf', mimeType: PDF, fileSize: 4_882_004, uploadedBy: 'Anand V', isArchived: false },
  { offsetDays: -8, entityType: 'Doctor', entityIndex: 0, documentType: 'Certificate', fileName: 'karthikeyan-mci-registration.pdf', mimeType: PDF, fileSize: 322_150, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -8, entityType: 'Doctor', entityIndex: 2, documentType: 'Certificate', fileName: 'suresh-kumar-md-ortho.pdf', mimeType: PDF, fileSize: 298_004, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -7, entityType: 'Staff', entityIndex: 0, documentType: 'ID Proof', fileName: 'kavitha-r-employee-id.jpg', mimeType: JPG, fileSize: 640_112, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -7, entityType: 'Staff', entityIndex: 1, documentType: 'Certificate', fileName: 'muthu-s-lab-tech-diploma.pdf', mimeType: PDF, fileSize: 512_990, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -6, entityType: 'Billing', entityIndex: 0, documentType: 'Invoice', fileName: 'invoice-inv8001.pdf', mimeType: PDF, fileSize: 98_204, uploadedBy: 'Selvi N', isArchived: false },
  { offsetDays: -6, entityType: 'Billing', entityIndex: 1, documentType: 'Invoice', fileName: 'invoice-inv8002.pdf', mimeType: PDF, fileSize: 104_552, uploadedBy: 'Selvi N', isArchived: false },
  { offsetDays: -5, entityType: 'Vendor', entityIndex: 0, documentType: 'Invoice', fileName: 'medsupply-po-2451.xlsx', mimeType: XLSX, fileSize: 44_802, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -5, entityType: 'Vendor', entityIndex: 1, documentType: 'Other', fileName: 'clinlab-service-agreement.docx', mimeType: DOCX, fileSize: 88_402, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -4, entityType: 'Asset', entityIndex: 0, documentType: 'Certificate', fileName: 'mri-machine-amc-certificate.pdf', mimeType: PDF, fileSize: 275_600, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -4, entityType: 'Asset', entityIndex: 3, documentType: 'Report', fileName: 'ambulance-fitness-report.pdf', mimeType: PDF, fileSize: 190_004, uploadedBy: 'Admin User', isArchived: false },
  { offsetDays: -3, entityType: 'Appointment', entityIndex: 0, documentType: 'Prescription', fileName: 'apt4001-prescription.pdf', mimeType: PDF, fileSize: 76_204, uploadedBy: 'Dr. Karthikeyan', isArchived: false },
  { offsetDays: -3, entityType: 'Admission', entityIndex: 0, documentType: 'Consent Form', fileName: 'adm5001-admission-consent.pdf', mimeType: PDF, fileSize: 168_020, uploadedBy: 'Revathi P', isArchived: false },
  { offsetDays: -2, entityType: 'Patient', entityIndex: 3, documentType: 'Prescription', fileName: 'muthu-selvam-discharge-summary.docx', mimeType: DOCX, fileSize: 55_204, uploadedBy: 'Revathi P', isArchived: true },
  { offsetDays: -20, entityType: 'Patient', entityIndex: 4, documentType: 'ID Proof', fileName: 'deepa-nachiyar-voter-id.jpg', mimeType: JPG, fileSize: 780_400, uploadedBy: 'Selvi N', isArchived: true },
  { offsetDays: -25, entityType: 'Vendor', entityIndex: 2, documentType: 'Invoice', fileName: 'sunrise-pharma-old-invoice.pdf', mimeType: PDF, fileSize: 62_004, uploadedBy: 'Admin User', isArchived: true },
  { offsetDays: -30, entityType: 'Staff', entityIndex: 2, documentType: 'Other', fileName: 'ganesan-k-relieving-letter.pdf', mimeType: PDF, fileSize: 45_120, uploadedBy: 'Admin User', isArchived: true },
  { offsetDays: 0, entityType: 'Lab', entityIndex: 2, documentType: 'Report', fileName: 'lft-result-lab6003.pdf', mimeType: PDF, fileSize: 132_400, uploadedBy: 'Muthu S', isArchived: false },
  { offsetDays: 0, entityType: 'Patient', entityIndex: 5, documentType: 'Insurance', fileName: 'saravanan-pillai-policy.pdf', mimeType: PDF, fileSize: 704_552, uploadedBy: 'Selvi N', isArchived: false },
];

function toIsoDateTime(offsetDays: number): string {
  const date = new Date();
  date.setDate(date.getDate() + offsetDays);
  date.setHours(9 + Math.abs(offsetDays % 6), 15, 0, 0);
  return date.toISOString();
}

export function buildMockDocuments(): HmsDocument[] {
  return SEED.map((seed, index) => {
    const entity = ENTITY_OPTIONS[seed.entityType][seed.entityIndex];
    return {
      id: `doc-${String(index + 1).padStart(3, '0')}`,
      entityType: seed.entityType,
      entityId: entity.id,
      documentType: seed.documentType,
      fileName: seed.fileName,
      originalFileName: seed.fileName,
      filePath: seed.hasRealContent ? DEMO_IMAGE_DATA_URL : 'seed://no-content',
      fileSize: seed.fileSize,
      mimeType: seed.mimeType,
      uploadedBy: seed.uploadedBy,
      uploadedAt: toIsoDateTime(seed.offsetDays),
      isArchived: seed.isArchived,
    } satisfies HmsDocument;
  });
}

export { todayIsoDate };
