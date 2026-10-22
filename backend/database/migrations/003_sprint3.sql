-- Apply once to a database that already has the base schema from schema.sql.

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
