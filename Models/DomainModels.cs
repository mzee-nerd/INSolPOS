using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INSolPOS.Models
{
    // ─────────────── USER SYSTEM ───────────────
    public class ApplicationUser
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string FullName { get; set; } = "";
        [Required, MaxLength(100)] public string Username { get; set; } = "";
        [Required] public string PasswordHash { get; set; } = "";
        [Required] public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public enum UserRole { SuperAdmin = 1, Admin = 2, Teller = 3, SalesMen = 4 }

    // ─────────────── CUSTOMER ───────────────
    public class Customer
    {
        public int Id { get; set; }
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? CNIC { get; set; }
        public string? Email { get; set; }
        public int? RouteId { get; set; }
        public SaleRoute? Route { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal OpeningBalance { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public List<Sale> Sales { get; set; } = new();
        public List<CustomerLedger> Ledgers { get; set; } = new();
    }

    // ─────────────── VENDOR ───────────────
    public class Vendor
    {
        public int Id { get; set; }
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? CNIC { get; set; }
        public string? CompanyName { get; set; }
        public decimal OpeningBalance { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public List<Purchase> Purchases { get; set; } = new();
    }

    // ─────────────── STAFF ───────────────
    public class Staff
    {
        public int Id { get; set; }
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? CNIC { get; set; }
        public string? Address { get; set; }
        public string Designation { get; set; } = "";
        public decimal BasicSalary { get; set; }
        public DateTime JoiningDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public List<StaffSalary> Salaries { get; set; } = new();
        public List<SaleCommission> Commissions { get; set; } = new();
    }

    public class StaffSalary
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public Staff? Staff { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal AdvanceDeduction { get; set; }
        public decimal NetSalary { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidOn { get; set; }
        public string? Notes { get; set; }
    }

    public class AdvanceSalary
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public Staff? Staff { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Reason { get; set; }
        public bool IsRecovered { get; set; }
    }

    // ─────────────── UNIT ───────────────
    public class Unit
    {
        public int Id { get; set; }
        [Required, MaxLength(50)] public string Name { get; set; } = "";
        public string? Abbreviation { get; set; }
    }

    // ─────────────── CATEGORY ───────────────
    public class Category
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = "";
        public List<Product> Products { get; set; } = new();
    }

    // ─────────────── PRODUCT ───────────────
    public class Product
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Name { get; set; } = "";
        public string? Barcode { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int UnitId { get; set; }
        public Unit? Unit { get; set; }
        public int? CartonUnitId { get; set; }
        public Unit? CartonUnit { get; set; }
        public int CartonQty { get; set; } = 1; // pieces per carton
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal CommissionPercentage { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNo { get; set; }
        public string? Color { get; set; }
        public decimal CurrentStock { get; set; } // in loose units
        public decimal MinStockLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public bool TrackExpiry { get; set; }
    }

    // ─────────────── WAREHOUSE ───────────────
    public class Warehouse
    {
        public int Id { get; set; }
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        public string? Location { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; } = true;
        public List<WarehouseStock> Stocks { get; set; } = new();
    }

    public class WarehouseStock
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal Quantity { get; set; }
        public decimal CartonQty { get; set; }
    }

    public class WarehouseTransfer
    {
        public int Id { get; set; }
        public int? FromWarehouseId { get; set; }
        public Warehouse? FromWarehouse { get; set; }
        public int? ToWarehouseId { get; set; }
        public Warehouse? ToWarehouse { get; set; }
        public DateTime TransferDate { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public List<WarehouseTransferItem> Items { get; set; } = new();
    }

    public class WarehouseTransferItem
    {
        public int Id { get; set; }
        public int TransferId { get; set; }
        public WarehouseTransfer? Transfer { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal Quantity { get; set; }
    }

    // ─────────────── VEHICLE ───────────────
    public class Vehicle
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string VehicleNo { get; set; } = "";
        public string? Model { get; set; }
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public bool IsActive { get; set; } = true;
        public List<VehicleLoad> Loads { get; set; } = new();
    }

    public class VehicleLoad
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }
        public DateTime LoadDate { get; set; } = DateTime.Now;
        public LoadStatus Status { get; set; } = LoadStatus.Loaded;
        public string? Notes { get; set; }
        public List<VehicleLoadItem> Items { get; set; } = new();
    }

    public enum LoadStatus { Loaded = 1, Unloaded = 2, Partial = 3 }

    public class VehicleLoadItem
    {
        public int Id { get; set; }
        public int LoadId { get; set; }
        public VehicleLoad? Load { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal LoadedQty { get; set; }
        public decimal UnloadedQty { get; set; }
        public decimal SoldQty => LoadedQty - UnloadedQty;
    }

    // ─────────────── SALE ROUTE ───────────────
    public class SaleRoute
    {
        public int Id { get; set; }
        [Required, MaxLength(150)] public string Name { get; set; } = "";
        public string? Area { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public List<Customer> Customers { get; set; } = new();
    }

    // ─────────────── PURCHASE ───────────────
    public class Purchase
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = "";
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount => TotalAmount - PaidAmount;
        public PaymentMethod PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public List<PurchaseItem> Items { get; set; } = new();
    }

    public class PurchaseItem
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal CartonQty { get; set; }
        public decimal LooseQty { get; set; }
        public decimal TotalQty => (CartonQty * (Product?.CartonQty ?? 1)) + LooseQty;
        public decimal PurchasePrice { get; set; }
        public decimal TotalAmount => TotalQty * PurchasePrice;
        public string? BatchNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class PurchaseReturn
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public string? Reason { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PurchaseReturnItem> Items { get; set; } = new();
    }

    public class PurchaseReturnItem
    {
        public int Id { get; set; }
        public int ReturnId { get; set; }
        public PurchaseReturn? Return { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount => Quantity * Price;
    }

    // ─────────────── SALE ───────────────
    public class Sale
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = "";
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int? StaffId { get; set; }
        public Staff? Staff { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public SaleType SaleType { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount => TotalAmount - PaidAmount;
        public PaymentMethod PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public bool IsCredit { get; set; }
        public List<SaleItem> Items { get; set; } = new();
        public List<SaleCommission> Commissions { get; set; } = new();
    }

    public enum SaleType { Cash = 1, Credit = 2, Wholesale = 3 }
    public enum PaymentMethod { Cash = 1, BankTransfer = 2, Cheque = 3, Credit = 4 }

    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal CartonQty { get; set; }
        public decimal LooseQty { get; set; }
        public decimal TotalQty => (CartonQty * (Product?.CartonQty ?? 1)) + LooseQty;
        public decimal SalePrice { get; set; }
        public decimal PurchaseRate { get; set; }
        public string? BatchNo { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount => (TotalQty * SalePrice) - Discount;
    }

    public class SaleReturn
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public string? Reason { get; set; }
        public decimal TotalAmount { get; set; }
        public List<SaleReturnItem> Items { get; set; } = new();
    }

    public class SaleReturnItem
    {
        public int Id { get; set; }
        public int ReturnId { get; set; }
        public SaleReturn? Return { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount => Quantity * Price;
    }

    public class SaleCommission
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public int StaffId { get; set; }
        public Staff? Staff { get; set; }
        public decimal CommissionAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidOn { get; set; }
    }

    // ─────────────── CUSTOMER LEDGER ───────────────
    public class CustomerLedger
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = "";
        public decimal Debit { get; set; }   // Sale / amount owed
        public decimal Credit { get; set; }  // Payment received
        public decimal Balance { get; set; } // Running balance
        public string? ReferenceType { get; set; } // Sale, Payment, Return
        public int? ReferenceId { get; set; }
    }

    // ─────────────── INITIAL CAPITAL ───────────────
    public class InitialCapital
    {
        public int Id { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public CapitalType Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }

    public enum CapitalType { Cash = 1, Bank = 2, Asset = 3, Inventory = 4 }

    // ─────────────── EXPENSE ───────────────
    public class Expense
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
    }

    // ─────────────── PAYMENT RECEIPT ───────────────
    public class PaymentReceipt
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public PaymentMethod Method { get; set; }
        public string? ChequeNo { get; set; }
        public string? Notes { get; set; }
        public int ReceivedByUserId { get; set; }
    }

    public class ExpenseCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CompanySettings
    {
        public int Id { get; set; }
        public string BusinessName { get; set; } = "My Business";
        public string? Tagline { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoPath { get; set; }  // relative path e.g. /uploads/logo.png
        public string? Currency { get; set; } = "Rs.";
        public string? InvoiceFooter { get; set; }
    }

    public class VendorLedger
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
    }

    public class VendorPayment
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public PaymentMethod Method { get; set; }
        public string? ChequeNo { get; set; }
        public string? Notes { get; set; }
        public int PaidByUserId { get; set; }
    }
}
