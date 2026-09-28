#!/usr/bin/env bash
set -e

/opt/mssql/bin/sqlservr &

echo "Waiting for SQL Server to start..."

# Wait until SQL Server is ready
for i in {1..30}; do
  if /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" -C > /dev/null 2>&1; then
    echo "✅ SQL Server is ready!"
    break
  fi
  echo "⏳ SQL Server not ready yet... ($i)"
  sleep 2
done

echo "Running initialization scripts..."

for f in /init-db/*.sql; do
  if [ -f "$f" ]; then
    echo "📄 Running $f"
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$MSSQL_SA_PASSWORD" -C -i "$f"
  fi
done

echo "✅ Initialization scripts completed."

# Keep SQL Server running
wait
