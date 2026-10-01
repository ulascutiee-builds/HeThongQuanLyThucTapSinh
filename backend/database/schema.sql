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
