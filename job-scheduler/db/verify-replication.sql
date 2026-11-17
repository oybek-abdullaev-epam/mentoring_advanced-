-- Replication check. Run against the primary, the HA replica (-K ReadOnly) and the named replica:
-- Updateability must be READ_WRITE on the primary and READ_ONLY on both replicas; row counts must converge.
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName,
       CAST(DATABASEPROPERTYEX(DB_NAME(), 'Updateability') AS NVARCHAR(20)) AS Updateability,
       @@SERVERNAME AS ServerName, SYSUTCDATETIME() AS CheckedAtUtc;
SELECT t.name AS TableName, SUM(p.rows) AS [RowCount]
FROM sys.tables t JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY t.name ORDER BY t.name;
