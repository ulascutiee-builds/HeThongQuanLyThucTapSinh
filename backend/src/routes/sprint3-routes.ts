import { randomUUID } from 'node:crypto';
import { Router } from 'express';
import type { PoolClient } from 'pg';
import { pool } from '../db/pool.js';
import { asyncHandler } from '../utils/async-handler.js';
import { HttpError } from '../utils/http-error.js';
import {
  allowanceInputSchema,
  allowancePaymentSchema,
  allowanceSummaryQuerySchema,
  attendanceRecordSchema,
  attendanceSummaryQuerySchema,
  createLeaveRequestSchema,
  leaveRequestQuerySchema,
  mentorAssignmentSchema,
  schoolsMajorsQuerySchema,
  updateLeaveRequestStatusSchema,
} from '../validation/sprint3.js';

export const sprint3Router = Router();

sprint3Router.get('/attendance/summary', asyncHandler(async (request, response) => {
  const filters = attendanceSummaryQuerySchema.parse(request.query);
  const to = filters.to ?? new Date().toISOString().slice(0, 10);
  const from = filters.from ?? `${to.slice(0, 7)}-01`;
  if (from > to) throw new HttpError(400, 'Ngày bắt đầu phải trước hoặc bằng ngày kết thúc');

  const result = await pool.query(
    `SELECT i.id AS "internId", i.full_name AS "fullName", i.student_id AS "studentId",
            COALESCE(a.work_days, 0)::int AS "workDays",
            COALESCE(a.late_days, 0)::int AS "lateDays",
            COALESCE(a.early_leave_days, 0)::int AS "earlyLeaveDays",
            COALESCE(l.leave_days, 0)::int AS "leaveDays"
       FROM interns i
       LEFT JOIN (
         SELECT intern_id,
                count(DISTINCT work_date) FILTER (WHERE check_in IS NOT NULL) AS work_days,
                count(DISTINCT work_date) FILTER (
                  WHERE (check_in AT TIME ZONE 'Asia/Ho_Chi_Minh')::time > scheduled_start
                ) AS late_days,
                count(DISTINCT work_date) FILTER (
                  WHERE check_out IS NOT NULL
                    AND (check_out AT TIME ZONE 'Asia/Ho_Chi_Minh')::time < scheduled_end
                ) AS early_leave_days
           FROM attendance_records
          WHERE work_date BETWEEN $1::date AND $2::date
          GROUP BY intern_id
       ) a ON a.intern_id = i.id
       LEFT JOIN (
         SELECT lr.intern_id, count(DISTINCT leave_day)::int AS leave_days
           FROM leave_requests lr
           CROSS JOIN LATERAL generate_series(
             greatest(lr.start_date, $1::date)::timestamp,
             least(lr.end_date, $2::date)::timestamp,
             interval '1 day'
           ) AS leave_days_by_date(leave_day)
          WHERE lr.status = 'APPROVED'
            AND lr.start_date <= $2::date AND lr.end_date >= $1::date
          GROUP BY lr.intern_id
       ) l ON l.intern_id = i.id
      WHERE ($3::uuid IS NULL OR i.id = $3::uuid)
      ORDER BY i.full_name, i.id`,
    [from, to, filters.internId ?? null],
  );

  response.json({ from, to, items: result.rows });
}));

// This write endpoint provides a single validated source record for the report;
// check-in/out workflows can also persist into attendance_records directly.
sprint3Router.post('/attendance/records', asyncHandler(async (request, response) => {
  const input = attendanceRecordSchema.parse(request.body);
  const intern = await pool.query('SELECT 1 FROM interns WHERE id = $1', [input.internId]);
  if (!intern.rowCount) throw new HttpError(404, 'Không tìm thấy hồ sơ thực tập sinh');
  const result = await pool.query(
    `INSERT INTO attendance_records
       (id, intern_id, work_date, check_in, check_out, scheduled_start, scheduled_end)
     VALUES ($1, $2, $3, $4, $5, $6, $7)
     RETURNING id, intern_id AS "internId", work_date AS "workDate", check_in AS "checkIn",
               check_out AS "checkOut", scheduled_start AS "scheduledStart", scheduled_end AS "scheduledEnd"`,
    [randomUUID(), input.internId, input.workDate, input.checkIn, input.checkOut ?? null,
      input.scheduledStart, input.scheduledEnd],
  );
  response.status(201).json({ data: result.rows[0] });
}));

sprint3Router.get('/leave-requests', asyncHandler(async (request, response) => {
  const filters = leaveRequestQuerySchema.parse(request.query);
  const values: unknown[] = [];
  const where: string[] = [];
  if (filters.internId) {
    values.push(filters.internId);
    where.push(`lr.intern_id = $${values.length}`);
  }
  if (filters.status) {
    values.push(filters.status);
    where.push(`lr.status = $${values.length}`);
  }
  const result = await pool.query(
    `SELECT lr.id, lr.intern_id AS "internId", i.full_name AS "internName", lr.leave_type AS "leaveType",
            lr.start_date AS "startDate", lr.end_date AS "endDate", lr.reason, lr.status,
            lr.feedback, lr.processed_by AS "processedBy", lr.processed_at AS "processedAt",
            lr.created_at AS "createdAt", lr.updated_at AS "updatedAt"
       FROM leave_requests lr JOIN interns i ON i.id = lr.intern_id
       ${where.length ? `WHERE ${where.join(' AND ')}` : ''}
      ORDER BY lr.created_at DESC, lr.id`,
    values,
  );
  response.json({ items: result.rows });
}));

sprint3Router.post('/leave-requests', asyncHandler(async (request, response) => {
  const input = createLeaveRequestSchema.parse(request.body);
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    await requireIntern(client, input.internId);
    const id = randomUUID();
    const result = await client.query(
      `INSERT INTO leave_requests (id, intern_id, leave_type, start_date, end_date, reason)
       VALUES ($1, $2, $3, $4, $5, $6)
       RETURNING id, intern_id AS "internId", leave_type AS "leaveType", start_date AS "startDate",
                 end_date AS "endDate", reason, status, feedback, processed_by AS "processedBy",
                 processed_at AS "processedAt", created_at AS "createdAt"`,
      [id, input.internId, input.leaveType, input.startDate, input.endDate, input.reason],
    );
    await client.query(
      `INSERT INTO leave_request_history
         (id, leave_request_id, previous_status, new_status, feedback, changed_by)
       VALUES ($1, $2, NULL, 'PENDING', '', $3)`,
      [randomUUID(), id, input.internId],
    );
    await client.query('COMMIT');
    response.status(201).json({ data: result.rows[0] });
  } catch (error) {
    await rollback(client);
    throw error;
  } finally {
    client.release();
  }
}));

sprint3Router.put('/leave-requests/:id/status', asyncHandler(async (request, response) => {
  const id = parseId(request.params.id, 'Mã yêu cầu nghỉ không hợp lệ');
  const input = updateLeaveRequestStatusSchema.parse(request.body);
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const current = await client.query<{ status: 'PENDING' | 'APPROVED' | 'REJECTED' }>(
      'SELECT status FROM leave_requests WHERE id = $1 FOR UPDATE', [id],
    );
    const previous = current.rows[0]?.status;
    if (!previous) throw new HttpError(404, 'Không tìm thấy yêu cầu nghỉ');
    if (previous !== 'PENDING') throw new HttpError(409, 'Yêu cầu nghỉ đã được xử lý');
    const result = await client.query(
      `UPDATE leave_requests
          SET status = $2, feedback = $3, processed_by = $4, processed_at = now(), updated_at = now()
        WHERE id = $1
        RETURNING id, intern_id AS "internId", leave_type AS "leaveType", start_date AS "startDate",
                  end_date AS "endDate", reason, status, feedback, processed_by AS "processedBy",
                  processed_at AS "processedAt", updated_at AS "updatedAt"`,
      [id, input.status, input.feedback, input.processedBy],
    );
    await client.query(
      `INSERT INTO leave_request_history
         (id, leave_request_id, previous_status, new_status, feedback, changed_by)
       VALUES ($1, $2, $3, $4, $5, $6)`,
      [randomUUID(), id, previous, input.status, input.feedback, input.processedBy],
    );
    await client.query('COMMIT');
    response.json({ data: result.rows[0] });
  } catch (error) {
    await rollback(client);
    throw error;
  } finally {
    client.release();
  }
}));

sprint3Router.get('/leave-requests/:id/history', asyncHandler(async (request, response) => {
  const id = parseId(request.params.id, 'Mã yêu cầu nghỉ không hợp lệ');
  const result = await pool.query(
    `SELECT previous_status AS "previousStatus", new_status AS "newStatus", feedback,
            changed_by AS "changedBy", changed_at AS "changedAt"
       FROM leave_request_history WHERE leave_request_id = $1 ORDER BY changed_at, id`, [id],
  );
  response.json({ items: result.rows });
}));

sprint3Router.get('/allowances', asyncHandler(async (request, response) => {
  const internId = typeof request.query.internId === 'string' ? request.query.internId : undefined;
  if (internId && !isUuid(internId)) throw new HttpError(400, 'Mã thực tập sinh không hợp lệ');
  const result = await pool.query(
    `SELECT id, intern_id AS "internId", title, amount::text AS amount, currency,
            period_start AS "periodStart", period_end AS "periodEnd", payment_status AS "paymentStatus",
            note, created_at AS "createdAt", updated_at AS "updatedAt"
       FROM allowances WHERE ($1::uuid IS NULL OR intern_id = $1::uuid)
      ORDER BY period_start DESC, title, id`, [internId ?? null],
  );
  response.json({ items: result.rows });
}));

sprint3Router.post('/allowances', asyncHandler(async (request, response) => {
  const input = allowanceInputSchema.parse(request.body);
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    await requireIntern(client, input.internId);
    const id = randomUUID();
    const result = await client.query(
      `INSERT INTO allowances
         (id, intern_id, title, amount, currency, period_start, period_end, note)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
       RETURNING id, intern_id AS "internId", title, amount::text AS amount, currency,
                 period_start AS "periodStart", period_end AS "periodEnd", payment_status AS "paymentStatus",
                 note, created_at AS "createdAt", updated_at AS "updatedAt"`,
      [id, input.internId, input.title, input.amount, input.currency, input.periodStart,
        input.periodEnd, input.note],
    );
    await writeAllowanceHistory(client, {
      allowanceId: id, action: 'CREATED', previous: null, next: result.rows[0],
      changedBy: input.changedBy, feedback: '',
    });
    await client.query('COMMIT');
    response.status(201).json({ data: result.rows[0] });
  } catch (error) {
    await rollback(client);
    throw error;
  } finally {
    client.release();
  }
}));

sprint3Router.put('/allowances/:id', asyncHandler(async (request, response) => {
  const id = parseId(request.params.id, 'Mã phụ cấp không hợp lệ');
  const input = allowanceInputSchema.parse(request.body);
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    await requireIntern(client, input.internId);
    const existing = await loadAllowance(client, id, true);
    if (!existing) throw new HttpError(404, 'Không tìm thấy khoản phụ cấp');
    const result = await client.query(
      `UPDATE allowances
          SET intern_id = $2, title = $3, amount = $4, currency = $5,
              period_start = $6, period_end = $7, note = $8, updated_at = now()
        WHERE id = $1
        RETURNING id, intern_id AS "internId", title, amount::text AS amount, currency,
                  period_start AS "periodStart", period_end AS "periodEnd", payment_status AS "paymentStatus",
                  note, created_at AS "createdAt", updated_at AS "updatedAt"`,
      [id, input.internId, input.title, input.amount, input.currency, input.periodStart,
        input.periodEnd, input.note],
    );
    await writeAllowanceHistory(client, {
      allowanceId: id, action: 'UPDATED', previous: existing, next: result.rows[0],
      changedBy: input.changedBy, feedback: '',
    });
    await client.query('COMMIT');
    response.json({ data: result.rows[0] });
  } catch (error) {
    await rollback(client);
    throw error;
  } finally {
    client.release();
  }
}));

sprint3Router.put('/allowances/:id/payment-status', asyncHandler(async (request, response) => {
  const id = parseId(request.params.id, 'Mã phụ cấp không hợp lệ');
  const input = allowancePaymentSchema.parse(request.body);
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const existing = await loadAllowance(client, id, true);
    if (!existing) throw new HttpError(404, 'Không tìm thấy khoản phụ cấp');
    if (existing.paymentStatus === input.status) {
      await client.query('COMMIT');
      response.json({ data: existing });
      return;
    }
    const result = await client.query(
      `UPDATE allowances SET payment_status = $2, updated_at = now()
        WHERE id = $1
        RETURNING id, intern_id AS "internId", title, amount::text AS amount, currency,
                  period_start AS "periodStart", period_end AS "periodEnd", payment_status AS "paymentStatus",
                  note, created_at AS "createdAt", updated_at AS "updatedAt"`,
      [id, input.status],
    );
    await writeAllowanceHistory(client, {
      allowanceId: id, action: 'PAYMENT_STATUS_CHANGED', previous: existing, next: result.rows[0],
      changedBy: input.changedBy, feedback: input.feedback,
    });
    await client.query('COMMIT');
    response.json({ data: result.rows[0] });
  } catch (error) {
    await rollback(client);
    throw error;
  } finally {
    client.release();
  }
}));

sprint3Router.get('/allowances/summary', asyncHandler(async (request, response) => {
  const filters = allowanceSummaryQuerySchema.parse(request.query);
  const to = filters.to ?? new Date().toISOString().slice(0, 10);
  const from = filters.from ?? `${to.slice(0, 7)}-01`;
  if (from > to) throw new HttpError(400, 'Ngày bắt đầu phải trước hoặc bằng ngày kết thúc');
  const result = await pool.query(
    `SELECT period_start AS "periodStart", period_end AS "periodEnd", payment_status AS "paymentStatus",
            currency, sum(amount)::numeric(14, 2)::text AS total, count(*)::int AS count
       FROM allowances
      WHERE period_start <= $2::date AND period_end >= $1::date
        AND ($3::uuid IS NULL OR intern_id = $3::uuid)
        AND ($4::text IS NULL OR payment_status = $4::text)
      GROUP BY period_start, period_end, payment_status, currency
      ORDER BY period_start, period_end, payment_status, currency`,
    [from, to, filters.internId ?? null, filters.status ?? null],
  );
  response.json({ from, to, items: result.rows });
}));

sprint3Router.get('/allowances/:id/history', asyncHandler(async (request, response) => {
  const id = parseId(request.params.id, 'Mã phụ cấp không hợp lệ');
  const result = await pool.query(
    `SELECT action, previous_values AS "previousValues", new_values AS "newValues", feedback,
            changed_by AS "changedBy", changed_at AS "changedAt"
       FROM allowance_histories WHERE allowance_id = $1 ORDER BY changed_at, id`, [id],
  );
  response.json({ items: result.rows });
}));

sprint3Router.post('/mentors/assignments', asyncHandler(async (request, response) => {
  const input = mentorAssignmentSchema.parse(request.body);
  const data = await saveMentorAssignment(input, null);
  response.status(201).json({ data });
}));

sprint3Router.put('/mentors/assignments/:id', asyncHandler(async (request, response) => {
  const id = parseId(request.params.id, 'Mã phân công không hợp lệ');
  const input = mentorAssignmentSchema.parse(request.body);
  const data = await saveMentorAssignment(input, id);
  response.json({ data });
}));

sprint3Router.get('/mentors/statistics/mentees', asyncHandler(async (_request, response) => {
  const result = await pool.query(
    `SELECT m.id AS "mentorId", m.full_name AS "mentorName", m.department,
            m.max_mentees AS "maxMentees", count(a.id)::int AS "menteeCount",
            greatest(m.max_mentees - count(a.id)::int, 0) AS "availableSlots",
            (count(a.id)::int > m.max_mentees) AS "overCapacity"
       FROM mentors m
       LEFT JOIN mentor_assignments a ON a.mentor_id = m.id AND a.is_active = true
        AND a.start_date <= current_date AND (a.end_date IS NULL OR a.end_date >= current_date)
      GROUP BY m.id, m.full_name, m.department, m.max_mentees
      ORDER BY m.full_name, m.id`,
  );
  response.json({ items: result.rows });
}));

sprint3Router.get('/statistics/schools-majors', asyncHandler(async (request, response) => {
  const filters = schoolsMajorsQuerySchema.parse(request.query);
  const result = await pool.query(
    `SELECT university, major, count(*)::int AS "internCount"
       FROM interns
      WHERE ($1::text IS NULL OR university ILIKE $1)
        AND ($2::text IS NULL OR major ILIKE $2)
      GROUP BY university, major
      ORDER BY university, major`,
    [filters.university ? `%${filters.university}%` : null,
      filters.major ? `%${filters.major}%` : null],
  );
  response.json({ items: result.rows });
}));

async function saveMentorAssignment(
  input: ReturnType<typeof mentorAssignmentSchema.parse>,
  id: string | null,
) {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const mentorResult = await client.query<{ is_active: boolean; max_mentees: number }>(
      'SELECT is_active, max_mentees FROM mentors WHERE id = $1 FOR UPDATE', [input.mentorId],
    );
    const mentor = mentorResult.rows[0];
    if (!mentor) throw new HttpError(404, 'Không tìm thấy mentor');
    if (!mentor.is_active) throw new HttpError(409, 'Mentor đang không hoạt động');
    await requireIntern(client, input.internId);

    const assignmentResult = id
      ? await client.query<{ id: string }>('SELECT id FROM mentor_assignments WHERE id = $1 FOR UPDATE', [id])
      : null;
    if (id && !assignmentResult?.rows[0]) throw new HttpError(404, 'Không tìm thấy phân công mentor');

    const duplicate = await client.query(
      `SELECT 1 FROM mentor_assignments
        WHERE is_active = true AND intern_id = $1
          AND ($2::uuid IS NULL OR id <> $2::uuid)
          AND start_date <= COALESCE($4::date, 'infinity'::date)
          AND COALESCE(end_date, 'infinity'::date) >= $3::date
        LIMIT 1`,
      [input.internId, id, input.startDate, input.endDate ?? null],
    );
    if (duplicate.rowCount) throw new HttpError(409, 'Thực tập sinh đã có mentor trong khoảng thời gian này');

    const capacity = await client.query<{ count: number }>(
      `SELECT count(*)::int AS count FROM mentor_assignments
        WHERE mentor_id = $1 AND is_active = true
          AND ($2::uuid IS NULL OR id <> $2::uuid)
          AND start_date <= COALESCE($4::date, 'infinity'::date)
          AND COALESCE(end_date, 'infinity'::date) >= $3::date`,
      [input.mentorId, id, input.startDate, input.endDate ?? null],
    );
    if (Number(capacity.rows[0]?.count ?? 0) >= mentor.max_mentees) {
      throw new HttpError(409, 'Mentor đã đạt số lượng mentee tối đa');
    }

    const result = id
      ? await client.query(
        `UPDATE mentor_assignments
            SET mentor_id = $2, intern_id = $3, start_date = $4, end_date = $5,
                assigned_by = $6, updated_at = now(), is_active = true
          WHERE id = $1
          RETURNING id, mentor_id AS "mentorId", intern_id AS "internId", start_date AS "startDate",
                    end_date AS "endDate", assigned_by AS "assignedBy", is_active AS "isActive"`,
        [id, input.mentorId, input.internId, input.startDate, input.endDate ?? null, input.assignedBy],
      )
      : await client.query(
        `INSERT INTO mentor_assignments
           (id, mentor_id, intern_id, start_date, end_date, assigned_by)
         VALUES ($1, $2, $3, $4, $5, $6)
         RETURNING id, mentor_id AS "mentorId", intern_id AS "internId", start_date AS "startDate",
                   end_date AS "endDate", assigned_by AS "assignedBy", is_active AS "isActive"`,
        [randomUUID(), input.mentorId, input.internId, input.startDate, input.endDate ?? null, input.assignedBy],
      );
    await client.query('COMMIT');
    return result.rows[0];
  } catch (error) {
    await rollback(client);
    throw error;
  } finally {
    client.release();
  }
}

async function requireIntern(client: PoolClient, internId: string) {
  const result = await client.query('SELECT 1 FROM interns WHERE id = $1', [internId]);
  if (!result.rowCount) throw new HttpError(404, 'Không tìm thấy hồ sơ thực tập sinh');
}

async function loadAllowance(client: PoolClient, id: string, lock: boolean) {
  const result = await client.query(
    `SELECT id, intern_id AS "internId", title, amount::text AS amount, currency,
            period_start AS "periodStart", period_end AS "periodEnd", payment_status AS "paymentStatus",
            note, created_at AS "createdAt", updated_at AS "updatedAt"
       FROM allowances WHERE id = $1 ${lock ? 'FOR UPDATE' : ''}`, [id],
  );
  return result.rows[0] as Record<string, unknown> | undefined;
}

async function writeAllowanceHistory(client: PoolClient, values: {
  allowanceId: string;
  action: 'CREATED' | 'UPDATED' | 'PAYMENT_STATUS_CHANGED';
  previous: unknown;
  next: unknown;
  changedBy: string;
  feedback: string;
}) {
  await client.query(
    `INSERT INTO allowance_histories
       (id, allowance_id, action, previous_values, new_values, feedback, changed_by)
     VALUES ($1, $2, $3, $4::jsonb, $5::jsonb, $6, $7)`,
    [randomUUID(), values.allowanceId, values.action,
      values.previous === null ? null : JSON.stringify(values.previous), JSON.stringify(values.next),
      values.feedback, values.changedBy],
  );
}

async function rollback(client: PoolClient) {
  try {
    await client.query('ROLLBACK');
  } catch (error) {
    console.error('Failed to roll back Sprint 3 transaction:', error);
  }
}

function parseId(value: string | string[] | undefined, message: string) {
  const id = Array.isArray(value) ? value[0] : value;
  if (!id || !isUuid(id)) throw new HttpError(400, message);
  return id;
}

function isUuid(value: string) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}
