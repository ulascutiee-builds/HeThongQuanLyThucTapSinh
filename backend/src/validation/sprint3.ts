import { z } from 'zod';

export const dateOnly = z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Ngày phải có định dạng YYYY-MM-DD').refine((value) => {
  const parsed = new Date(`${value}T00:00:00.000Z`);
  return !Number.isNaN(parsed.getTime()) && parsed.toISOString().slice(0, 10) === value;
}, 'Ngày không hợp lệ');

const dateRange = <T extends { startDate: string; endDate: string }>(value: T) => value.endDate >= value.startDate;
const dateRangeMessage = { path: ['endDate'], message: 'Ngày kết thúc phải bằng hoặc sau ngày bắt đầu' };
const isoCurrencyCodes = new Set(Intl.supportedValuesOf('currency'));
const isoCurrency = z.string().trim()
  .regex(/^[A-Z]{3}$/, 'Mã tiền tệ phải gồm 3 chữ in hoa theo ISO 4217')
  .refine((value) => isoCurrencyCodes.has(value), 'Mã tiền tệ không hợp lệ theo ISO 4217');

export const attendanceSummaryQuerySchema = z.object({
  from: dateOnly.optional(),
  to: dateOnly.optional(),
  internId: z.string().uuid().optional(),
});

export const attendanceRecordSchema = z.object({
  internId: z.string().uuid(),
  workDate: dateOnly,
  checkIn: z.string().datetime({ offset: true }),
  checkOut: z.string().datetime({ offset: true }).nullable().optional(),
  scheduledStart: z.string().regex(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/, 'Giờ bắt đầu không hợp lệ'),
  scheduledEnd: z.string().regex(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/, 'Giờ kết thúc không hợp lệ'),
}).refine((value) => timeInSeconds(value.scheduledEnd) > timeInSeconds(value.scheduledStart), {
  path: ['scheduledEnd'],
  message: 'Giờ kết thúc ca phải sau giờ bắt đầu ca',
});

export const createLeaveRequestSchema = z.object({
  internId: z.string().uuid(),
  leaveType: z.string().trim().min(1).max(80),
  startDate: dateOnly,
  endDate: dateOnly,
  reason: z.string().trim().min(1).max(2000),
}).refine(dateRange, dateRangeMessage);

export const leaveRequestQuerySchema = z.object({
  internId: z.string().uuid().optional(),
  status: z.enum(['PENDING', 'APPROVED', 'REJECTED']).optional(),
});

export const updateLeaveRequestStatusSchema = z.object({
  status: z.enum(['APPROVED', 'REJECTED']),
  feedback: z.string().trim().max(2000).default(''),
  processedBy: z.string().trim().min(1).max(100),
});

export const allowanceInputSchema = z.object({
  internId: z.string().uuid(),
  title: z.string().trim().min(1).max(150),
  amount: z.number().finite().nonnegative().max(999999999999.99),
  currency: isoCurrency,
  periodStart: dateOnly,
  periodEnd: dateOnly,
  note: z.string().trim().max(2000).default(''),
  changedBy: z.string().trim().min(1).max(100),
}).refine((value) => value.periodEnd >= value.periodStart, {
  path: ['periodEnd'],
  message: 'Kỳ kết thúc phải bằng hoặc sau kỳ bắt đầu',
});

export const allowancePaymentSchema = z.object({
  status: z.enum(['UNPAID', 'PAID']),
  feedback: z.string().trim().max(2000).default(''),
  changedBy: z.string().trim().min(1).max(100),
});

export const allowanceSummaryQuerySchema = z.object({
  from: dateOnly.optional(),
  to: dateOnly.optional(),
  internId: z.string().uuid().optional(),
  status: z.enum(['UNPAID', 'PAID']).optional(),
});

export const mentorAssignmentSchema = z.object({
  mentorId: z.string().uuid(),
  internId: z.string().uuid(),
  startDate: dateOnly,
  endDate: dateOnly.nullable().optional(),
  assignedBy: z.string().trim().min(1).max(100),
}).refine((value) => !value.endDate || value.endDate >= value.startDate, {
  path: ['endDate'],
  message: 'Ngày kết thúc phân công phải bằng hoặc sau ngày bắt đầu',
});

export const schoolsMajorsQuerySchema = z.object({
  university: z.string().trim().max(200).optional(),
  major: z.string().trim().max(200).optional(),
});

function timeInSeconds(value: string) {
  const parts = value.split(':').map(Number);
  const hour = parts[0] ?? -1;
  const minute = parts[1] ?? -1;
  const second = parts[2] ?? 0;
  return hour * 3600 + minute * 60 + second;
}
