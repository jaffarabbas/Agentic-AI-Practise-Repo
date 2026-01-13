-- DocumentQA Database Schema
-- PostgreSQL with pgvector extension

-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Documents table
CREATE TABLE IF NOT EXISTS documents (
    id UUID PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    filename VARCHAR(500) NOT NULL,
    content_type VARCHAR(100),
    file_size_bytes BIGINT,
    chunk_count INT NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    error_message TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMP,

    CONSTRAINT chk_status CHECK (status IN ('pending', 'processing', 'completed', 'failed'))
);

-- Chunks table with vector embeddings
CREATE TABLE IF NOT EXISTS chunks (
    id UUID PRIMARY KEY,
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    embedding vector(1536) NOT NULL, -- text-embedding-3-small produces 1536 dimensions
    chunk_index INT NOT NULL,
    token_count INT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT unique_chunk UNIQUE (document_id, chunk_index)
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_documents_user_id ON documents(user_id);
CREATE INDEX IF NOT EXISTS idx_documents_status ON documents(status);
CREATE INDEX IF NOT EXISTS idx_documents_created_at ON documents(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_chunks_document_id ON chunks(document_id);

-- HNSW index for fast vector similarity search
CREATE INDEX IF NOT EXISTS idx_chunks_embedding ON chunks
USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

-- Alternative: IVFFlat index (faster build, slower search)
-- CREATE INDEX IF NOT EXISTS idx_chunks_embedding ON chunks
-- USING ivfflat (embedding vector_cosine_ops)
-- WITH (lists = 100);

-- Create a view for document statistics
CREATE OR REPLACE VIEW document_stats AS
SELECT
    d.id,
    d.user_id,
    d.filename,
    d.status,
    d.chunk_count,
    d.file_size_bytes,
    d.created_at,
    d.processed_at,
    COALESCE(AVG(c.token_count), 0) as avg_chunk_tokens,
    EXTRACT(EPOCH FROM (d.processed_at - d.created_at)) as processing_time_seconds
FROM documents d
LEFT JOIN chunks c ON d.id = c.document_id
GROUP BY d.id, d.user_id, d.filename, d.status, d.chunk_count, d.file_size_bytes, d.created_at, d.processed_at;

-- Function to search for similar chunks
CREATE OR REPLACE FUNCTION search_similar_chunks(
    query_embedding vector(1536),
    top_k INT DEFAULT 5,
    min_similarity FLOAT DEFAULT 0.7,
    filter_user_id VARCHAR(255) DEFAULT NULL,
    filter_document_id UUID DEFAULT NULL
)
RETURNS TABLE (
    chunk_id UUID,
    document_id UUID,
    content TEXT,
    similarity FLOAT,
    filename VARCHAR(500)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.id as chunk_id,
        c.document_id,
        c.content,
        1 - (c.embedding <=> query_embedding) as similarity,
        d.filename
    FROM chunks c
    JOIN documents d ON c.document_id = d.id
    WHERE
        (filter_user_id IS NULL OR d.user_id = filter_user_id)
        AND (filter_document_id IS NULL OR c.document_id = filter_document_id)
        AND 1 - (c.embedding <=> query_embedding) >= min_similarity
    ORDER BY c.embedding <=> query_embedding
    LIMIT top_k;
END;
$$ LANGUAGE plpgsql;

-- Sample queries for testing

-- Get all documents for a user
-- SELECT * FROM documents WHERE user_id = 'user123' ORDER BY created_at DESC;

-- Get document statistics
-- SELECT * FROM document_stats WHERE user_id = 'user123';

-- Search for similar chunks (example)
-- SELECT * FROM search_similar_chunks(
--     (SELECT embedding FROM chunks LIMIT 1),  -- Example query embedding
--     5,                                         -- Top 5 results
--     0.7,                                       -- Min similarity 70%
--     'user123',                                 -- Filter by user
--     NULL                                       -- No document filter
-- );

-- Check database size and index usage
-- SELECT
--     pg_size_pretty(pg_total_relation_size('chunks')) as chunks_total_size,
--     pg_size_pretty(pg_relation_size('chunks')) as chunks_table_size,
--     pg_size_pretty(pg_total_relation_size('chunks') - pg_relation_size('chunks')) as chunks_indexes_size;
