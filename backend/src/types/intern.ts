export const internStatuses = ['PENDING', 'APPROVED', 'REJECTED'] as const;
export type InternStatus = (typeof internStatuses)[number];

export interface Intern {
  id: string;
  fullName: string;
  studentId: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  gender: 'male' | 'female' | 'other';
  university: string;
  major: string;
  startDate: string;
  endDate: string;
  department: string | null;
  note: string | null;
  status: InternStatus;
  reviewedBy: string | null;
  reviewedAt: string | null;
  rejectionReason: string | null;
  createdAt: string;
  updatedAt: string;
}
