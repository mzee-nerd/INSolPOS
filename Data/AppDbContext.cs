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
        public DbSet<VendorLedger> VendorLedgers { get; set; }
        public DbSet<VendorPayment> VendorPayments { get; set; }
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

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Decimal precision
            mb.Entity<Product>().Property(p => p.PurchasePrice).HasPrecision(18, 4);
            mb.Entity<Product>().Property(p => p.SalePrice).HasPrecision(18, 4);
            mb.Entity<Product>().Property(p => p.WholesalePrice).HasPrecision(18, 4);
            mb.Entity<Product>().Property(p => p.CommissionPercentage).HasPrecision(5, 2);
            mb.Entity<Product>().Property(p => p.CurrentStock).HasPrecision(18, 4);
            mb.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
            mb.Entity<Sale>().Property(s => s.PaidAmount).HasPrecision(18, 2);
            mb.Entity<Purchase>().Property(p => p.TotalAmount).HasPrecision(18, 2);
            mb.Entity<CustomerLedger>().Property(l => l.Balance).HasPrecision(18, 2);
            mb.Entity<CustomerLedger>().Property(l => l.Debit).HasPrecision(18, 2);
            mb.Entity<CustomerLedger>().Property(l => l.Credit).HasPrecision(18, 2);
            mb.Entity<VendorLedger>().Property(l => l.Balance).HasPrecision(18, 2);
            mb.Entity<VendorLedger>().Property(l => l.Debit).HasPrecision(18, 2);
            mb.Entity<VendorLedger>().Property(l => l.Credit).HasPrecision(18, 2);

            // Ignore computed properties (not mapped to DB columns)
            mb.Entity<Purchase>().Ignore(p => p.DueAmount);
            mb.Entity<Sale>().Ignore(s => s.DueAmount);
            mb.Entity<SaleItem>().Ignore(i => i.TotalQty).Ignore(i => i.TotalAmount);
            mb.Entity<PurchaseItem>().Ignore(i => i.TotalQty).Ignore(i => i.TotalAmount);
            mb.Entity<PurchaseReturnItem>().Ignore(i => i.Amount);
            mb.Entity<SaleReturnItem>().Ignore(i => i.Amount);
            mb.Entity<VehicleLoadItem>().Ignore(i => i.SoldQty);

            // Seed super admin
            mb.Entity<ApplicationUser>().HasData(new ApplicationUser
            {
                Id = 1, FullName = "Super Admin", Username = "superadmin",
                PasswordHash = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes("Admin@123_INSolSalt2024")),
                Role = UserRole.SuperAdmin, IsActive = true, CreatedAt = new DateTime(2024, 1, 1)
            });

            mb.Entity<Unit>().HasData(
                new Unit { Id = 1, Name = "Piece",    Abbreviation = "Pcs" },
                new Unit { Id = 2, Name = "Carton",   Abbreviation = "Ctn" },
                new Unit { Id = 3, Name = "Kilogram", Abbreviation = "Kg"  },
                new Unit { Id = 4, Name = "Liter",    Abbreviation = "Ltr" },
                new Unit { Id = 5, Name = "Dozen",    Abbreviation = "Dz"  },
                new Unit { Id = 6, Name = "Pack",     Abbreviation = "Pk"  }
            );

            mb.Entity<Category>().HasData(
                new Category { Id = 1, Name = "General"          },
                new Category { Id = 2, Name = "Food & Beverages" },
                new Category { Id = 3, Name = "Electronics"      },
                new Category { Id = 4, Name = "Dairy Products"   },
                new Category { Id = 5, Name = "Snacks"           }
            );

            mb.Entity<Warehouse>().HasData(
                new Warehouse { Id = 1, Name = "Main Warehouse", Location = "Head Office", IsActive = true }
            );
        }
    }
}
