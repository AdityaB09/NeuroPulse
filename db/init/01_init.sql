CREATE EXTENSION IF NOT EXISTS vector;      -- pgvector
CREATE EXTENSION IF NOT EXISTS pg_trgm;     -- for fuzzy text (optional)
CREATE EXTENSION IF NOT EXISTS btree_gin;   -- for fast filtering (optional)

-- =========================
-- Core docs
-- =========================
CREATE TABLE IF NOT EXISTS documents (
  id           UUID PRIMARY KEY,
  source       TEXT        NOT NULL,
  content      TEXT        NOT NULL,
  content_sha  TEXT        NOT NULL,        -- idempotency key
  metadata     JSONB       DEFAULT '{}'::jsonb,
  embedding    vector(768),
  bm25         tsvector,                    -- full-text index
  created_at   TIMESTAMPTZ DEFAULT now()
);

-- Idempotent ingest: skip duplicates
CREATE UNIQUE INDEX IF NOT EXISTS uq_documents_content_sha ON documents (content_sha);

-- ANN + text indexes
CREATE INDEX IF NOT EXISTS idx_documents_embedding
  ON documents USING ivfflat (embedding vector_l2_ops) WITH (lists = 100);
CREATE INDEX IF NOT EXISTS idx_documents_bm25 ON documents USING GIN (bm25);
CREATE INDEX IF NOT EXISTS idx_documents_source ON documents (source);

-- Keep bm25 in sync
CREATE OR REPLACE FUNCTION documents_bm25_update() RETURNS trigger AS $$
BEGIN
  NEW.bm25 := to_tsvector('english', coalesce(NEW.content,''));
  RETURN NEW;
END $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_documents_bm25 ON documents;
CREATE TRIGGER trg_documents_bm25
BEFORE INSERT OR UPDATE OF content ON documents
FOR EACH ROW EXECUTE FUNCTION documents_bm25_update();

-- =========================
-- Chunked text (long files)
-- =========================
CREATE TABLE IF NOT EXISTS chunks (
  id          UUID PRIMARY KEY,
  doc_id      UUID        NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
  chunk_id    INT         NOT NULL,         -- 0..N
  content     TEXT        NOT NULL,
  metadata    JSONB       DEFAULT '{}'::jsonb,
  embedding   vector(768),
  bm25        tsvector,
  created_at  TIMESTAMPTZ DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_chunks_doc_chunk ON chunks(doc_id, chunk_id);
CREATE INDEX IF NOT EXISTS idx_chunks_embedding ON chunks USING ivfflat (embedding vector_l2_ops) WITH (lists = 100);
CREATE INDEX IF NOT EXISTS idx_chunks_bm25 ON chunks USING GIN (bm25);

CREATE OR REPLACE FUNCTION chunks_bm25_update() RETURNS trigger AS $$
BEGIN
  NEW.bm25 := to_tsvector('english', coalesce(NEW.content,''));
  RETURN NEW;
END $$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_chunks_bm25 ON chunks;
CREATE TRIGGER trg_chunks_bm25
BEFORE INSERT OR UPDATE OF content ON chunks
FOR EACH ROW EXECUTE FUNCTION chunks_bm25_update();
