import { randomUUID } from 'node:crypto';
import type { Pool } from 'pg';
import { pool } from '../db/pool.js';
import type { CreateInternInput } from '../validation/intern.js';
import type { Intern, InternStatus } from '../types/intern.js';
import { HttpError } from '../utils/http-error.js';
import { enqueueDecisionEmail } from '../email/outbox.js';

interface InternRow {
  id: string;
  full_name: string;
  student_id: string;
  email: string;
  phone: string;
  date_of_birth: string;
  gender: 'male' | 'female' | 'other';
  university: string;
  major: string;
  start_date: string;
  end_date: string;
  department: string | null;
  note: string | null;
  status: InternStatus;
  reviewed_by: string | null;
  reviewed_at: Date | null;
  rejection_reason: string | null;
  created_at: Date;
  updated_at: Date;
}

function toIntern(row: InternRow): Intern {
  return {
    id: row.id,
    fullName: row.full_name,
    studentId: row.student_id,
    email: row.email,
    phone: row.phone,
    dateOfBirth: String(row.date_of_birth).slice(0, 10),
    gender: row.gender,
    university: row.university,
    major: row.major,
    startDate: String(row.start_date).slice(0, 10),
    endDate: String(row.end_date).slice(0, 10),
    department: row.department,
    note: row.note,
    status: row.status,
    reviewedBy: row.reviewed_by,
    reviewedAt: row.reviewed_at?.toISOString() ?? null,
    rejectionReason: row.rejection_reason,
    createdAt: row.created_at.toISOString(),
    updatedAt: row.updated_at.toISOString(),
  };
}

export async function createIntern(input: CreateInternInput): Promise<Intern> {
  const result = await pool.query<InternRow>(
    `INSERT INTO interns (
       id, full_name, student_id, email, phone, date_of_birth, gender,
       university, major, start_date, end_date, department, note
     ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)
     RETURNING *`,
    [randomUUID(), input.fullName, input.studentId, input.email, input.phone, input.dateOfBirth,
      input.gender, input.university, input.major, input.startDate, input.endDate,
      input.department || null, input.note || null],
  );
  const row = result.rows[0];
  if (!row) throw new Error('PostgreSQL did not return the newly created intern');
  return toIntern(row);
}

export interface ListInternFilters {
  search?: string;
  status?: InternStatus;
  page: number;
  limit: number;
}

export async function listInterns(filters: ListInternFilters) {
  const values: string[] = [];
  const where: string[] = [];

  if (filters.status) {
    values.push(filters.status);
    where.push(`status = $${values.length}`);
  }
  if (filters.search) {
    values.push(`%${filters.search}%`);
    const parameter = `$${values.length}`;
    where.push(`(full_name ILIKE ${parameter} OR student_id ILIKE ${parameter} OR email ILIKE ${parameter} OR university ILIKE ${parameter} OR major ILIKE ${parameter})`);
  }

  const whereSql = where.length ? `WHERE ${where.join(' AND ')}` : '';
  const countResult = await pool.query<{ total: string }>(
    `SELECT count(*)::text AS total FROM interns ${whereSql}`,
    values,
  );
  const total = Number(countResult.rows[0]?.total ?? 0);
  const offset = (filters.page - 1) * filters.limit;
  const dataResult = await pool.query<InternRow>(
    `SELECT * FROM interns ${whereSql} ORDER BY created_at DESC, id ASC LIMIT $${values.length + 1} OFFSET $${values.length + 2}`,
    [...values, filters.limit, offset],
  );

  return {
    items: dataResult.rows.map(toIntern),
    pagination: {
      page: filters.page,
      limit: filters.limit,
      total,
      totalPages: Math.ceil(total / filters.limit),
    },
  };
}

export async function reviewInternApplication(input: {
  internId: string;
  decision: 'APPROVED' | 'REJECTED';
  reviewerId: string;
  rejectionReason?: string;
}, database: Pick<Pool, 'connect'> = pool): Promise<Intern> {
  if (input.decision === 'REJECTED' && !input.rejectionReason?.trim()) {
    throw new HttpError(400, 'Lý do từ chối là bắt buộc');
  }
  const client = await database.connect();
  try {
    await client.query('BEGIN');
    try {
      const result = await client.query<InternRow>(
        `UPDATE interns
           SET status = $2, reviewed_by = $3, reviewed_at = now(),
               rejection_reason = $4, updated_at = now()
         WHERE id = $1 AND status = 'PENDING'
         RETURNING *`,
        [input.internId, input.decision, input.reviewerId,
          input.decision === 'REJECTED' ? input.rejectionReason?.trim() : null],
      );
      const row = result.rows[0];
      if (!row) {
        const exists = await client.query('SELECT 1 FROM interns WHERE id = $1', [input.internId]);
        if (!exists.rowCount) throw new HttpError(404, 'Không tìm thấy hồ sơ thực tập sinh');
        throw new HttpError(409, 'Hồ sơ đã được xử lý');
      }

      const intern = toIntern(row);
      await enqueueDecisionEmail({
        internId: intern.id,
        recipient: intern.email,
        decision: input.decision,
        idempotencyKey: 'initial-review',
        data: {
          fullName: intern.fullName,
          ...(input.decision === 'REJECTED' ? { rejectionReason: intern.rejectionReason ?? undefined } : {}),
        },
      }, client);

      await client.query('COMMIT');
      return intern;
    } catch (error) {
      try {
        await client.query('ROLLBACK');
      } catch (rollbackError) {
        console.error('Failed to roll back intern review transaction:', rollbackError);
      }
      throw error;
    }
  } finally {
    client.release();
  }
}
