-- db/migrations/02_idempotent.sql
ALTER TABLE documents
  ADD COLUMN IF NOT EXISTS content_sha TEXT;

CREATE UNIQUE INDEX IF NOT EXISTS ux_documents_content_sha
  ON documents(content_sha);
