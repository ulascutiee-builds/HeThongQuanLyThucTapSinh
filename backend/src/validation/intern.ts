import { z } from 'zod';

const dateOnly = z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Ngày phải có định dạng YYYY-MM-DD').refine((value) => {
  const parsed = new Date(`${value}T00:00:00.000Z`);
  return !Number.isNaN(parsed.getTime()) && parsed.toISOString().slice(0, 10) === value;
}, 'Ngày không hợp lệ');

export const createInternSchema = z.object({
  fullName: z.string().trim().min(2, 'Họ tên phải có ít nhất 2 ký tự').max(150),
  studentId: z.string().trim().min(1, 'Mã sinh viên là bắt buộc').max(50),
  email: z.email('Email không hợp lệ').trim().toLowerCase().max(254),
  phone: z.string().trim().regex(/^0[35789]\d{8}$/, 'Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 03, 05, 07, 08 hoặc 09'),
  dateOfBirth: dateOnly,
  gender: z.enum(['male', 'female', 'other']),
  university: z.string().trim().min(1, 'Trường đại học là bắt buộc').max(200),
  major: z.string().trim().min(1, 'Chuyên ngành là bắt buộc').max(200),
  startDate: dateOnly,
  endDate: dateOnly,
  department: z.string().trim().max(150).optional().nullable(),
  note: z.string().trim().max(2000).optional().nullable(),
}).refine((data) => data.endDate > data.startDate, {
  path: ['endDate'],
  message: 'Ngày kết thúc phải sau ngày bắt đầu',
});

export type CreateInternInput = z.infer<typeof createInternSchema>;

export const listInternQuerySchema = z.object({
  search: z.string().trim().max(200).optional(),
  status: z.enum(['PENDING', 'APPROVED', 'REJECTED']).optional(),
  page: z.coerce.number().int().min(1).default(1),
  limit: z.coerce.number().int().min(1).max(100).default(20),
});

// Shared policy for account-registration code owned by the authentication task.
export const passwordSchema = z.string()
  .min(8, 'Mật khẩu phải có ít nhất 8 ký tự')
  .max(72, 'Mật khẩu không được vượt quá 72 ký tự')
  .regex(/[A-Za-z]/, 'Mật khẩu phải chứa ít nhất một chữ cái')
  .regex(/[0-9]/, 'Mật khẩu phải chứa ít nhất một chữ số');
