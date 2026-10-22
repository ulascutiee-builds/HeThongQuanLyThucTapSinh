import assert from 'node:assert/strict';
import test from 'node:test';

const {
  allowanceInputSchema,
  attendanceRecordSchema,
  createLeaveRequestSchema,
  mentorAssignmentSchema,
  updateLeaveRequestStatusSchema,
} = await import('../dist/validation/sprint3.js');

const internId = '45b32d6f-45c2-4866-a4a8-a9490abf1c10';
const mentorId = '360d068e-4946-431a-a056-d9d12b0ec0e1';

test('accepts an attendance record with valid shift bounds', () => {
  const result = attendanceRecordSchema.safeParse({
    internId,
    workDate: '2026-10-10',
    checkIn: '2026-10-10T08:05:00+07:00',
    checkOut: '2026-10-10T17:00:00+07:00',
    scheduledStart: '08:00',
    scheduledEnd: '17:00',
  });
  assert.equal(result.success, true);
});

test('rejects leave periods where the end date precedes the start date', () => {
  const result = createLeaveRequestSchema.safeParse({
    internId,
    leaveType: 'Nghỉ phép',
    startDate: '2026-10-12',
    endDate: '2026-10-11',
    reason: 'Có việc cá nhân',
  });
  assert.equal(result.success, false);
});

test('requires a processor when a leave request is decided', () => {
  const result = updateLeaveRequestStatusSchema.safeParse({
    status: 'APPROVED',
    feedback: 'Đã duyệt',
  });
  assert.equal(result.success, false);
});

test('accepts ISO currency codes and rejects invalid codes or reversed allowance periods', () => {
  const valid = allowanceInputSchema.safeParse({
    internId,
    title: 'Phụ cấp ăn trưa',
    amount: 500000,
    currency: 'VND',
    periodStart: '2026-10-01',
    periodEnd: '2026-10-31',
    changedBy: 'hr-user',
  });
  const invalidCurrency = allowanceInputSchema.safeParse({
    internId,
    title: 'Phụ cấp ăn trưa',
    amount: 500000,
    currency: 'XYZ',
    periodStart: '2026-10-01',
    periodEnd: '2026-10-31',
    changedBy: 'hr-user',
  });
  const invalidPeriod = allowanceInputSchema.safeParse({
    internId,
    title: 'Phụ cấp ăn trưa',
    amount: 500000,
    currency: 'VND',
    periodStart: '2026-10-31',
    periodEnd: '2026-10-01',
    changedBy: 'hr-user',
  });
  assert.equal(valid.success, true);
  assert.equal(invalidCurrency.success, false);
  assert.equal(invalidPeriod.success, false);
});

test('accepts a mentor assignment with a valid date range', () => {
  const result = mentorAssignmentSchema.safeParse({
    mentorId,
    internId,
    startDate: '2026-10-10',
    endDate: '2026-12-31',
    assignedBy: 'hr-user',
  });
  assert.equal(result.success, true);
});
