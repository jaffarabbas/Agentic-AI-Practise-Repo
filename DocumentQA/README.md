# Document Q&A API

A production-ready RAG (Retrieval-Augmented Generation) API for document question answering using .NET, PostgreSQL with pgvector, and OpenAI.

## Features

- Document upload with background processing
- PDF and text file support
- Intelligent text chunking with overlap
- OpenAI embeddings (text-embedding-3-small)
- pgvector for fast semantic search
- RAG-based question answering
- Streaming response support
- Clean architecture (Domain, Application, Infrastructure, API layers)
- User isolation

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop (for PostgreSQL with pgvector)
- OpenAI API Key

## Quick Start

### 1. Database Setup

The PostgreSQL database with pgvector is already running in Docker:

```bash
# Verify the container is running
docker ps | grep postgres

# Connection details in appsettings.Development.json
```

The schema has been initialized with:
- `documents` table for document metadata
- `chunks` table with vector embeddings
- HNSW index for fast similarity search

### 2. Configuration

**IMPORTANT**: Create your own configuration file with your credentials:

```bash
cd src/DocumentQA.Api
cp appsettings.Example.json appsettings.Development.json
```

Edit `appsettings.Development.json` and add:
- Your PostgreSQL password in the connection string
- Your OpenAI API key

**Never commit `appsettings.Development.json` to Git!**

### 3. Run the API

```bash
cd src/DocumentQA.Api
dotnet run
```

The API will start at:
- HTTP: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

## API Endpoints

### Document Management

#### Upload Document
```http
POST /api/documents
Content-Type: multipart/form-data
X-User-Id: user123

Form Data:
- file: (PDF or text file, max 10MB)
```

Response:
```json
{
  "documentId": "guid",
  "status": "pending",
  "message": "Document queued for processing."
}
```

#### Get Document Status
```http
GET /api/documents/{id}
X-User-Id: user123
```

#### List User Documents
```http
GET /api/documents
X-User-Id: user123
```

#### Delete Document
```http
DELETE /api/documents/{id}
X-User-Id: user123
```

### Question Answering

#### Ask Question (Synchronous)
```http
POST /api/ask
Content-Type: application/json
X-User-Id: user123

{
  "question": "What is this document about?",
  "documentId": "optional-guid",
  "topK": 5
}
```

Response:
```json
{
  "answer": "The document is about...",
  "sources": [
    {
      "chunkId": "guid",
      "documentId": "guid",
      "contentPreview": "...",
      "similarity": 0.85,
      "filename": "document.pdf"
    }
  ],
  "usage": {
    "inputTokens": 1234,
    "outputTokens": 567,
    "estimatedCost": 0.000123
  }
}
```

#### Ask Question (Streaming)
```http
POST /api/ask/stream
Content-Type: application/json
X-User-Id: user123

{
  "question": "Summarize the key points"
}
```

Returns: Server-Sent Events (text/event-stream)

### Health Check
```http
GET /health
```

## Testing with cURL

### Upload a Document
```bash
curl -X POST http://localhost:5000/api/documents \
  -H "X-User-Id: user123" \
  -F "file=@test.pdf"
```

### Check Document Status
```bash
curl http://localhost:5000/api/documents/{document-id} \
  -H "X-User-Id: user123"
```

### List Documents
```bash
curl http://localhost:5000/api/documents \
  -H "X-User-Id: user123"
```

### Ask a Question
```bash
curl -X POST http://localhost:5000/api/ask \
  -H "Content-Type: application/json" \
  -H "X-User-Id: user123" \
  -d '{"question": "What is this document about?"}'
```

### Ask with Streaming
```bash
curl -X POST http://localhost:5000/api/ask/stream \
  -H "Content-Type: application/json" \
  -H "X-User-Id: user123" \
  -d '{"question": "Summarize the key points"}' \
  --no-buffer
```

## Project Structure

```
DocumentQA/
├── src/
│   ├── DocumentQA.Api/              # REST API endpoints
│   │   ├── Endpoints/               # Minimal API endpoints
│   │   └── Program.cs               # Startup configuration
│   ├── DocumentQA.Application/      # Business logic
│   │   ├── Services/                # Application services
│   │   ├── Interfaces/              # Abstraction interfaces
│   │   ├── Configuration/           # Settings classes
│   │   ├── DTOs/                    # Data transfer objects
│   │   └── BackgroundJobs/          # Background workers
│   ├── DocumentQA.Domain/           # Domain entities
│   │   └── Entities/                # Domain models
│   └── DocumentQA.Infrastructure/   # External integrations
│       ├── AI/                      # OpenAI implementations
│       ├── Persistence/             # Database repositories
│       └── FileProcessing/          # Text extraction
├── tests/
│   ├── DocumentQA.UnitTests/
│   └── DocumentQA.IntegrationTests/
├── database/
│   └── init.sql                     # Database schema
└── README.md
```

## Architecture

### Clean Architecture Layers

1. **Domain Layer** (`DocumentQA.Domain`)
   - Pure business entities
   - No dependencies on external frameworks
   - Entities: `Document`, `Chunk`, `SearchResult`

2. **Application Layer** (`DocumentQA.Application`)
   - Business logic and use cases
   - Interfaces for external services
   - Services: `IngestionService`, `RagService`, `ChunkingService`

3. **Infrastructure Layer** (`DocumentQA.Infrastructure`)
   - External integrations
   - OpenAI API client
   - PostgreSQL repositories with Dapper
   - PDF text extraction

4. **API Layer** (`DocumentQA.Api`)
   - REST endpoints using Minimal APIs
   - Dependency injection configuration
   - Swagger/OpenAPI documentation

### How It Works

1. **Document Ingestion**:
   - User uploads a file via POST /api/documents
   - File is saved temporarily
   - Document record created in database (status: pending)
   - Job queued for background processing
   - Background worker picks up the job:
     - Extracts text from PDF/text file
     - Chunks text with overlap (500 tokens, 50 token overlap)
     - Generates embeddings using OpenAI
     - Stores chunks with embeddings in PostgreSQL
     - Updates document status to completed

2. **Question Answering**:
   - User asks a question via POST /api/ask
   - Question is embedded using OpenAI
   - Vector similarity search finds relevant chunks (pgvector)
   - Top K most similar chunks retrieved
   - Context built from retrieved chunks
   - OpenAI generates answer based only on context
   - Response includes answer, sources, and usage info

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=documentqa;Username=postgres;Password=***"
  },
  "OpenAI": {
    "ApiKey": "sk-...",
    "ChatModel": "gpt-4o-mini",
    "EmbeddingModel": "text-embedding-3-small",
    "MaxRetries": 3,
    "TimeoutSeconds": 60
  },
  "Ingestion": {
    "MaxFileSizeMb": 10,
    "ChunkSize": 500,
    "ChunkOverlap": 50,
    "AllowedContentTypes": [
      "application/pdf",
      "text/plain",
      "text/markdown"
    ]
  },
  "Rag": {
    "TopK": 5,
    "MinSimilarity": 0.7,
    "MaxContextTokens": 4000
  }
}
```

## Database Schema

### documents table
- `id` (UUID) - Primary key
- `user_id` (VARCHAR) - User identifier
- `filename` (VARCHAR) - Original filename
- `content_type` (VARCHAR) - MIME type
- `file_size_bytes` (BIGINT) - File size
- `chunk_count` (INT) - Number of chunks
- `status` (VARCHAR) - pending, processing, completed, failed
- `error_message` (TEXT) - Error details if failed
- `created_at` (TIMESTAMP) - Creation time
- `processed_at` (TIMESTAMP) - Processing completion time

### chunks table
- `id` (UUID) - Primary key
- `document_id` (UUID) - Foreign key to documents
- `content` (TEXT) - Chunk text
- `embedding` (vector(1536)) - OpenAI embedding
- `chunk_index` (INT) - Position in document
- `token_count` (INT) - Estimated tokens
- `created_at` (TIMESTAMP) - Creation time

### Indexes
- HNSW index on `chunks.embedding` for fast vector similarity search
- B-tree indexes on `user_id`, `document_id`, `status`

## Cost Estimation

### OpenAI API Costs (as of January 2025)

**Embeddings** (text-embedding-3-small):
- $0.00002 per 1K tokens

**Chat Completions** (gpt-4o-mini):
- Input: $0.00015 per 1K tokens
- Output: $0.0006 per 1K tokens

**Example: 10-page PDF (5000 words)**
- Text extraction: Free
- Chunking into ~30 chunks: Free
- Embeddings: ~$0.0003
- Question answering (5 retrieved chunks + response): ~$0.001

**Total cost per document + question: ~$0.0013**

## Troubleshooting

### Build Errors

If you get dependency errors:
```bash
cd src/DocumentQA.Infrastructure
dotnet restore
```

### Database Connection Issues

1. Ensure PostgreSQL container is running:
```bash
docker ps | grep some-postgres
```

2. Test connection:
```bash
docker exec some-postgres psql -U postgres -d documentqa -c "SELECT 1;"
```

3. Check if tables exist:
```bash
docker exec some-postgres psql -U postgres -d documentqa -c "\dt"
```

### OpenAI API Errors

1. Verify your API key in `appsettings.json`
2. Check your OpenAI account has credits
3. Ensure you're not hitting rate limits

### Background Worker Not Processing

1. Check logs for errors
2. Verify the IngestionWorker is registered in `Program.cs`
3. Check temporary file permissions

## Next Steps / Enhancements

### Authentication & Authorization
- [ ] Add JWT authentication
- [ ] Implement role-based access control
- [ ] Add API key management

### Features
- [ ] Support more file types (DOCX, HTML, Markdown)
- [ ] Add semantic caching for repeated questions
- [ ] Implement usage tracking and analytics
- [ ] Add rate limiting per user
- [ ] Support multi-document queries
- [ ] Add conversation history/memory

### Performance
- [ ] Implement batch processing for multiple files
- [ ] Add Redis for caching
- [ ] Optimize chunk storage (compression)
- [ ] Add query result caching

### Deployment
- [ ] Create Docker Compose for full stack
- [ ] Add health checks and monitoring
- [ ] CI/CD pipeline setup
- [ ] Kubernetes deployment manifests

### Testing
- [ ] Add unit tests
- [ ] Add integration tests with Testcontainers
- [ ] Add E2E tests
- [ ] Load testing with k6 or similar

## License

MIT License - see LICENSE file for details

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Support

For issues and questions:
- Open an issue on GitHub
- Check existing issues for solutions
