import nodemailer from 'nodemailer';
import { env } from '../config/env.js';
import { pool } from '../db/pool.js';
import { renderDecisionEmail, type DecisionEmailData } from './templates.js';

interface OutboxRow {
  id: string;
  recipient: string;
  template: 'intern_approved' | 'intern_rejected';
  payload: DecisionEmailData;
  attempts: number;
}

const transporter = nodemailer.createTransport({
  host: env.SMTP_HOST,
  port: env.SMTP_PORT,
  secure: env.SMTP_SECURE,
  ...(env.SMTP_USER ? { auth: { user: env.SMTP_USER, pass: env.SMTP_PASSWORD } } : {}),
});

let timer: NodeJS.Timeout | undefined;
let processing = false;

export function startEmailWorker() {
  if (!env.EMAIL_WORKER_ENABLED) return;
  timer = setInterval(() => { void processOutboxBatch(); }, env.EMAIL_WORKER_INTERVAL_MS);
  timer.unref();
  void processOutboxBatch();
}

export async function stopEmailWorker() {
  if (timer) clearInterval(timer);
  if (processing) {
    while (processing) await new Promise((resolve) => setTimeout(resolve, 50));
  }
}

export async function processOutboxBatch(batchSize = 10) {
  if (processing) return;
  processing = true;
  try {
    const jobs = await pool.query<OutboxRow>(
      `WITH picked AS (
         SELECT id FROM email_outbox
         WHERE (status = 'PENDING' AND next_attempt_at <= now())
            OR (status = 'PROCESSING' AND locked_at < now() - interval '5 minutes')
         ORDER BY next_attempt_at, created_at
         FOR UPDATE SKIP LOCKED
         LIMIT $1
       )
       UPDATE email_outbox AS job
       SET status = 'PROCESSING', locked_at = now(), updated_at = now()
       FROM picked WHERE job.id = picked.id
       RETURNING job.id, job.recipient, job.template, job.payload, job.attempts`,
      [batchSize],
    );

    for (const job of jobs.rows) await sendOne(job);
  } catch (error) {
    console.error('Email outbox worker failed:', error);
  } finally {
    processing = false;
  }
}

async function sendOne(job: OutboxRow) {
  try {
    const message = renderDecisionEmail(job.template, job.payload);
    await transporter.sendMail({
      from: env.EMAIL_FROM,
      to: job.recipient,
      subject: message.subject,
      text: message.text,
      html: message.html,
      messageId: `<${job.id}@intern-management.local>`,
    });
    await pool.query(
      `UPDATE email_outbox SET status = 'SENT', sent_at = now(), locked_at = NULL,
         last_error = NULL, updated_at = now() WHERE id = $1`,
      [job.id],
    );
  } catch (error) {
    const attempts = job.attempts + 1;
    const retryDelaySeconds = Math.min(3600, 30 * 2 ** Math.min(attempts - 1, 7));
    const message = error instanceof Error ? error.message.slice(0, 4000) : 'Unknown SMTP error';
    await pool.query(
      `UPDATE email_outbox
       SET attempts = $2,
           status = CASE WHEN $2 >= $3 THEN 'FAILED' ELSE 'PENDING' END,
           next_attempt_at = now() + ($4 * interval '1 second'),
           locked_at = NULL, last_error = $5, updated_at = now()
       WHERE id = $1`,
      [job.id, attempts, env.EMAIL_MAX_ATTEMPTS, retryDelaySeconds, message],
    );
    console.error(`Email job ${job.id} failed (attempt ${attempts}):`, message);
  }
}
