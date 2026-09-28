CREATE DATABASE IsolationDemo;
GO
-- Turn on snapshot isolation
ALTER DATABASE IsolationDemo SET ALLOW_SNAPSHOT_ISOLATION ON;
GO
USE IsolationDemo;
GO

-- Db table Accounts for dirty read, non-repeatable read, and snapshot demonstration
CREATE TABLE Accounts (
    AccountID INT PRIMARY KEY,
    Balance INT
);
INSERT INTO Accounts (AccountID, Balance) VALUES (1, 100);
GO

-- Db table Orders for phantom reading demonstration
CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    Customer VARCHAR(100)
);
INSERT INTO Orders (Customer) VALUES ('Alice'), ('Bob'), ('Charlie');
GO
