-- Apply once to an empty PostgreSQL database, for example:
-- psql "$DATABASE_URL" -f database/schema.sql

CREATE TABLE IF NOT EXISTS interns (
  id uuid PRIMARY KEY,
  full_name varchar(150) NOT NULL,
  student_id varchar(50) NOT NULL UNIQUE,
  email varchar(254) NOT NULL,
  phone varchar(10) NOT NULL,
  date_of_birth date NOT NULL,
  gender varchar(10) NOT NULL CHECK (gender IN ('male', 'female', 'other')),
  university varchar(200) NOT NULL,
  major varchar(200) NOT NULL,
  start_date date NOT NULL,
  end_date date NOT NULL,
  department varchar(150),
  note varchar(2000),
  status varchar(10) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),
  reviewed_by varchar(100),
  reviewed_at timestamptz,
  rejection_reason text,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT interns_date_range_check CHECK (end_date > start_date),
  CONSTRAINT interns_review_state_check CHECK (
    (status = 'PENDING' AND reviewed_at IS NULL AND rejection_reason IS NULL)
    OR (status = 'APPROVED' AND reviewed_at IS NOT NULL AND rejection_reason IS NULL)
    OR (status = 'REJECTED' AND reviewed_at IS NOT NULL AND rejection_reason IS NOT NULL)
  )
);

CREATE UNIQUE INDEX IF NOT EXISTS interns_email_lower_unique ON interns (lower(email));
CREATE INDEX IF NOT EXISTS interns_created_at_idx ON interns (created_at DESC);
CREATE INDEX IF NOT EXISTS interns_status_created_at_idx ON interns (status, created_at DESC);

CREATE TABLE IF NOT EXISTS intern_documents (
  id uuid PRIMARY KEY,
  intern_id uuid NOT NULL REFERENCES interns(id) ON DELETE CASCADE,
  document_type varchar(80) NOT NULL,
  original_filename varchar(255) NOT NULL,
  storage_key text NOT NULL,
  uploaded_by varchar(100) NOT NULL,
  uploaded_at timestamptz NOT NULL DEFAULT now(),
  status varchar(10) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),
  reviewed_by varchar(100),
  reviewed_at timestamptz,
  rejection_reason text,
  CONSTRAINT intern_documents_review_state_check CHECK (
    (status = 'PENDING' AND reviewed_at IS NULL AND rejection_reason IS NULL)
    OR (status = 'APPROVED' AND reviewed_at IS NOT NULL AND rejection_reason IS NULL)
    OR (status = 'REJECTED' AND reviewed_at IS NOT NULL AND rejection_reason IS NOT NULL)
  )
);

CREATE INDEX IF NOT EXISTS intern_documents_intern_idx ON intern_documents (intern_id, uploaded_at DESC);

CREATE TABLE IF NOT EXISTS intern_contracts (
  id uuid PRIMARY KEY,
  intern_id uuid NOT NULL REFERENCES interns(id) ON DELETE CASCADE,
  version integer NOT NULL CHECK (version > 0),
  original_filename varchar(255) NOT NULL,
  storage_key text NOT NULL,
  content_type varchar(150) NOT NULL,
  file_size_bytes bigint NOT NULL CHECK (file_size_bytes > 0),
  uploaded_by varchar(100) NOT NULL,
  uploaded_at timestamptz NOT NULL DEFAULT now(),
  confirmed_at timestamptz,
  confirmed_by varchar(100),
  UNIQUE (intern_id, version)
);

CREATE INDEX IF NOT EXISTS intern_contracts_intern_idx ON intern_contracts (intern_id, version DESC);

CREATE TABLE IF NOT EXISTS email_outbox (
  id uuid PRIMARY KEY,
  dedupe_key varchar(300) NOT NULL UNIQUE,
  recipient varchar(254) NOT NULL,
  template varchar(40) NOT NULL CHECK (template IN ('intern_approved', 'intern_rejected')),
  payload jsonb NOT NULL,
  status varchar(12) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'PROCESSING', 'SENT', 'FAILED')),
  attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
  next_attempt_at timestamptz NOT NULL DEFAULT now(),
  locked_at timestamptz,
  sent_at timestamptz,
  last_error text,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS email_outbox_ready_idx ON email_outbox (next_attempt_at, created_at)
  WHERE status IN ('PENDING', 'PROCESSING');

CREATE TABLE IF NOT EXISTS attendance_records (
  id uuid PRIMARY KEY,
  intern_id uuid NOT NULL REFERENCES interns(id) ON DELETE CASCADE,
  work_date date NOT NULL,
  check_in timestamptz NOT NULL,
  check_out timestamptz,
  scheduled_start time NOT NULL,
  scheduled_end time NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT attendance_records_shift_check CHECK (scheduled_end > scheduled_start),
  CONSTRAINT attendance_records_checkout_check CHECK (check_out IS NULL OR check_out >= check_in),
  UNIQUE (intern_id, work_date)
);

CREATE INDEX IF NOT EXISTS attendance_records_date_intern_idx ON attendance_records (work_date, intern_id);

CREATE TABLE IF NOT EXISTS leave_requests (
  id uuid PRIMARY KEY,
  intern_id uuid NOT NULL REFERENCES interns(id) ON DELETE CASCADE,
  leave_type varchar(80) NOT NULL,
  start_date date NOT NULL,
  end_date date NOT NULL,
  reason varchar(2000) NOT NULL,
  status varchar(10) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),
  feedback varchar(2000),
  processed_by varchar(100),
  processed_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT leave_requests_date_range_check CHECK (end_date >= start_date),
  CONSTRAINT leave_requests_processing_check CHECK (
    (status = 'PENDING' AND processed_by IS NULL AND processed_at IS NULL)
    OR (status IN ('APPROVED', 'REJECTED') AND processed_by IS NOT NULL AND processed_at IS NOT NULL)
  )
);

CREATE INDEX IF NOT EXISTS leave_requests_intern_status_dates_idx
  ON leave_requests (intern_id, status, start_date, end_date);

CREATE TABLE IF NOT EXISTS leave_request_history (
  id uuid PRIMARY KEY,
  leave_request_id uuid NOT NULL REFERENCES leave_requests(id) ON DELETE CASCADE,
  previous_status varchar(10) CHECK (previous_status IN ('PENDING', 'APPROVED', 'REJECTED')),
  new_status varchar(10) NOT NULL CHECK (new_status IN ('PENDING', 'APPROVED', 'REJECTED')),
  feedback varchar(2000) NOT NULL DEFAULT '',
  changed_by varchar(100) NOT NULL,
  changed_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS leave_request_history_request_time_idx
  ON leave_request_history (leave_request_id, changed_at DESC);

CREATE TABLE IF NOT EXISTS allowances (
  id uuid PRIMARY KEY,
  intern_id uuid NOT NULL REFERENCES interns(id) ON DELETE CASCADE,
  title varchar(150) NOT NULL,
  amount numeric(14, 2) NOT NULL CHECK (amount >= 0),
  currency char(3) NOT NULL CHECK (currency ~ '^[A-Z]{3}$'),
  period_start date NOT NULL,
  period_end date NOT NULL,
  payment_status varchar(10) NOT NULL DEFAULT 'UNPAID' CHECK (payment_status IN ('UNPAID', 'PAID')),
  note varchar(2000) NOT NULL DEFAULT '',
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT allowances_period_check CHECK (period_end >= period_start)
);

CREATE INDEX IF NOT EXISTS allowances_intern_period_status_idx
  ON allowances (intern_id, period_start, period_end, payment_status);

CREATE TABLE IF NOT EXISTS allowance_histories (
  id uuid PRIMARY KEY,
  allowance_id uuid NOT NULL REFERENCES allowances(id) ON DELETE CASCADE,
  action varchar(20) NOT NULL CHECK (action IN ('CREATED', 'UPDATED', 'PAYMENT_STATUS_CHANGED')),
  previous_values jsonb,
  new_values jsonb NOT NULL,
  feedback varchar(2000) NOT NULL DEFAULT '',
  changed_by varchar(100) NOT NULL,
  changed_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS allowance_histories_allowance_time_idx
  ON allowance_histories (allowance_id, changed_at DESC);

CREATE TABLE IF NOT EXISTS mentors (
  id uuid PRIMARY KEY,
  full_name varchar(150) NOT NULL,
  email varchar(254) NOT NULL,
  department varchar(150),
  is_active boolean NOT NULL DEFAULT true,
  max_mentees integer NOT NULL DEFAULT 5 CHECK (max_mentees >= 0),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS mentors_email_lower_unique ON mentors (lower(email));

CREATE TABLE IF NOT EXISTS mentor_assignments (
  id uuid PRIMARY KEY,
  mentor_id uuid NOT NULL REFERENCES mentors(id),
  intern_id uuid NOT NULL REFERENCES interns(id) ON DELETE CASCADE,
  start_date date NOT NULL,
  end_date date,
  is_active boolean NOT NULL DEFAULT true,
  assigned_by varchar(100) NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT mentor_assignments_period_check CHECK (end_date IS NULL OR end_date >= start_date)
);

CREATE INDEX IF NOT EXISTS mentor_assignments_mentor_dates_idx
  ON mentor_assignments (mentor_id, is_active, start_date, end_date);
CREATE INDEX IF NOT EXISTS mentor_assignments_intern_dates_idx
  ON mentor_assignments (intern_id, is_active, start_date, end_date);
