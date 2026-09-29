import { randomUUID } from 'node:crypto';
import { pool } from '../db/pool.js';
import { HttpError } from '../utils/http-error.js';

export async function recordContractUpload(input: {
  internId: string;
  originalFilename: string;
  storageKey: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedBy: string;
}) {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    const intern = await client.query('SELECT id FROM interns WHERE id = $1 FOR UPDATE', [input.internId]);
    if (!intern.rowCount) throw new HttpError(404, 'Không tìm thấy hồ sơ thực tập sinh');

    const versionResult = await client.query<{ version: number }>(
      'SELECT COALESCE(MAX(version), 0) + 1 AS version FROM intern_contracts WHERE intern_id = $1',
      [input.internId],
    );
    const version = versionResult.rows[0]?.version;
    if (!version) throw new Error('Could not allocate contract version');

    const inserted = await client.query(
      `INSERT INTO intern_contracts (
         id, intern_id, version, original_filename, storage_key,
         content_type, file_size_bytes, uploaded_by
       ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
       RETURNING id, intern_id, version, original_filename, storage_key,
                 content_type, file_size_bytes, uploaded_by, uploaded_at`,
      [randomUUID(), input.internId, version, input.originalFilename, input.storageKey,
        input.contentType, input.fileSizeBytes, input.uploadedBy],
    );
    await client.query('COMMIT');
    return inserted.rows[0];
  } catch (error) {
    await client.query('ROLLBACK');
    throw error;
  } finally {
    client.release();
  }
}
