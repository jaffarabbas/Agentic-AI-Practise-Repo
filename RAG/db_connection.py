import os
import psycopg2
from psycopg2 import Error
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

def connect_to_postgres():
    """
    Connect to PostgreSQL database using credentials from .env file

    Returns:
        connection: PostgreSQL database connection object
    """
    try:
        # Get database credentials from environment variables
        connection = psycopg2.connect(
            host=os.getenv("POSTGRES_HOST", "localhost"),
            port=os.getenv("POSTGRES_PORT", "5432"),
            database=os.getenv("POSTGRES_DB"),
            user=os.getenv("POSTGRES_USER"),
            password=os.getenv("POSTGRES_PASSWORD")
        )

        # Create a cursor object
        cursor = connection.cursor()

        # Print PostgreSQL connection properties
        print("✓ Successfully connected to PostgreSQL database")
        cursor.execute("SELECT version();")
        record = cursor.fetchone()
        print(f"PostgreSQL version: {record[0]}")

        # Close cursor
        cursor.close()

        return connection

    except (Exception, Error) as error:
        print(f"✗ Error while connecting to PostgreSQL: {error}")
        return None

def test_connection():
    """Test the database connection and display available tables"""
    connection = connect_to_postgres()

    if connection:
        try:
            cursor = connection.cursor()

            # Get list of all tables in the database
            cursor.execute("""
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public'
                ORDER BY table_name;
            """)

            tables = cursor.fetchall()

            if tables:
                print(f"\n✓ Found {len(tables)} tables in the database:")
                for table in tables:
                    print(f"  - {table[0]}")
            else:
                print("\n! No tables found in the database")

            cursor.close()

        except (Exception, Error) as error:
            print(f"✗ Error executing query: {error}")

        finally:
            # Close the connection
            if connection:
                connection.close()
                print("\n✓ PostgreSQL connection closed")

if __name__ == "__main__":
    # Test the connection when running this file directly
    test_connection()
