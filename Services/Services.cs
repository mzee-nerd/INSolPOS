using INSolPOS.Data;
using INSolPOS.Models;
using Microsoft.EntityFrameworkCore;

namespace INSolPOS.Services
{
    // ─────────────── AUTH SERVICE ───────────────
    public interface IAuthService
    {
        Task<ApplicationUser?> LoginAsync(string username, string password);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        public AuthService(AppDbContext db) => _db = db;

        public async Task<ApplicationUser?> LoginAsync(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null) return null;
            return VerifyPassword(password, user.PasswordHash) ? user : null;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null || !VerifyPassword(oldPassword, user.PasswordHash)) return false;
            user.PasswordHash = HashPassword(newPassword);
            await _db.SaveChangesAsync();
            return true;
        }

        public string HashPassword(string password) =>
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "_INSolSalt2024"));

        public bool VerifyPassword(string password, string hash) =>
            HashPassword(password) == hash;
    }

    // ─────────────── PRODUCT SERVICE ───────────────
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<bool> AdjustStockAsync(int productId, decimal qty, bool isAddition);
        Task<List<Product>> GetLowStockProductsAsync();
        Task<List<Product>> GetExpiringProductsAsync(int daysAhead = 30);
        string GenerateBarcode();
    }

    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;
        public ProductService(AppDbContext db) => _db = db;

        public async Task<List<Product>> GetAllProductsAsync() =>
            await _db.Products.Include(p => p.Category).Include(p => p.Unit).Where(p => p.IsActive).ToListAsync();

        public async Task<Product?> GetProductByIdAsync(int id) =>
            await _db.Products.Include(p => p.Category).Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<bool> AdjustStockAsync(int productId, decimal qty, bool isAddition)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return false;
            product.CurrentStock = isAddition ? product.CurrentStock + qty : product.CurrentStock - qty;
            if (product.CurrentStock < 0) product.CurrentStock = 0;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Product>> GetLowStockProductsAsync() =>
            await _db.Products.Where(p => p.IsActive && p.CurrentStock <= p.MinStockLevel).ToListAsync();

        public async Task<List<Product>> GetExpiringProductsAsync(int daysAhead = 30) =>
            await _db.Products.Where(p => p.IsActive && p.ExpiryDate.HasValue &&
                p.ExpiryDate.Value <= DateTime.Now.AddDays(daysAhead)).ToListAsync();

        public string GenerateBarcode() => "PRD" + DateTime.Now.Ticks.ToString().Substring(8);
    }

    // ─────────────── SALE SERVICE ───────────────
    public interface ISaleService
    {
        Task<Sale> CreateSaleAsync(Sale sale);
        Task<string> GenerateInvoiceNoAsync();
        Task<SaleReturn> CreateSaleReturnAsync(SaleReturn ret);
        Task<List<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to);
        Task<decimal> GetTotalSalesAsync(DateTime from, DateTime to);
    }

    public class SaleService : ISaleService
    {
        private readonly AppDbContext _db;
        private readonly IProductService _productService;
        private readonly ILedgerService _ledgerService;

        public SaleService(AppDbContext db, IProductService productService, ILedgerService ledgerService)
        {
            _db = db;
            _productService = productService;
            _ledgerService = ledgerService;
        }

        public async Task<Sale> CreateSaleAsync(Sale sale)
        {
            sale.InvoiceNo = await GenerateInvoiceNoAsync();

            // Adjust stock and calculate commissions
            foreach (var item in sale.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    await _productService.AdjustStockAsync(item.ProductId, item.TotalQty, false);

                    // Commission for salesman
                    if (sale.StaffId.HasValue && product.CommissionPercentage > 0)
                    {
                        var commission = new SaleCommission
                        {
                            StaffId = sale.StaffId.Value,
                            CommissionAmount = item.TotalAmount * (product.CommissionPercentage / 100)
                        };
                        sale.Commissions.Add(commission);
                    }
                }
            }

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            // Update customer ledger for credit sales
            if (sale.IsCredit && sale.CustomerId.HasValue)
            {
                await _ledgerService.AddEntryAsync(sale.CustomerId.Value,
                    $"Sale Invoice #{sale.InvoiceNo}", sale.TotalAmount, 0, "Sale", sale.Id);
            }

            return sale;
        }

        public async Task<string> GenerateInvoiceNoAsync()
        {
            var count = await _db.Sales.CountAsync() + 1;
            return $"INV-{DateTime.Now:yyMM}-{count:D4}";
        }

        public async Task<SaleReturn> CreateSaleReturnAsync(SaleReturn ret)
        {
            foreach (var item in ret.Items)
                await _productService.AdjustStockAsync(item.ProductId, item.Quantity, true);

            _db.SaleReturns.Add(ret);
            await _db.SaveChangesAsync();
            return ret;
        }

        public async Task<List<Sale>> GetSalesByDateRangeAsync(DateTime from, DateTime to) =>
            await _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .Where(s => s.SaleDate >= from && s.SaleDate <= to)
                .OrderByDescending(s => s.SaleDate).ToListAsync();

        public async Task<decimal> GetTotalSalesAsync(DateTime from, DateTime to) =>
            await _db.Sales.Where(s => s.SaleDate >= from && s.SaleDate <= to).SumAsync(s => s.TotalAmount);
    }

    // ─────────────── PURCHASE SERVICE ───────────────
    public interface IPurchaseService
    {
        Task<Purchase> CreatePurchaseAsync(Purchase purchase);
        Task<string> GeneratePurchaseNoAsync();
        Task<PurchaseReturn> CreatePurchaseReturnAsync(PurchaseReturn ret);
    }

    public class PurchaseService : IPurchaseService
    {
        private readonly AppDbContext _db;
        private readonly IProductService _productService;

        public PurchaseService(AppDbContext db, IProductService productService)
        {
            _db = db;
            _productService = productService;
        }

        public async Task<Purchase> CreatePurchaseAsync(Purchase purchase)
        {
            purchase.InvoiceNo = await GeneratePurchaseNoAsync();

            foreach (var item in purchase.Items)
            {
                await _productService.AdjustStockAsync(item.ProductId, item.TotalQty, true);
                // Update batch/expiry on product
                if (!string.IsNullOrEmpty(item.BatchNo))
                {
                    var product = await _db.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.BatchNo = item.BatchNo;
                        if (item.ExpiryDate.HasValue) product.ExpiryDate = item.ExpiryDate;
                    }
                }
            }

            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync();
            return purchase;
        }

        public async Task<string> GeneratePurchaseNoAsync()
        {
            var count = await _db.Purchases.CountAsync() + 1;
            return $"PUR-{DateTime.Now:yyMM}-{count:D4}";
        }

        public async Task<PurchaseReturn> CreatePurchaseReturnAsync(PurchaseReturn ret)
        {
            foreach (var item in ret.Items)
                await _productService.AdjustStockAsync(item.ProductId, item.Quantity, false);

            _db.PurchaseReturns.Add(ret);
            await _db.SaveChangesAsync();
            return ret;
        }
    }

    // ─────────────── LEDGER SERVICE ───────────────
    public interface ILedgerService
    {
        Task AddEntryAsync(int customerId, string desc, decimal debit, decimal credit, string refType, int refId);
        Task<List<CustomerLedger>> GetCustomerLedgerAsync(int customerId);
        Task<List<CustomerLedger>> GetCustomerLedgerAsync(int customerId, DateTime? from, DateTime? to);
        Task<decimal> GetCustomerBalanceAsync(int customerId);
    }

    public class LedgerService : ILedgerService
    {
        private readonly AppDbContext _db;
        public LedgerService(AppDbContext db) => _db = db;

        public async Task AddEntryAsync(int customerId, string desc, decimal debit, decimal credit, string refType, int refId)
        {
            var lastBalance = await GetCustomerBalanceAsync(customerId);
            var entry = new CustomerLedger
            {
                CustomerId = customerId,
                Description = desc,
                Debit = debit,
                Credit = credit,
                Balance = lastBalance + debit - credit,
                ReferenceType = refType,
                ReferenceId = refId,
                Date = DateTime.Now
            };
            _db.CustomerLedgers.Add(entry);
            await _db.SaveChangesAsync();
        }

        public async Task<List<CustomerLedger>> GetCustomerLedgerAsync(int customerId) =>
            await _db.CustomerLedgers.Where(l => l.CustomerId == customerId)
                .OrderBy(l => l.Date).ThenBy(l => l.Id).ToListAsync();

        public async Task<List<CustomerLedger>> GetCustomerLedgerAsync(int customerId, DateTime? from, DateTime? to)
        {
            var q = _db.CustomerLedgers.Where(l => l.CustomerId == customerId);
            if (from.HasValue) q = q.Where(l => l.Date >= from.Value);
            if (to.HasValue)   q = q.Where(l => l.Date <= to.Value.AddDays(1));
            return await q.OrderBy(l => l.Date).ThenBy(l => l.Id).ToListAsync();
        }

        public async Task<decimal> GetCustomerBalanceAsync(int customerId)
        {
            var last = await _db.CustomerLedgers.Where(l => l.CustomerId == customerId)
                .OrderByDescending(l => l.Id).FirstOrDefaultAsync();
            return last?.Balance ?? 0;
        }
    }

    // ─────────────── REPORT SERVICE ───────────────
    public interface IReportService
    {
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<ProfitLossReport> GetProfitLossAsync(DateTime from, DateTime to);
        Task<TrialBalance> GetTrialBalanceAsync(DateTime asOf);
    }

    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;
        public ReportService(AppDbContext db) => _db = db;

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;

            // ── PendingReceivables: fetch the latest ledger row per customer in SQL,
            //    then sum on the client — avoids the untranslatable GroupBy+First combo.
            var latestBalances = await _db.CustomerLedgers
                .GroupBy(l => l.CustomerId)
                .Select(g => g.OrderByDescending(l => l.Id).First().Balance)
                .ToListAsync();                              // pull to client
            var pendingReceivables = latestBalances.Where(b => b > 0).Sum();

            // ── EF cannot translate .Date == today directly on all providers;
            //    compare year/month/day components instead.
            return new DashboardStats
            {
                TodaySales = await _db.Sales
                    .Where(s => s.SaleDate.Year == today.Year &&
                                s.SaleDate.Month == today.Month &&
                                s.SaleDate.Day == today.Day)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0,

                TodayPurchases = await _db.Purchases
                    .Where(p => p.PurchaseDate.Year == today.Year &&
                                p.PurchaseDate.Month == today.Month &&
                                p.PurchaseDate.Day == today.Day)
                    .SumAsync(p => (decimal?)p.TotalAmount) ?? 0,

                TotalCustomers = await _db.Customers.CountAsync(c => c.IsActive),
                TotalVendors = await _db.Vendors.CountAsync(v => v.IsActive),
                TotalProducts = await _db.Products.CountAsync(p => p.IsActive),
                LowStockCount = await _db.Products
                    .CountAsync(p => p.IsActive && p.CurrentStock <= p.MinStockLevel),

                PendingReceivables = pendingReceivables,

                MonthSales = await _db.Sales
                    .Where(s => s.SaleDate.Month == today.Month &&
                                s.SaleDate.Year == today.Year)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0,

                MonthPurchases = await _db.Purchases
                    .Where(p => p.PurchaseDate.Month == today.Month &&
                                p.PurchaseDate.Year == today.Year)
                    .SumAsync(p => (decimal?)p.TotalAmount) ?? 0
            };
        }

        public async Task<ProfitLossReport> GetProfitLossAsync(DateTime from, DateTime to)
        {
            var sales = await _db.Sales.Where(s => s.SaleDate >= from && s.SaleDate <= to).SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            var saleReturns = await _db.SaleReturns.Where(r => r.ReturnDate >= from && r.ReturnDate <= to).SumAsync(r => (decimal?)r.TotalAmount) ?? 0;
            var purchases = await _db.Purchases.Where(p => p.PurchaseDate >= from && p.PurchaseDate <= to).SumAsync(p => (decimal?)p.TotalAmount) ?? 0;
            var purchaseReturns = await _db.PurchaseReturns.Where(r => r.ReturnDate >= from && r.ReturnDate <= to).SumAsync(r => (decimal?)r.TotalAmount) ?? 0;
            var expenses = await _db.Expenses.Where(e => e.Date >= from && e.Date <= to).SumAsync(e => (decimal?)e.Amount) ?? 0;
            var salaries = await _db.StaffSalaries.Where(s => s.IsPaid).SumAsync(s => (decimal?)s.NetSalary) ?? 0;
            var commissions = await _db.SaleCommissions.Where(c => c.IsPaid).SumAsync(c => (decimal?)c.CommissionAmount) ?? 0;

            var netSales = sales - saleReturns;
            var netPurchases = purchases - purchaseReturns;
            var grossProfit = netSales - netPurchases;
            var totalExpenses = expenses + salaries + commissions;
            var netProfit = grossProfit - totalExpenses;

            return new ProfitLossReport
            {
                From = from,
                To = to,
                TotalSales = sales,
                SaleReturns = saleReturns,
                NetSales = netSales,
                TotalPurchases = purchases,
                PurchaseReturns = purchaseReturns,
                NetPurchases = netPurchases,
                GrossProfit = grossProfit,
                TotalExpenses = totalExpenses,
                SalaryExpenses = salaries,
                CommissionExpenses = commissions,
                OtherExpenses = expenses,
                NetProfit = netProfit
            };
        }

        public async Task<TrialBalance> GetTrialBalanceAsync(DateTime asOf)
        {
            // ── CREDIT side
            var capital = await _db.InitialCapitals.Where(c => c.Date <= asOf).SumAsync(c => (decimal?)c.Amount) ?? 0;
            var totalSales = await _db.Sales.Where(s => s.SaleDate <= asOf).SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            var saleReturns = await _db.SaleReturns.Where(r => r.ReturnDate <= asOf).SumAsync(r => (decimal?)r.TotalAmount) ?? 0;
            var payables = await _db.Purchases.Where(p => p.PurchaseDate <= asOf)
                                      .SumAsync(p => (decimal?)(p.TotalAmount - p.PaidAmount)) ?? 0;

            // ── DEBIT side
            var totalPurchases = await _db.Purchases.Where(p => p.PurchaseDate <= asOf).SumAsync(p => (decimal?)p.TotalAmount) ?? 0;
            var purchaseReturns = await _db.PurchaseReturns.Where(r => r.ReturnDate <= asOf).SumAsync(r => (decimal?)r.TotalAmount) ?? 0;
            var salaryExpenses = await _db.StaffSalaries.Where(s => s.IsPaid).SumAsync(s => (decimal?)s.NetSalary) ?? 0;
            var commExpenses = await _db.SaleCommissions.Where(c => c.IsPaid).SumAsync(c => (decimal?)c.CommissionAmount) ?? 0;
            var otherExpenses = await _db.Expenses.Where(e => e.Date <= asOf).SumAsync(e => (decimal?)e.Amount) ?? 0;
            var receivables = await _db.Sales.Where(s => s.SaleDate <= asOf && s.IsCredit)
                                      .SumAsync(s => (decimal?)(s.TotalAmount - s.PaidAmount)) ?? 0;
            var inventory = await _db.Products.SumAsync(p => (decimal?)(p.CurrentStock * p.PurchasePrice)) ?? 0;

            // Cash in hand = capital + all cash received - all cash paid out
            var cashReceived = await _db.Sales.Where(s => s.SaleDate <= asOf).SumAsync(s => (decimal?)s.PaidAmount) ?? 0;
            var cashPaid = await _db.Purchases.Where(p => p.PurchaseDate <= asOf).SumAsync(p => (decimal?)p.PaidAmount) ?? 0;
            var cashInHand = capital + cashReceived - cashPaid - salaryExpenses - commExpenses - otherExpenses;

            return new TrialBalance
            {
                AsOf = asOf,
                Capital = capital,
                TotalSales = totalSales,
                SaleReturns = saleReturns,
                TotalPurchases = totalPurchases,
                PurchaseReturns = purchaseReturns,
                SalaryExpenses = salaryExpenses,
                CommExpenses = commExpenses,
                OtherExpenses = otherExpenses,
                Receivables = receivables,
                Payables = payables,
                Inventory = inventory,
                CashInHand = cashInHand > 0 ? cashInHand : 0,
            };
        }
    }

    // ─────────────── INVOICE SERVICE ───────────────
    public interface IInvoiceService
    {
        Task<byte[]> GenerateSaleInvoicePdfAsync(int saleId);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _db;
        public InvoiceService(AppDbContext db) => _db = db;

        public async Task<byte[]> GenerateSaleInvoicePdfAsync(int saleId)
        {
            // PDF generation placeholder - implement with QuestPDF or similar
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }
    }

    // ─────────────── REPORT VIEW MODELS ───────────────
    public class DashboardStats
    {
        public decimal TodaySales { get; set; }
        public decimal TodayPurchases { get; set; }
        public decimal MonthSales { get; set; }
        public decimal MonthPurchases { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalVendors { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public decimal PendingReceivables { get; set; }
    }

    public class ProfitLossReport
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalSales { get; set; }
        public decimal SaleReturns { get; set; }
        public decimal NetSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal PurchaseReturns { get; set; }
        public decimal NetPurchases { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal SalaryExpenses { get; set; }
        public decimal CommissionExpenses { get; set; }
        public decimal OtherExpenses { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class TrialBalance
    {
        public DateTime AsOf { get; set; }

        // ── Credit side (sources)
        public decimal Capital { get; set; }
        public decimal TotalSales { get; set; }
        public decimal SaleReturns { get; set; }
        public decimal Payables { get; set; }          // unpaid supplier invoices

        // ── Debit side (uses)
        public decimal TotalPurchases { get; set; }
        public decimal PurchaseReturns { get; set; }
        public decimal SalaryExpenses { get; set; }
        public decimal CommExpenses { get; set; }
        public decimal OtherExpenses { get; set; }
        public decimal Receivables { get; set; }       // unpaid customer invoices
        public decimal Inventory { get; set; }
        public decimal CashInHand { get; set; }
        public decimal BankBalance { get; set; }

        // ── Derived
        public decimal NetSales => TotalSales - SaleReturns;
        public decimal NetPurchases => TotalPurchases - PurchaseReturns;
        public decimal TotalExpenses => SalaryExpenses + CommExpenses + OtherExpenses;
        public decimal GrossProfit => NetSales - NetPurchases;
        public decimal NetProfit => GrossProfit - TotalExpenses;

        // The balancing figure: if Dr != Cr it goes here
        public decimal TotalDebit => TotalPurchases - PurchaseReturns
                                    + SalaryExpenses + CommExpenses + OtherExpenses
                                    + Receivables + Inventory + CashInHand + BankBalance
                                    + (NetProfit < 0 ? Math.Abs(NetProfit) : 0);   // net loss on Dr side
        public decimal TotalCredit => Capital + TotalSales - SaleReturns + Payables
                                    + (NetProfit > 0 ? NetProfit : 0);              // net profit on Cr side
        public decimal Discrepancy => TotalDebit - TotalCredit;
    }

    // ─────────────── VENDOR LEDGER SERVICE ───────────────
    public interface IVendorLedgerService
    {
        Task AddEntryAsync(int vendorId, string desc, decimal debit, decimal credit, string refType, int refId);
        Task<List<VendorLedger>> GetLedgerAsync(int vendorId, DateTime? from = null, DateTime? to = null);
        Task<decimal> GetBalanceAsync(int vendorId);
    }

    public class VendorLedgerService : IVendorLedgerService
    {
        private readonly AppDbContext _db;
        public VendorLedgerService(AppDbContext db) => _db = db;

        public async Task AddEntryAsync(int vendorId, string desc, decimal debit, decimal credit, string refType, int refId)
        {
            var balance = await GetBalanceAsync(vendorId);
            _db.VendorLedgers.Add(new VendorLedger
            {
                VendorId      = vendorId,
                Description   = desc,
                Debit         = debit,
                Credit        = credit,
                Balance       = balance + credit - debit,  // credit = we owe more, debit = we paid
                ReferenceType = refType,
                ReferenceId   = refId,
                Date          = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        public async Task<List<VendorLedger>> GetLedgerAsync(int vendorId, DateTime? from = null, DateTime? to = null)
        {
            var q = _db.VendorLedgers.Where(l => l.VendorId == vendorId);
            if (from.HasValue) q = q.Where(l => l.Date >= from.Value);
            if (to.HasValue)   q = q.Where(l => l.Date <= to.Value.AddDays(1));
            return await q.OrderBy(l => l.Date).ThenBy(l => l.Id).ToListAsync();
        }

        public async Task<decimal> GetBalanceAsync(int vendorId)
        {
            var last = await _db.VendorLedgers.Where(l => l.VendorId == vendorId)
                .OrderByDescending(l => l.Id).FirstOrDefaultAsync();
            return last?.Balance ?? 0;
        }
    }
}
