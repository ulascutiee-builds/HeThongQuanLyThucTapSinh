# Backend API

REST API for the intern management system, built with Node.js, TypeScript, Express, and PostgreSQL.

## Requirements and setup

- Node.js 20 or newer
- PostgreSQL 14 or newer

From this directory:

```powershell
npm install
Copy-Item .env.example .env
```

Set `DATABASE_URL` in `.env`, create the database, then apply the initial schema:

```powershell
createdb intern_management
psql "$env:DATABASE_URL" -f database/schema.sql
```

Start the API in development mode:

```powershell
npm run dev
```

Build and run the production JavaScript:

```powershell
npm run build
npm start
```

`GET /health` checks that the API can reach PostgreSQL. The API listens on port 3000 by default.

## API

### Create an intern application

`POST /api/interns`

```json
{
  "fullName": "Nguyen Van A",
  "studentId": "SV001",
  "email": "a@example.com",
  "phone": "0912345678",
  "dateOfBirth": "2003-01-15",
  "gender": "male",
  "university": "CodeGym",
  "major": "Software Development",
  "startDate": "2026-10-01",
  "endDate": "2026-12-31",
  "department": "Backend",
  "note": ""
}
```

Required values are validated, the phone number must be a 10-digit Vietnamese mobile number, the end date must follow the start date, and email is normalized to lowercase. Email (case-insensitive) and student ID duplicates return `409 Conflict`. Successful creation returns `201` with `{ "data": ... }`; new applications start with status `PENDING`.

### List and search

`GET /api/interns?search=nguyen&status=PENDING&page=1&limit=20`

Search checks full name, student ID, email, university, and major. `status` is optional; `page` defaults to 1 and `limit` to 20 (maximum 100). Response:

```json
{
  "items": [],
  "pagination": { "page": 1, "limit": 20, "total": 0, "totalPages": 0 }
}
```

## Sprint services for other backend routes to call

These services persist the assigned review and contract/email metadata; the review, authentication, and file-upload routes can call them without competing route definitions:

- `reviewInternApplication(...)` records the approved/rejected decision and atomically queues the matching email in the same PostgreSQL transaction. If the email cannot be queued, the review is rolled back so the intern is not left with a decision but no notification.
- `reviewInternDocument(...)` records document status, reviewer, time, and required rejection reason.
- `saveInternDocument(...)` stores document upload metadata after the file is saved by the upload layer.
- `recordContractUpload(...)` allocates the next contract version per intern and stores uploader, storage key, content type, file size, and upload time. The caller must save the file first and pass its storage key.
- The review service uses `enqueueDecisionEmail(...)` with a stable idempotency key, so repeated handling of the same decision cannot create duplicate outbox jobs.

The email worker is disabled by default. To enable delivery, configure `SMTP_HOST`, `SMTP_PORT`, `SMTP_SECURE`, `SMTP_USER`, `SMTP_PASSWORD`, and a real `EMAIL_FROM`, then set `EMAIL_WORKER_ENABLED=true`. The review route must call `reviewInternApplication(...)`; this branch does not add a review route or authentication policy. The worker records status and errors, retries with exponential backoff, and marks exhausted jobs `FAILED`. SMTP is at-least-once: a process crash after the SMTP server accepts a message but before the database records `SENT` can still cause a retry.

`passwordSchema` in `src/validation/intern.ts` is a reusable baseline for the account-registration task. Passwords are not stored on the intern profile; the authentication owner should hash passwords and own the registration route.

## Current integration boundary

The initial schema is owned by this backend implementation because the repository's `main` branch had no server or database schema. Before combining with a teammate's database work, align table and column names and merge rather than applying this schema over an existing database. Approval, authentication, and actual file-transfer endpoints remain with their assigned owners; the services above provide their persistence hooks.

## Sprint 3: attendance, leave, allowances, and mentor statistics

The Sprint 3 tables are included in `database/schema.sql`. For a database already created from the earlier schema, apply `database/migrations/003_sprint3.sql` once before starting these endpoints.

- `GET /api/attendance/summary?from=YYYY-MM-DD&to=YYYY-MM-DD&internId=<uuid>` returns workdays, late days, early-leave days, and approved leave days per intern. `from` and `to` default to the current month and today. `POST /api/attendance/records` stores a validated source record with its scheduled shift times.
- `POST /api/leave-requests` accepts `{ "internId": "<uuid>", "leaveType": "...", "startDate": "YYYY-MM-DD", "endDate": "YYYY-MM-DD", "reason": "..." }`. `GET /api/leave-requests` accepts optional `internId` and `status` filters.
- `PUT /api/leave-requests/{id}/status` accepts `{ "status": "APPROVED" | "REJECTED", "feedback": "...", "processedBy": "..." }`. Only pending requests can be processed. The processor, timestamp, feedback, and status history are persisted together.
- `GET/POST /api/allowances` and `PUT /api/allowances/{id}` manage an intern's allowance. Writes accept `internId`, `title`, non-negative `amount`, ISO 4217 `currency`, `periodStart`, `periodEnd`, optional `note`, and `changedBy`. Amounts, currency codes, and date periods are validated, and each write creates an audit record.
- `PUT /api/allowances/{id}/payment-status` accepts `{ "status": "UNPAID" | "PAID", "feedback": "...", "changedBy": "..." }`. `GET /api/allowances/summary` groups totals by period, payment status, and currency; `GET /api/allowances/{id}/history` returns the audit trail.
- `POST /api/mentors/assignments` and `PUT /api/mentors/assignments/{id}` accept `mentorId`, `internId`, `startDate`, optional `endDate`, and `assignedBy`. They reject inactive/missing mentors, duplicate overlapping intern assignments, and assignments over mentor capacity.
- `GET /api/mentors/statistics/mentees` returns mentee counts and remaining capacity per mentor. `GET /api/statistics/schools-majors` returns intern counts grouped by university and major, with optional `university` and `major` filters.

These endpoints follow the current integration boundary and do not add an authentication middleware. In deployment, restrict them to the authenticated application/gateway and populate `processedBy`, `changedBy`, and `assignedBy` from its verified identity rather than accepting arbitrary client values.
