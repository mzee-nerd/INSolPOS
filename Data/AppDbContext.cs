using Microsoft.EntityFrameworkCore;
using INSolPOS.Models;

namespace INSolPOS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<StaffSalary> StaffSalaries { get; set; }
        public DbSet<AdvanceSalary> AdvanceSalaries { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseStock> WarehouseStocks { get; set; }
        public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; }
        public DbSet<WarehouseTransferItem> WarehouseTransferItems { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleLoad> VehicleLoads { get; set; }
        public DbSet<VehicleLoadItem> VehicleLoadItems { get; set; }
        public DbSet<SaleRoute> SaleRoutes { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
        public DbSet<PurchaseReturnItem> PurchaseReturnItems { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<SaleReturn> SaleReturns { get; set; }
        public DbSet<SaleReturnItem> SaleReturnItems { get; set; }
        public DbSet<SaleCommission> SaleCommissions { get; set; }
        public DbSet<CustomerLedger> CustomerLedgers { get; set; }
        public DbSet<PaymentReceipt> PaymentReceipts { get; set; }
        public DbSet<InitialCapital> InitialCapitals { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<VendorLedger> VendorLedgers { get; set; }
        public DbSet<VendorPayment> VendorPayments { get; set; }
        public DbSet<CompanySettings> CompanySettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Decimal precision
            modelBuilder.Entity<Product>().Property(p => p.PurchasePrice).HasPrecision(18, 4);
            modelBuilder.Entity<Product>().Property(p => p.SalePrice).HasPrecision(18, 4);
            modelBuilder.Entity<Product>().Property(p => p.CommissionPercentage).HasPrecision(5, 2);
            modelBuilder.Entity<Product>().Property(p => p.CurrentStock).HasPrecision(18, 4);

            modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Sale>().Property(s => s.PaidAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Purchase>().Property(p => p.TotalAmount).HasPrecision(18, 2);

            modelBuilder.Entity<CustomerLedger>().Property(l => l.Balance).HasPrecision(18, 2);
            modelBuilder.Entity<CustomerLedger>().Property(l => l.Debit).HasPrecision(18, 2);
            modelBuilder.Entity<CustomerLedger>().Property(l => l.Credit).HasPrecision(18, 2);

            // Ignore computed properties
            modelBuilder.Entity<Purchase>().Ignore(p => p.DueAmount);
            modelBuilder.Entity<Sale>().Ignore(s => s.DueAmount);
            modelBuilder.Entity<SaleItem>().Ignore(i => i.TotalQty).Ignore(i => i.TotalAmount);
            modelBuilder.Entity<PurchaseItem>().Ignore(i => i.TotalQty).Ignore(i => i.TotalAmount);
            modelBuilder.Entity<PurchaseReturnItem>().Ignore(i => i.Amount);
            modelBuilder.Entity<SaleReturnItem>().Ignore(i => i.Amount);
            modelBuilder.Entity<VehicleLoadItem>().Ignore(i => i.SoldQty);

            // Seed default super admin
            modelBuilder.Entity<ApplicationUser>().HasData(new ApplicationUser
            {
                Id = 1,
                FullName = "Super Admin",
                Username = "superadmin",
                PasswordHash = BCryptHash("Admin@123"),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1)
            });

            // Seed default units
            modelBuilder.Entity<Unit>().HasData(
                new Unit { Id = 1, Name = "Piece", Abbreviation = "Pcs" },
                new Unit { Id = 2, Name = "Carton", Abbreviation = "Ctn" },
                new Unit { Id = 3, Name = "Kilogram", Abbreviation = "Kg" },
                new Unit { Id = 4, Name = "Liter", Abbreviation = "Ltr" },
                new Unit { Id = 5, Name = "Dozen", Abbreviation = "Dz" }
            );

            // Seed default categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "General" },
                new Category { Id = 2, Name = "Food & Beverages" },
                new Category { Id = 3, Name = "Electronics" }
            );
        }

        private static string BCryptHash(string password)
        {
            // Simple hash placeholder - in production use BCrypt.Net
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password + "_hashed"));
        }
    }
}
