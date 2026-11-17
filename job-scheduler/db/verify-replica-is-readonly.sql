-- Replicas only (HA replica via -K ReadOnly, named replica). A write must fail with error 3906
-- ("Failed to update database ... because the database is read-only"). Never run this against a primary:
-- there it creates and drops a probe table and reports UNEXPECTED.
SET NOCOUNT ON;
BEGIN TRY
  CREATE TABLE dbo.__ReplicaWriteProbe (Id INT);
  SELECT N'UNEXPECTED: write succeeded' AS Result;
  DROP TABLE dbo.__ReplicaWriteProbe;
END TRY
BEGIN CATCH
  SELECT ERROR_NUMBER() AS ErrorNumber, ERROR_MESSAGE() AS ErrorMessage;
END CATCH
