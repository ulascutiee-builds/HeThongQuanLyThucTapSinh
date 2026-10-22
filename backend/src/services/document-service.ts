import { randomUUID } from 'node:crypto';
import { pool } from '../db/pool.js';
import { HttpError } from '../utils/http-error.js';

export async function reviewInternDocument(input: {
  documentId: string;
  decision: 'APPROVED' | 'REJECTED';
  reviewerId: string;
  rejectionReason?: string;
}) {
  if (input.decision === 'REJECTED' && !input.rejectionReason?.trim()) {
    throw new HttpError(400, 'Lý do từ chối tài liệu là bắt buộc');
  }

  const result = await pool.query(
    `UPDATE intern_documents
       SET status = $2, reviewed_by = $3, reviewed_at = now(), rejection_reason = $4
     WHERE id = $1 AND status = 'PENDING'
     RETURNING id, intern_id, document_type, status, reviewed_by, reviewed_at, rejection_reason`,
    [input.documentId, input.decision, input.reviewerId,
      input.decision === 'REJECTED' ? input.rejectionReason?.trim() : null],
  );
  if (result.rows[0]) return result.rows[0];

  const exists = await pool.query('SELECT 1 FROM intern_documents WHERE id = $1', [input.documentId]);
  if (!exists.rowCount) throw new HttpError(404, 'Không tìm thấy tài liệu');
  throw new HttpError(409, 'Tài liệu đã được xử lý');
}

export async function saveInternDocument(input: {
  internId: string;
  documentType: string;
  originalFilename: string;
  storageKey: string;
  uploadedBy: string;
}) {
  const result = await pool.query(
    `INSERT INTO intern_documents (id, intern_id, document_type, original_filename, storage_key, uploaded_by)
     VALUES ($1, $2, $3, $4, $5, $6)
     RETURNING id, intern_id, document_type, original_filename, storage_key, uploaded_by, uploaded_at, status`,
    [randomUUID(), input.internId, input.documentType, input.originalFilename, input.storageKey, input.uploadedBy],
  );
  return result.rows[0];
}
