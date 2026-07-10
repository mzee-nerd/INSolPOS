-- ============================================================
--  INSol POS v2 — Database Patch Script
--  Run in SQL Server Management Studio after the original setup.sql
--  Safe to run multiple times (checks IF NOT EXISTS)
-- ============================================================

USE INSolPOS;
GO

-- ── VendorLedgers table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='VendorLedgers')
BEGIN
    CREATE TABLE VendorLedgers (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        VendorId      INT           NOT NULL REFERENCES Vendors(Id),
        Date          DATETIME      NOT NULL DEFAULT GETDATE(),
        Description   NVARCHAR(300) NOT NULL,
        Debit         DECIMAL(18,2) NOT NULL DEFAULT 0,
        Credit        DECIMAL(18,2) NOT NULL DEFAULT 0,
        Balance       DECIMAL(18,2) NOT NULL DEFAULT 0,
        ReferenceType NVARCHAR(50)  NULL,
        ReferenceId   INT           NULL
    );
    CREATE INDEX IX_VendorLedgers_VendorId ON VendorLedgers(VendorId);
    PRINT '✓ VendorLedgers table created';

    -- Seed opening balances for existing vendors that have OpeningBalance > 0
    INSERT INTO VendorLedgers (VendorId, Description, Debit, Credit, Balance, ReferenceType, ReferenceId, Date)
    SELECT Id, 'Opening Balance', 0, OpeningBalance, OpeningBalance, 'Opening', Id, GETDATE()
    FROM Vendors WHERE OpeningBalance > 0;
    PRINT '✓ Opening balances seeded for existing vendors';
END
ELSE PRINT '- VendorLedgers already exists, skipped';
GO

-- ── VendorPayments table
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='VendorPayments')
BEGIN
    CREATE TABLE VendorPayments (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        VendorId     INT           NOT NULL REFERENCES Vendors(Id),
        Amount       DECIMAL(18,2) NOT NULL,
        Date         DATETIME      NOT NULL DEFAULT GETDATE(),
        Method       INT           NOT NULL DEFAULT 1,
        ChequeNo     NVARCHAR(50)  NULL,
        Notes        NVARCHAR(300) NULL,
        PaidByUserId INT           NOT NULL DEFAULT 1
    );
    PRINT '✓ VendorPayments table created';
END
ELSE PRINT '- VendorPayments already exists, skipped';
GO

-- Note: WarehouseId is NOT added to Purchases/Sales tables
-- as the repo model does not define these properties.
-- Warehouse tracking is done via WarehouseStock table.

-- ── Seed CustomerLedger opening balances for existing customers
INSERT INTO CustomerLedgers (CustomerId, Date, Description, Debit, Credit, Balance, ReferenceType, ReferenceId)
SELECT c.Id, GETDATE(), 'Opening Balance', c.OpeningBalance, 0, c.OpeningBalance, 'Opening', c.Id
FROM Customers c
WHERE c.OpeningBalance > 0
AND NOT EXISTS (
    SELECT 1 FROM CustomerLedgers cl WHERE cl.CustomerId = c.Id AND cl.ReferenceType = 'Opening'
);
PRINT '✓ Customer opening balances seeded';
GO

-- ── Verify
SELECT 'VendorLedgers'  AS TableName, COUNT(*) AS Rows FROM VendorLedgers  UNION ALL
SELECT 'VendorPayments' AS TableName, COUNT(*) AS Rows FROM VendorPayments UNION ALL
SELECT 'CustomerLedgers'AS TableName, COUNT(*) AS Rows FROM CustomerLedgers;
GO

PRINT '';
PRINT '====================================================';
PRINT '  INSol POS v2 Patch Applied Successfully!';
PRINT '====================================================';
GO
