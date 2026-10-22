import assert from 'node:assert/strict';
import test from 'node:test';

process.env.DATABASE_URL ??= 'postgresql://test:test@localhost:5432/intern_management_test';

const { enqueueDecisionEmail } = await import('../dist/email/outbox.js');
const { renderDecisionEmail } = await import('../dist/email/templates.js');
const { reviewInternApplication } = await import('../dist/services/intern-service.js');

test('queues the approval template for the intern email address', async () => {
  const calls = [];
  const executor = {
    query: async (sql, values) => {
      calls.push({ sql, values });
      return { rows: [{ id: 'outbox-1' }] };
    },
  };

  const result = await enqueueDecisionEmail({
    internId: 'intern-1',
    recipient: 'intern@example.com',
    decision: 'APPROVED',
    idempotencyKey: 'initial-review',
    data: { fullName: 'Nguyen Van A' },
  }, executor);

  assert.deepEqual(result, { id: 'outbox-1', queued: true });
  assert.equal(calls.length, 1);
  assert.match(calls[0].sql, /INSERT INTO email_outbox/);
  assert.equal(calls[0].values[2], 'intern@example.com');
  assert.equal(calls[0].values[3], 'intern_approved');
  assert.deepEqual(JSON.parse(calls[0].values[4]), { fullName: 'Nguyen Van A' });
});

test('returns the existing outbox job for a duplicate review event', async () => {
  let queryCount = 0;
  const executor = {
    query: async () => {
      queryCount += 1;
      return queryCount === 1 ? { rows: [] } : { rows: [{ id: 'outbox-existing' }] };
    },
  };

  const result = await enqueueDecisionEmail({
    internId: 'intern-1',
    recipient: 'intern@example.com',
    decision: 'REJECTED',
    idempotencyKey: 'initial-review',
    data: { fullName: 'Nguyen Van A', rejectionReason: 'Thiếu hồ sơ' },
  }, executor);

  assert.deepEqual(result, { id: 'outbox-existing', queued: false });
  assert.equal(queryCount, 2);
});

test('renders safe HTML and includes the rejection reason in plain text', () => {
  const email = renderDecisionEmail('intern_rejected', {
    fullName: '<script>alert(1)</script>',
    rejectionReason: '<thiếu giấy tờ>',
  });

  assert.match(email.html, /&lt;script&gt;/);
  assert.doesNotMatch(email.html, /<script>/);
  assert.match(email.text, /Lý do: <thiếu giấy tờ>/);
});

function createReviewDatabase({ failOnOutboxInsert = false } = {}) {
  const calls = [];
  let released = false;
  const client = {
    query: async (sql, values) => {
      calls.push({ sql, values });
      if (sql === 'BEGIN' || sql === 'COMMIT' || sql === 'ROLLBACK') return { rows: [] };
      if (sql.startsWith('UPDATE interns')) {
        return {
          rows: [{
            id: values[0],
            full_name: 'Nguyen Van A',
            student_id: 'SV001',
            email: 'intern@example.com',
            phone: '0912345678',
            date_of_birth: '2003-01-15',
            gender: 'male',
            university: 'ICTU',
            major: 'Software Engineering',
            start_date: '2026-10-01',
            end_date: '2026-12-31',
            department: null,
            note: null,
            status: values[1],
            reviewed_by: values[2],
            reviewed_at: new Date('2026-10-05T00:00:00Z'),
            rejection_reason: values[3],
            created_at: new Date('2026-09-01T00:00:00Z'),
            updated_at: new Date('2026-10-05T00:00:00Z'),
          }],
        };
      }
      if (sql.startsWith('INSERT INTO email_outbox')) {
        if (failOnOutboxInsert) throw new Error('outbox insert unavailable');
        return { rows: [{ id: 'outbox-1' }] };
      }
      throw new Error(`Unexpected query: ${sql}`);
    },
    release: () => { released = true; },
  };

  return {
    database: { connect: async () => client },
    calls,
    wasReleased: () => released,
  };
}

test('review commits only after queuing the intern decision email', async () => {
  const database = createReviewDatabase();
  const reviewed = await reviewInternApplication({
    internId: 'intern-1',
    decision: 'APPROVED',
    reviewerId: 'reviewer-1',
  }, database.database);

  assert.equal(reviewed.status, 'APPROVED');
  const insertIndex = database.calls.findIndex(({ sql }) => sql.startsWith('INSERT INTO email_outbox'));
  const commitIndex = database.calls.findIndex(({ sql }) => sql === 'COMMIT');
  assert.ok(insertIndex > 0);
  assert.ok(commitIndex > insertIndex);
  assert.equal(database.calls[insertIndex].values[2], 'intern@example.com');
  assert.equal(database.calls[insertIndex].values[3], 'intern_approved');
  assert.equal(database.wasReleased(), true);
});

test('rolls back the review when the decision email cannot be queued', async () => {
  const database = createReviewDatabase({ failOnOutboxInsert: true });

  await assert.rejects(() => reviewInternApplication({
    internId: 'intern-1',
    decision: 'REJECTED',
    reviewerId: 'reviewer-1',
    rejectionReason: 'Thiếu hồ sơ',
  }, database.database), /outbox insert unavailable/);

  assert.ok(database.calls.some(({ sql }) => sql === 'ROLLBACK'));
  assert.equal(database.calls.some(({ sql }) => sql === 'COMMIT'), false);
  assert.equal(database.wasReleased(), true);
});
