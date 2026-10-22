import type { ErrorRequestHandler } from 'express';
import { ZodError } from 'zod';
import { HttpError } from '../utils/http-error.js';

export const errorHandler: ErrorRequestHandler = (error, _request, response, _next) => {
  if (error instanceof ZodError) {
    response.status(400).json({
      error: 'Dữ liệu không hợp lệ',
      details: error.issues.map((issue) => ({ field: issue.path.join('.'), message: issue.message })),
    });
    return;
  }

  if (error instanceof HttpError) {
    response.status(error.statusCode).json({ error: error.message, ...(error.details ? { details: error.details } : {}) });
    return;
  }

  if (isPostgresUniqueViolation(error)) {
    const constraint = error.constraint ?? '';
    if (constraint.includes('student_id')) {
      response.status(409).json({ error: 'Mã sinh viên đã được sử dụng', field: 'studentId' });
      return;
    }
    if (constraint.includes('email')) {
      response.status(409).json({ error: 'Email đã được sử dụng', field: 'email' });
      return;
    }
    response.status(409).json({ error: 'Dữ liệu bị trùng lặp' });
    return;
  }

  console.error(error);
  response.status(500).json({ error: 'Đã xảy ra lỗi máy chủ' });
};

function isPostgresUniqueViolation(error: unknown): error is { code: string; constraint?: string } {
  return typeof error === 'object' && error !== null && 'code' in error && error.code === '23505';
}
