import { randomUUID } from 'node:crypto';
import { pool } from '../db/pool.js';
import type { DecisionEmailData } from './templates.js';

export async function enqueueDecisionEmail(input: {
  internId: string;
  recipient: string;
  decision: 'APPROVED' | 'REJECTED';
  idempotencyKey: string;
  data: DecisionEmailData;
}) {
  const template = input.decision === 'APPROVED' ? 'intern_approved' : 'intern_rejected';
  const dedupeKey = `intern-decision:${input.internId}:${input.decision}:${input.idempotencyKey}`;
  const inserted = await pool.query<{ id: string }>(
    `INSERT INTO email_outbox (id, dedupe_key, recipient, template, payload)
     VALUES ($1, $2, $3, $4, $5::jsonb)
     ON CONFLICT (dedupe_key) DO NOTHING
     RETURNING id`,
    [randomUUID(), dedupeKey, input.recipient, template, JSON.stringify(input.data)],
  );
  if (inserted.rows[0]) return { id: inserted.rows[0].id, queued: true };

  const existing = await pool.query<{ id: string }>('SELECT id FROM email_outbox WHERE dedupe_key = $1', [dedupeKey]);
  return { id: existing.rows[0]?.id, queued: false };
}
