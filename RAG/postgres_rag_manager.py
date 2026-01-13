import os
import psycopg2
from psycopg2.extras import Json, execute_values
from typing import List, Dict, Any, Tuple
import numpy as np
from dotenv import load_dotenv

# Load environment variables
load_dotenv()


class PostgresRAGManager:
    """Manage RAG documents and embeddings in PostgreSQL with pgvector"""

    def __init__(self):
        """Initialize database connection"""
        self.conn = None
        self.connect()

    def connect(self):
        """Connect to PostgreSQL database"""
        try:
            self.conn = psycopg2.connect(
                host=os.getenv("POSTGRES_HOST", "localhost"),
                port=os.getenv("POSTGRES_PORT", "5432"),
                database=os.getenv("POSTGRES_DB", "rag"),
                user=os.getenv("POSTGRES_USER", "postgres"),
                password=os.getenv("POSTGRES_PASSWORD")
            )
            print("[OK] Connected to PostgreSQL database")
        except Exception as e:
            print(f"[ERROR] Error connecting to database: {e}")
            raise

    def close(self):
        """Close database connection"""
        if self.conn:
            self.conn.close()
            print("[OK] Database connection closed")

    def insert_document(self, content: str, metadata: Dict = None, source: str = None) -> int:
        """
        Insert a document into the database

        Args:
            content: The document content
            metadata: Optional metadata dictionary
            source: Optional source identifier

        Returns:
            The document ID
        """
        try:
            cursor = self.conn.cursor()
            cursor.execute(
                """
                INSERT INTO documents (content, metadata, source)
                VALUES (%s, %s, %s)
                RETURNING id
                """,
                (content, Json(metadata) if metadata else None, source)
            )
            document_id = cursor.fetchone()[0]
            self.conn.commit()
            cursor.close()
            print(f"[OK] Inserted document with ID: {document_id}")
            return document_id
        except Exception as e:
            self.conn.rollback()
            print(f"[ERROR] Error inserting document: {e}")
            raise

    def insert_chunks_with_embeddings(
        self,
        document_id: int,
        chunks: List[str],
        embeddings: np.ndarray,
        metadata_list: List[Dict] = None
    ):
        """
        Insert document chunks with their embeddings

        Args:
            document_id: The parent document ID
            chunks: List of text chunks
            embeddings: Numpy array of embeddings (shape: [n_chunks, embedding_dim])
            metadata_list: Optional list of metadata dictionaries for each chunk
        """
        if len(chunks) != len(embeddings):
            raise ValueError("Number of chunks must match number of embeddings")

        try:
            cursor = self.conn.cursor()

            # Prepare data for batch insert
            data = []
            for i, (chunk, embedding) in enumerate(zip(chunks, embeddings)):
                metadata = metadata_list[i] if metadata_list and i < len(metadata_list) else None
                data.append((
                    document_id,
                    i,
                    chunk,
                    Json(metadata) if metadata else None,
                    embedding.tolist()
                ))

            # Batch insert
            execute_values(
                cursor,
                """
                INSERT INTO document_chunks (document_id, chunk_index, content, metadata, embedding)
                VALUES %s
                """,
                data,
                template="(%s, %s, %s, %s, %s::vector)"
            )

            self.conn.commit()
            cursor.close()
            print(f"[OK] Inserted {len(chunks)} chunks for document {document_id}")
        except Exception as e:
            self.conn.rollback()
            print(f"[ERROR] Error inserting chunks: {e}")
            raise

    def search_similar_chunks(
        self,
        query_embedding: np.ndarray,
        top_k: int = 5,
        similarity_threshold: float = 0.5
    ) -> List[Dict[str, Any]]:
        """
        Search for similar chunks using vector similarity

        Args:
            query_embedding: The query embedding vector
            top_k: Number of results to return
            similarity_threshold: Minimum similarity score (0-1)

        Returns:
            List of dictionaries containing matching chunks and metadata
        """
        try:
            cursor = self.conn.cursor()
            cursor.execute(
                """
                SELECT
                    dc.id,
                    dc.document_id,
                    dc.content,
                    dc.metadata,
                    d.source,
                    1 - (dc.embedding <=> %s::vector) AS similarity
                FROM document_chunks dc
                JOIN documents d ON dc.document_id = d.id
                WHERE 1 - (dc.embedding <=> %s::vector) > %s
                ORDER BY dc.embedding <=> %s::vector
                LIMIT %s
                """,
                (query_embedding.tolist(), query_embedding.tolist(),
                 similarity_threshold, query_embedding.tolist(), top_k)
            )

            results = []
            for row in cursor.fetchall():
                results.append({
                    'chunk_id': row[0],
                    'document_id': row[1],
                    'content': row[2],
                    'metadata': row[3],
                    'source': row[4],
                    'similarity': float(row[5])
                })

            cursor.close()
            print(f"[OK] Found {len(results)} similar chunks")
            return results
        except Exception as e:
            print(f"[ERROR] Error searching chunks: {e}")
            raise

    def get_document_stats(self) -> List[Dict[str, Any]]:
        """Get statistics about documents in the database"""
        try:
            cursor = self.conn.cursor()
            cursor.execute("SELECT * FROM document_stats")

            stats = []
            for row in cursor.fetchall():
                stats.append({
                    'document_id': row[0],
                    'source': row[1],
                    'created_at': row[2],
                    'chunk_count': row[3],
                    'avg_chunk_length': float(row[4]) if row[4] else 0
                })

            cursor.close()
            return stats
        except Exception as e:
            print(f"[ERROR] Error getting document stats: {e}")
            raise

    def delete_document(self, document_id: int):
        """Delete a document and all its chunks"""
        try:
            cursor = self.conn.cursor()
            cursor.execute("DELETE FROM documents WHERE id = %s", (document_id,))
            self.conn.commit()
            cursor.close()
            print(f"[OK] Deleted document {document_id} and all its chunks")
        except Exception as e:
            self.conn.rollback()
            print(f"[ERROR] Error deleting document: {e}")
            raise


def test_connection():
    """Test the PostgreSQL RAG Manager"""
    manager = PostgresRAGManager()

    try:
        # Get statistics
        print("\n=== Database Statistics ===")
        stats = manager.get_document_stats()
        if stats:
            for stat in stats:
                print(f"Document {stat['document_id']}: {stat['chunk_count']} chunks, "
                      f"avg length: {stat['avg_chunk_length']:.0f} chars")
        else:
            print("No documents in database yet")

    finally:
        manager.close()


if __name__ == "__main__":
    test_connection()
