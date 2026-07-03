-- ============================================================
-- INSol POS - Database Setup Script
-- Run this after: dotnet ef database update
-- Or execute manually in SQL Server Management Studio
-- ============================================================

USE INSolPOS;
GO

-- ── If tables don't exist yet, EnsureCreated() handles it.
-- ── This script adds default seed data and useful views.

-- ── Default Super Admin (username: superadmin / password: Admin@123)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'superadmin')
BEGIN
    INSERT INTO Users (FullName, Username, PasswordHash, Role, IsActive, CreatedAt)
    VALUES ('Super Admin', 'superadmin',
        'QWRtaW5AMTIzX0lOU29sU2FsdDIwMjQ=', -- Base64(Admin@123_INSolSalt2024)
        1, 1, GETDATE())
END

-- ── Default Units
IF NOT EXISTS (SELECT 1 FROM Units WHERE Name = 'Piece')
BEGIN
    INSERT INTO Units (Name, Abbreviation) VALUES
        ('Piece', 'Pcs'), ('Carton', 'Ctn'), ('Kilogram', 'Kg'),
        ('Liter', 'Ltr'), ('Dozen', 'Dz'), ('Pack', 'Pk'), ('Box', 'Bx')
END

-- ── Default Categories
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'General')
BEGIN
    INSERT INTO Categories (Name) VALUES
        ('General'), ('Food & Beverages'), ('Electronics'),
        ('Clothing'), ('Medicine'), ('Stationery'), ('Hardware')
END

-- ── Useful Views for Reporting

-- Customer Receivables Summary
CREATE OR ALTER VIEW vw_CustomerReceivables AS
SELECT
    c.Id AS CustomerId,
    c.Name AS CustomerName,
    c.Phone,
    r.Name AS RouteName,
    ISNULL(cl.Balance, 0) AS OutstandingBalance,
    c.CreditLimit
FROM Customers c
LEFT JOIN SaleRoutes r ON c.RouteId = r.Id
LEFT JOIN (
    SELECT CustomerId, Balance
    FROM CustomerLedgers cl1
    WHERE Id = (SELECT MAX(Id) FROM CustomerLedgers cl2 WHERE cl2.CustomerId = cl1.CustomerId)
) cl ON cl.CustomerId = c.Id
WHERE c.IsActive = 1;
GO

-- Daily Sales Summary
CREATE OR ALTER VIEW vw_DailySalesSummary AS
SELECT
    CAST(SaleDate AS DATE) AS SaleDay,
    COUNT(*) AS TotalTransactions,
    SUM(TotalAmount) AS TotalRevenue,
    SUM(PaidAmount) AS TotalCollected,
    SUM(TotalAmount - PaidAmount) AS TotalDue,
    SUM(CASE WHEN SaleType = 1 THEN TotalAmount ELSE 0 END) AS CashSales,
    SUM(CASE WHEN SaleType = 2 THEN TotalAmount ELSE 0 END) AS CreditSales
FROM Sales
GROUP BY CAST(SaleDate AS DATE);
GO

-- Product Stock Status
CREATE OR ALTER VIEW vw_StockStatus AS
SELECT
    p.Id, p.Name, p.Barcode, c.Name AS Category,
    u.Name AS Unit, p.CurrentStock,
    p.MinStockLevel, p.PurchasePrice, p.SalePrice,
    p.CurrentStock * p.PurchasePrice AS StockValue,
    p.ExpiryDate, p.BatchNo,
    CASE
        WHEN p.CurrentStock = 0 THEN 'Out of Stock'
        WHEN p.CurrentStock <= p.MinStockLevel THEN 'Low Stock'
        WHEN p.ExpiryDate IS NOT NULL AND p.ExpiryDate < GETDATE() THEN 'Expired'
        WHEN p.ExpiryDate IS NOT NULL AND p.ExpiryDate < DATEADD(DAY, 30, GETDATE()) THEN 'Expiring Soon'
        ELSE 'OK'
    END AS StockStatus
FROM Products p
LEFT JOIN Categories c ON p.CategoryId = c.Id
LEFT JOIN Units u ON p.UnitId = u.Id
WHERE p.IsActive = 1;
GO

-- Staff Commission Summary
CREATE OR ALTER VIEW vw_StaffCommissions AS
SELECT
    s.Id AS StaffId,
    s.Name AS StaffName,
    s.Designation,
    COUNT(sc.Id) AS TotalSales,
    SUM(sc.CommissionAmount) AS TotalCommission,
    SUM(CASE WHEN sc.IsPaid = 1 THEN sc.CommissionAmount ELSE 0 END) AS PaidCommission,
    SUM(CASE WHEN sc.IsPaid = 0 THEN sc.CommissionAmount ELSE 0 END) AS PendingCommission
FROM Staff s
LEFT JOIN SaleCommissions sc ON s.Id = sc.StaffId
WHERE s.IsActive = 1
GROUP BY s.Id, s.Name, s.Designation;
GO

PRINT 'INSol POS database setup completed successfully!';
GO
