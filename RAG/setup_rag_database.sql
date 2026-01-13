-- ============================================
-- RAG Database Setup with Vector Support
-- ============================================

-- Create the 'rag' database (run this separately first)
-- CREATE DATABASE rag;

-- Connect to the rag database before running the rest
-- \c rag;

-- ============================================
-- Enable pgvector extension for vector support
-- ============================================
CREATE EXTENSION IF NOT EXISTS vector;

-- ============================================
-- Create documents table
-- ============================================
CREATE TABLE IF NOT EXISTS documents (
    id SERIAL PRIMARY KEY,
    content TEXT NOT NULL,
    metadata JSONB,
    source VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- Create document_chunks table with vector embeddings
-- ============================================
CREATE TABLE IF NOT EXISTS document_chunks (
    id SERIAL PRIMARY KEY,
    document_id INTEGER REFERENCES documents(id) ON DELETE CASCADE,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB,
    -- Vector embedding column (384 dimensions for all-MiniLM-L6-v2)
    -- Adjust dimension based on your embedding model
    embedding vector(384),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_chunk UNIQUE (document_id, chunk_index)
);

-- ============================================
-- Create indexes for better performance
-- ============================================

-- Index on document_id for faster joins
CREATE INDEX IF NOT EXISTS idx_chunks_document_id
ON document_chunks(document_id);

-- Index on metadata for JSON queries
CREATE INDEX IF NOT EXISTS idx_documents_metadata
ON documents USING GIN (metadata);

CREATE INDEX IF NOT EXISTS idx_chunks_metadata
ON document_chunks USING GIN (metadata);

-- Vector similarity search index (HNSW - Hierarchical Navigable Small World)
-- This enables fast approximate nearest neighbor search
CREATE INDEX IF NOT EXISTS idx_chunks_embedding
ON document_chunks USING hnsw (embedding vector_cosine_ops);

-- Alternative: IVFFlat index (faster build time, slightly slower search)
-- CREATE INDEX IF NOT EXISTS idx_chunks_embedding
-- ON document_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- ============================================
-- Create function to update updated_at timestamp
-- ============================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Create triggers for auto-updating timestamps
-- ============================================
CREATE TRIGGER update_documents_updated_at
    BEFORE UPDATE ON documents
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- Create function for vector similarity search
-- ============================================
CREATE OR REPLACE FUNCTION search_similar_chunks(
    query_embedding vector(384),
    match_threshold FLOAT DEFAULT 0.5,
    match_count INT DEFAULT 5
)
RETURNS TABLE (
    id INTEGER,
    document_id INTEGER,
    content TEXT,
    metadata JSONB,
    similarity FLOAT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        dc.id,
        dc.document_id,
        dc.content,
        dc.metadata,
        1 - (dc.embedding <=> query_embedding) AS similarity
    FROM document_chunks dc
    WHERE 1 - (dc.embedding <=> query_embedding) > match_threshold
    ORDER BY dc.embedding <=> query_embedding
    LIMIT match_count;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Create view for document statistics
-- ============================================
CREATE OR REPLACE VIEW document_stats AS
SELECT
    d.id AS document_id,
    d.source,
    d.created_at,
    COUNT(dc.id) AS chunk_count,
    AVG(LENGTH(dc.content)) AS avg_chunk_length
FROM documents d
LEFT JOIN document_chunks dc ON d.id = dc.document_id
GROUP BY d.id, d.source, d.created_at;

-- ============================================
-- Insert sample data (optional)
-- ============================================
-- Uncomment to add sample data
/*
INSERT INTO documents (content, metadata, source) VALUES
('Sample document about artificial intelligence', '{"topic": "AI", "language": "en"}', 'sample.txt');
*/

-- ============================================
-- Verify setup
-- ============================================
SELECT 'Database setup completed successfully!' AS status;
SELECT 'Tables created:' AS info;
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public' AND table_type = 'BASE TABLE';
