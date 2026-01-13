# PostgreSQL RAG Database Setup

## Overview
Successfully set up a PostgreSQL database with pgvector extension for RAG (Retrieval-Augmented Generation) operations.

## Database Details

### Connection Information
- **Host**: localhost
- **Port**: 5432
- **Database**: rag
- **User**: postgres
- **Password**: (set in .env file or environment variable)

### Docker Container
```bash
docker run --name some-postgres -e POSTGRES_PASSWORD=YOUR_PASSWORD_HERE -p 5432:5432 -d pgvector/pgvector:pg17
```

## Database Schema

### Tables

#### 1. `documents`
Stores the original documents.

| Column | Type | Description |
|--------|------|-------------|
| id | SERIAL (PK) | Unique document identifier |
| content | TEXT | Full document content |
| metadata | JSONB | Document metadata (JSON) |
| source | VARCHAR(500) | Document source/filename |
| created_at | TIMESTAMP | Creation timestamp |
| updated_at | TIMESTAMP | Last update timestamp |

#### 2. `document_chunks`
Stores document chunks with vector embeddings.

| Column | Type | Description |
|--------|------|-------------|
| id | SERIAL (PK) | Unique chunk identifier |
| document_id | INTEGER (FK) | Reference to parent document |
| chunk_index | INTEGER | Index of chunk within document |
| content | TEXT | Chunk text content |
| metadata | JSONB | Chunk-specific metadata |
| embedding | vector(384) | 384-dimensional embedding vector |
| created_at | TIMESTAMP | Creation timestamp |

### Indexes
- **document_id**: B-tree index for fast joins
- **metadata**: GIN index for JSON queries
- **embedding**: HNSW index for vector similarity search (cosine distance)

### Functions

#### `search_similar_chunks(query_embedding, match_threshold, match_count)`
Performs vector similarity search using cosine distance.

**Parameters:**
- `query_embedding`: vector(384) - The query embedding
- `match_threshold`: FLOAT (default 0.5) - Minimum similarity score
- `match_count`: INT (default 5) - Number of results to return

**Returns:** Table with id, document_id, content, metadata, and similarity score

### Views

#### `document_stats`
Shows statistics for each document including chunk count and average chunk length.

## Python Integration

### Files Created

1. **`db_connection.py`**: Basic PostgreSQL connection helper
2. **`postgres_rag_manager.py`**: Full RAG manager with the following methods:
   - `insert_document()`: Insert a new document
   - `insert_chunks_with_embeddings()`: Batch insert chunks with embeddings
   - `search_similar_chunks()`: Vector similarity search
   - `get_document_stats()`: Get database statistics
   - `delete_document()`: Delete document and chunks
3. **`setup_rag_database.sql`**: Complete database schema setup script

### Usage Example

```python
from postgres_rag_manager import PostgresRAGManager
import numpy as np

# Initialize manager
manager = PostgresRAGManager()

# Insert a document
doc_id = manager.insert_document(
    content="Sample document content",
    metadata={"topic": "AI", "language": "en"},
    source="sample.txt"
)

# Insert chunks with embeddings
chunks = ["Chunk 1 text", "Chunk 2 text"]
embeddings = np.random.rand(2, 384)  # Replace with actual embeddings
manager.insert_chunks_with_embeddings(doc_id, chunks, embeddings)

# Search for similar chunks
query_embedding = np.random.rand(384)  # Replace with actual query embedding
results = manager.search_similar_chunks(query_embedding, top_k=5)

for result in results:
    print(f"Similarity: {result['similarity']:.3f}")
    print(f"Content: {result['content']}")
    print()

# Close connection
manager.close()
```

## VS Code Connection

### Using PostgreSQL Explorer Extension

1. Click the PostgreSQL icon in the VS Code sidebar
2. Click the "+" button to add a connection
3. Enter the connection details:
   - Host: localhost
   - Port: 5432
   - User: postgres
   - Password: (your database password)
   - Database: rag

## Useful Commands

### Docker Commands
```bash
# Start container
docker start some-postgres

# Stop container
docker stop some-postgres

# View logs
docker logs some-postgres

# Access PostgreSQL shell
docker exec -it some-postgres psql -U postgres -d rag
```

### PostgreSQL Commands
```sql
-- List all tables
\dt

-- Describe table structure
\d document_chunks

-- View embeddings info
\dx vector

-- Get document count
SELECT COUNT(*) FROM documents;

-- Get chunk count
SELECT COUNT(*) FROM document_chunks;

-- View document stats
SELECT * FROM document_stats;
```

## Next Steps

1. **Integrate with your embedding model**: Use the `EmbeddingManager` from your notebook to generate embeddings
2. **Migrate data**: Convert your existing ChromaDB data to PostgreSQL
3. **Update RAG pipeline**: Modify your retrieval pipeline to use PostgreSQL instead of ChromaDB
4. **Add more features**: Consider adding full-text search, filtering by metadata, etc.

## Features

- ✅ Vector similarity search with pgvector
- ✅ HNSW index for fast approximate nearest neighbor search
- ✅ JSON metadata support
- ✅ Document versioning with timestamps
- ✅ Batch insertion support
- ✅ Cascading deletes (deleting a document removes all chunks)
- ✅ Python integration with psycopg2
- ✅ Environment variable configuration
