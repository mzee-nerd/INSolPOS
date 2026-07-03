using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using INSolPOS.Data;
using INSolPOS.Helpers;
using INSolPOS.Models;
using INSolPOS.Services;

namespace INSolPOS.Controllers
{
    // ─────────────── USERS ───────────────
    public class UsersController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly IAuthService _auth;
        public UsersController(AppDbContext db, IAuthService auth) { _db = db; _auth = auth; }

        public async Task<IActionResult> Index() => View(await _db.Users.ToListAsync());

        public IActionResult Create() => View(new ApplicationUser());

        [HttpPost]
        public async Task<IActionResult> Create(ApplicationUser user, string password)
        {
            user.PasswordHash = _auth.HashPassword(password);
            user.CreatedAt = DateTime.Now;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = "User created successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id) => View(await _db.Users.FindAsync(id));

        [HttpPost]
        public async Task<IActionResult> Edit(ApplicationUser user)
        {
            var existing = await _db.Users.FindAsync(user.Id);
            if (existing == null) return NotFound();
            existing.FullName = user.FullName; existing.Username = user.Username;
            existing.Role = user.Role; existing.IsActive = user.IsActive;
            existing.Phone = user.Phone; existing.Email = user.Email;
            await _db.SaveChangesAsync();
            TempData["Success"] = "User updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null) { user.IsActive = false; await _db.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }
    }

    // ─────────────── CUSTOMERS ───────────────
    public class CustomersController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly ILedgerService _ledger;
        public CustomersController(AppDbContext db, ILedgerService ledger) { _db = db; _ledger = ledger; }

        public async Task<IActionResult> Index() =>
            View(await _db.Customers.Include(c => c.Route).Where(c => c.IsActive).ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewBag.Routes = await _db.SaleRoutes.Where(r => r.IsActive).ToListAsync();
            return View(new Customer());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            customer.RegisteredAt = DateTime.Now;
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            if (customer.OpeningBalance > 0)
                await _ledger.AddEntryAsync(customer.Id, "Opening Balance", customer.OpeningBalance, 0, "Opening", customer.Id);

            TempData["Success"] = "Customer registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Routes = await _db.SaleRoutes.Where(r => r.IsActive).ToListAsync();
            return View(await _db.Customers.FindAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer customer)
        {
            var existing = await _db.Customers.FindAsync(customer.Id);
            if (existing == null) return NotFound();
            existing.Name = customer.Name; existing.Phone = customer.Phone;
            existing.Address = customer.Address; existing.CNIC = customer.CNIC;
            existing.Email = customer.Email; existing.RouteId = customer.RouteId;
            existing.CreditLimit = customer.CreditLimit;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer updated.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Ledger(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            var ledger = await _ledger.GetCustomerLedgerAsync(id);
            var balance = await _ledger.GetCustomerBalanceAsync(id);
            ViewBag.Customer = customer;
            ViewBag.Balance = balance;
            return View(ledger);
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment(int customerId, decimal amount, string? notes)
        {
            await _ledger.AddEntryAsync(customerId, notes ?? "Payment Received", 0, amount, "Payment", 0);
            _db.PaymentReceipts.Add(new PaymentReceipt
            {
                CustomerId = customerId,
                Amount = amount,
                Notes = notes,
                Method = PaymentMethod.Cash,
                ReceivedByUserId = CurrentUserId!.Value,
                Date = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Payment of Rs.{amount:N2} recorded.";
            return RedirectToAction("Ledger", new { id = customerId });
        }
    }

    // ─────────────── VENDORS ───────────────
    public class VendorsController : BaseController
    {
        private readonly AppDbContext _db;
        public VendorsController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.Vendors.Where(v => v.IsActive).ToListAsync());

        public IActionResult Create() => View(new Vendor());

        [HttpPost]
        public async Task<IActionResult> Create(Vendor vendor)
        {
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vendor registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id) => View(await _db.Vendors.FindAsync(id));

        [HttpPost]
        public async Task<IActionResult> Edit(Vendor vendor)
        {
            var existing = await _db.Vendors.FindAsync(vendor.Id);
            if (existing == null) return NotFound();
            existing.Name = vendor.Name; existing.Phone = vendor.Phone;
            existing.Address = vendor.Address; existing.CNIC = vendor.CNIC;
            existing.CompanyName = vendor.CompanyName;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vendor updated.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── STAFF ───────────────
    public class StaffController : BaseController
    {
        private readonly AppDbContext _db;
        public StaffController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.Staff.Where(s => s.IsActive).ToListAsync());

        public IActionResult Create() => View(new Staff());

        [HttpPost]
        public async Task<IActionResult> Create(Staff staff)
        {
            _db.Staff.Add(staff);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Staff registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id) => View(await _db.Staff.FindAsync(id));

        [HttpPost]
        public async Task<IActionResult> Edit(Staff staff)
        {
            var existing = await _db.Staff.FindAsync(staff.Id);
            if (existing == null) return NotFound();
            existing.Name = staff.Name; existing.Phone = staff.Phone;
            existing.CNIC = staff.CNIC; existing.Designation = staff.Designation;
            existing.BasicSalary = staff.BasicSalary; existing.Address = staff.Address;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Staff updated.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Salaries(int id)
        {
            var staff = await _db.Staff.FindAsync(id);
            ViewBag.Staff = staff;
            var salaries = await _db.StaffSalaries.Where(s => s.StaffId == id).OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ToListAsync();
            return View(salaries);
        }

        [HttpPost]
        public async Task<IActionResult> PaySalary(int staffId, int month, int year, decimal allowances, decimal deductions)
        {
            var staff = await _db.Staff.FindAsync(staffId);
            if (staff == null) return NotFound();

            var advanceDeduction = await _db.AdvanceSalaries
                .Where(a => a.StaffId == staffId && !a.IsRecovered).SumAsync(a => (decimal?)a.Amount) ?? 0;

            var salary = new StaffSalary
            {
                StaffId = staffId,
                Month = month,
                Year = year,
                BasicSalary = staff.BasicSalary,
                Allowances = allowances,
                Deductions = deductions,
                AdvanceDeduction = advanceDeduction,
                NetSalary = staff.BasicSalary + allowances - deductions - advanceDeduction,
                IsPaid = true,
                PaidOn = DateTime.Now
            };
            _db.StaffSalaries.Add(salary);

            // Mark advances as recovered
            var advances = await _db.AdvanceSalaries.Where(a => a.StaffId == staffId && !a.IsRecovered).ToListAsync();
            advances.ForEach(a => a.IsRecovered = true);

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Salary paid for {month}/{year}.";
            return RedirectToAction("Salaries", new { id = staffId });
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdvance(int staffId, decimal amount, string reason)
        {
            _db.AdvanceSalaries.Add(new AdvanceSalary
            {
                StaffId = staffId,
                Amount = amount,
                Reason = reason,
                Date = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Advance of Rs.{amount:N2} given.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Commissions()
        {
            var commissions = await _db.SaleCommissions
                .Include(c => c.Staff).Include(c => c.Sale)
                .OrderByDescending(c => c.Sale!.SaleDate).ToListAsync();
            return View(commissions);
        }
    }

    // ─────────────── PRODUCTS ───────────────
    public class ProductsController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly IProductService _productService;
        public ProductsController(AppDbContext db, IProductService productService) { _db = db; _productService = productService; }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.Include(p => p.Category).Include(p => p.Unit).Where(p => p.IsActive).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Units = await _db.Units.ToListAsync();
            return View(new Product());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            if (string.IsNullOrEmpty(product.Barcode))
                product.Barcode = _productService.GenerateBarcode();
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Categories = await _db.Categories.ToListAsync();
            ViewBag.Units = await _db.Units.ToListAsync();
            return View(await _db.Products.FindAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            var existing = await _db.Products.FindAsync(product.Id);
            if (existing == null) return NotFound();
            existing.Name = product.Name; existing.Barcode = product.Barcode;
            existing.CategoryId = product.CategoryId; existing.UnitId = product.UnitId;
            existing.CartonQty = product.CartonQty; existing.PurchasePrice = product.PurchasePrice;
            existing.SalePrice = product.SalePrice; existing.WholesalePrice = product.WholesalePrice;
            existing.CommissionPercentage = product.CommissionPercentage;
            existing.ExpiryDate = product.ExpiryDate; existing.BatchNo = product.BatchNo;
            existing.MinStockLevel = product.MinStockLevel; existing.TrackExpiry = product.TrackExpiry;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product updated.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Units() => View(await _db.Units.ToListAsync());

        [HttpPost]
        public async Task<IActionResult> CreateUnit(Unit unit)
        {
            _db.Units.Add(unit);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Unit added.";
            return RedirectToAction("Units");
        }

        public async Task<IActionResult> Categories() => View(await _db.Categories.ToListAsync());

        [HttpPost]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category added.";
            return RedirectToAction("Categories");
        }

        public async Task<JsonResult> GetByBarcode(string barcode)
        {
            var product = await _db.Products.Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
            if (product == null) return Json(new { success = false });
            return Json(new
            {
                success = true,
                id = product.Id,
                name = product.Name,
                salePrice = product.SalePrice,
                purchasePrice = product.PurchasePrice,
                stock = product.CurrentStock,
                barcode = product.Barcode,
                commissionPct = product.CommissionPercentage,
                cartonQty = product.CartonQty,
                unit = product.Unit?.Name
            });
        }

        public async Task<JsonResult> Search(string q)
        {
            var products = await _db.Products
                .Where(p => p.IsActive && (p.Name.Contains(q) || (p.Barcode != null && p.Barcode.Contains(q))))
                .Take(10).Select(p => new { p.Id, p.Name, p.Barcode, p.SalePrice, p.CurrentStock, p.CartonQty, p.CommissionPercentage })
                .ToListAsync();
            return Json(products);
        }
    }

    // ─────────────── WAREHOUSE ───────────────
    public class WarehouseController : BaseController
    {
        private readonly AppDbContext _db;
        public WarehouseController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.Warehouses.Where(w => w.IsActive).ToListAsync());

        public IActionResult Create() => View(new Warehouse());

        [HttpPost]
        public async Task<IActionResult> Create(Warehouse warehouse)
        {
            _db.Warehouses.Add(warehouse);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Warehouse registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Stock(int id)
        {
            var warehouse = await _db.Warehouses.FindAsync(id);
            var stock = await _db.WarehouseStocks.Include(s => s.Product)
                .Where(s => s.WarehouseId == id).ToListAsync();
            ViewBag.Warehouse = warehouse;
            return View(stock);
        }

        public async Task<IActionResult> Transfer()
        {
            ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            ViewBag.Products = await _db.Products.Where(p => p.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(int fromWarehouseId, int toWarehouseId, List<int> productIds, List<decimal> quantities)
        {
            var transfer = new WarehouseTransfer
            {
                FromWarehouseId = fromWarehouseId,
                ToWarehouseId = toWarehouseId,
                TransferDate = DateTime.Now
            };
            for (int i = 0; i < productIds.Count; i++)
            {
                transfer.Items.Add(new WarehouseTransferItem
                {
                    ProductId = productIds[i],
                    Quantity = quantities[i]
                });
                // Update warehouse stocks
                var fromStock = await _db.WarehouseStocks
                    .FirstOrDefaultAsync(s => s.WarehouseId == fromWarehouseId && s.ProductId == productIds[i]);
                if (fromStock != null) fromStock.Quantity -= quantities[i];

                var toStock = await _db.WarehouseStocks
                    .FirstOrDefaultAsync(s => s.WarehouseId == toWarehouseId && s.ProductId == productIds[i]);
                if (toStock != null) toStock.Quantity += quantities[i];
                else _db.WarehouseStocks.Add(new WarehouseStock
                {
                    WarehouseId = toWarehouseId,
                    ProductId = productIds[i],
                    Quantity = quantities[i]
                });
            }
            _db.WarehouseTransfers.Add(transfer);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Transfer completed.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── VEHICLES ───────────────
    public class VehiclesController : BaseController
    {
        private readonly AppDbContext _db;
        public VehiclesController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.Vehicles.Where(v => v.IsActive).ToListAsync());

        public IActionResult Create() => View(new Vehicle());

        [HttpPost]
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vehicle registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> LoadVehicle(int id)
        {
            ViewBag.Vehicle = await _db.Vehicles.FindAsync(id);
            ViewBag.Products = await _db.Products.Where(p => p.IsActive && p.CurrentStock > 0).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoadVehicle(int vehicleId, List<int> productIds, List<decimal> quantities)
        {
            var load = new VehicleLoad { VehicleId = vehicleId, LoadDate = DateTime.Now, Status = LoadStatus.Loaded };
            for (int i = 0; i < productIds.Count; i++)
            {
                load.Items.Add(new VehicleLoadItem { ProductId = productIds[i], LoadedQty = quantities[i] });
                await DeductStock(productIds[i], quantities[i]);
            }
            _db.VehicleLoads.Add(load);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vehicle loaded.";
            return RedirectToAction("Index");
        }

        private async Task DeductStock(int productId, decimal qty)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product != null) { product.CurrentStock -= qty; }
        }

        public async Task<IActionResult> UnloadVehicle(int loadId)
        {
            var load = await _db.VehicleLoads.Include(l => l.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(l => l.Id == loadId);
            return View(load);
        }

        [HttpPost]
        public async Task<IActionResult> UnloadVehicle(int loadId, List<int> itemIds, List<decimal> unloadedQtys)
        {
            var load = await _db.VehicleLoads.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == loadId);
            if (load == null) return NotFound();
            for (int i = 0; i < itemIds.Count; i++)
            {
                var item = load.Items.FirstOrDefault(x => x.Id == itemIds[i]);
                if (item != null)
                {
                    item.UnloadedQty = unloadedQtys[i];
                    // Return unloaded stock
                    var product = await _db.Products.FindAsync(item.ProductId);
                    if (product != null) product.CurrentStock += unloadedQtys[i];
                }
            }
            load.Status = LoadStatus.Unloaded;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vehicle unloaded.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── ROUTES ───────────────
    public class RoutesController : BaseController
    {
        private readonly AppDbContext _db;
        public RoutesController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index() => View(await _db.SaleRoutes.Where(r => r.IsActive).ToListAsync());

        public IActionResult Create() => View(new SaleRoute());

        [HttpPost]
        public async Task<IActionResult> Create(SaleRoute route)
        {
            _db.SaleRoutes.Add(route);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Route registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id) => View(await _db.SaleRoutes.FindAsync(id));

        [HttpPost]
        public async Task<IActionResult> Edit(SaleRoute route)
        {
            var existing = await _db.SaleRoutes.FindAsync(route.Id);
            if (existing == null) return NotFound();
            existing.Name = route.Name; existing.Area = route.Area; existing.Description = route.Description;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Route updated.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── PURCHASES ───────────────
    public class PurchasesController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly IPurchaseService _purchaseService;
        public PurchasesController(AppDbContext db, IPurchaseService purchaseService) { _db = db; _purchaseService = purchaseService; }

        public async Task<IActionResult> Index()
        {
            var purchases = await _db.Purchases.Include(p => p.Vendor)
                .OrderByDescending(p => p.PurchaseDate).Take(100).ToListAsync();
            return View(purchases);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Vendors = await _db.Vendors.Where(v => v.IsActive).ToListAsync();
            ViewBag.Products = await _db.Products.Where(p => p.IsActive).ToListAsync();
            return View(new Purchase());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Purchase purchase, List<int> productIds,
            List<decimal> cartonQtys, List<decimal> looseQtys, List<decimal> prices,
            List<string?> batchNos, List<DateTime?> expiryDates)
        {
            purchase.CreatedByUserId = CurrentUserId!.Value;
            for (int i = 0; i < productIds.Count; i++)
            {
                var item = new PurchaseItem
                {
                    ProductId = productIds[i],
                    CartonQty = cartonQtys[i],
                    LooseQty = looseQtys[i],
                    PurchasePrice = prices[i],
                    BatchNo = batchNos.Count > i ? batchNos[i] : null,
                    ExpiryDate = expiryDates.Count > i ? expiryDates[i] : null
                };
                var product = await _db.Products.FindAsync(productIds[i]);
                item.Product = product; // for computed property
                purchase.Items.Add(item);
            }
            purchase.SubTotal = purchase.Items.Sum(i => i.TotalAmount);
            purchase.TotalAmount = purchase.SubTotal + purchase.Tax - purchase.Discount;
            await _purchaseService.CreatePurchaseAsync(purchase);
            TempData["Success"] = $"Purchase {purchase.InvoiceNo} recorded.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _db.Purchases.Include(p => p.Vendor).Include(p => p.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);
            return View(purchase);
        }

        public async Task<IActionResult> Return(int purchaseId)
        {
            var purchase = await _db.Purchases.Include(p => p.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);
            ViewBag.Purchase = purchase;
            return View(new PurchaseReturn { PurchaseId = purchaseId });
        }

        [HttpPost]
        public async Task<IActionResult> Return(PurchaseReturn ret, List<int> productIds, List<decimal> quantities, List<decimal> prices)
        {
            for (int i = 0; i < productIds.Count; i++)
            {
                if (quantities[i] > 0)
                    ret.Items.Add(new PurchaseReturnItem { ProductId = productIds[i], Quantity = quantities[i], Price = prices[i] });
            }
            ret.TotalAmount = ret.Items.Sum(i => i.Amount);
            await _purchaseService.CreatePurchaseReturnAsync(ret);
            TempData["Success"] = "Purchase return recorded.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── SALES ───────────────
    public class SalesController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly ISaleService _saleService;
        public SalesController(AppDbContext db, ISaleService saleService) { _db = db; _saleService = saleService; }

        public async Task<IActionResult> Index()
        {
            var sales = await _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .OrderByDescending(s => s.SaleDate).Take(100).ToListAsync();
            return View(sales);
        }

        public async Task<IActionResult> NewSale()
        {
            ViewBag.Customers = await _db.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Staff = await _db.Staff.Where(s => s.IsActive).ToListAsync();
            ViewBag.Products = await _db.Products.Where(p => p.IsActive).ToListAsync();
            return View(new Sale());
        }

        [HttpPost]
        public async Task<IActionResult> NewSale(Sale sale, List<int> productIds,
            List<decimal> cartonQtys, List<decimal> looseQtys, List<decimal> prices, List<decimal> discounts)
        {
            sale.CreatedByUserId = CurrentUserId!.Value;
            sale.SaleDate = DateTime.Now;
            for (int i = 0; i < productIds.Count; i++)
            {
                var product = await _db.Products.FindAsync(productIds[i]);
                var item = new SaleItem
                {
                    ProductId = productIds[i],
                    CartonQty = cartonQtys[i],
                    LooseQty = looseQtys[i],
                    SalePrice = prices[i],
                    Discount = discounts.Count > i ? discounts[i] : 0,
                    Product = product
                };
                sale.Items.Add(item);
            }
            sale.SubTotal = sale.Items.Sum(i => i.TotalAmount);
            sale.TotalAmount = sale.SubTotal + sale.Tax - sale.Discount;
            sale.IsCredit = sale.SaleType == SaleType.Credit;

            var result = await _saleService.CreateSaleAsync(sale);
            TempData["Success"] = $"Sale {result.InvoiceNo} completed!";
            return RedirectToAction("Receipt", new { id = result.Id });
        }

        public async Task<IActionResult> Receipt(int id)
        {
            var sale = await _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
            return View(sale);
        }

        public async Task<IActionResult> Details(int id)
        {
            var sale = await _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
            return View(sale);
        }

        public async Task<IActionResult> Return(int saleId)
        {
            var sale = await _db.Sales.Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == saleId);
            ViewBag.Sale = sale;
            return View(new SaleReturn { SaleId = saleId });
        }

        [HttpPost]
        public async Task<IActionResult> Return(SaleReturn ret, List<int> productIds, List<decimal> quantities, List<decimal> prices)
        {
            for (int i = 0; i < productIds.Count; i++)
            {
                if (quantities[i] > 0)
                    ret.Items.Add(new SaleReturnItem { ProductId = productIds[i], Quantity = quantities[i], Price = prices[i] });
            }
            ret.TotalAmount = ret.Items.Sum(i => i.Amount);
            await _saleService.CreateSaleReturnAsync(ret);
            TempData["Success"] = "Sale return recorded.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── REPORTS ───────────────
    public class ReportsController : BaseController
    {
        private readonly IReportService _reports;
        private readonly AppDbContext _db;
        public ReportsController(IReportService reports, AppDbContext db) { _reports = reports; _db = db; }

        public IActionResult Index() => View();

        public async Task<IActionResult> ProfitLoss(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to ?? DateTime.Today;
            var report = await _reports.GetProfitLossAsync(f, t);
            return View(report);
        }

        public async Task<IActionResult> TrialBalance(DateTime? asOf)
        {
            var date = asOf ?? DateTime.Today;
            var report = await _reports.GetTrialBalanceAsync(date);
            return View(report);
        }

        public async Task<IActionResult> SalesReport(DateTime? from, DateTime? to, int? customerId, int? staffId)
        {
            var f = from ?? DateTime.Today;
            var t = to ?? DateTime.Today;
            var query = _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .Where(s => s.SaleDate.Date >= f && s.SaleDate.Date <= t);
            if (customerId.HasValue) query = query.Where(s => s.CustomerId == customerId);
            if (staffId.HasValue) query = query.Where(s => s.StaffId == staffId);
            ViewBag.Customers = await _db.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Staff = await _db.Staff.Where(s => s.IsActive).ToListAsync();
            ViewBag.From = f; ViewBag.To = t;
            return View(await query.OrderByDescending(s => s.SaleDate).ToListAsync());
        }

        public async Task<IActionResult> StockReport()
        {
            var products = await _db.Products.Include(p => p.Category).Include(p => p.Unit)
                .Where(p => p.IsActive).OrderBy(p => p.Category!.Name).ThenBy(p => p.Name).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> ExpiryReport()
        {
            var products = await _db.Products.Where(p => p.IsActive && p.ExpiryDate.HasValue)
                .OrderBy(p => p.ExpiryDate).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> CommissionReport(int? staffId, DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to ?? DateTime.Today;
            var query = _db.SaleCommissions.Include(c => c.Staff).Include(c => c.Sale)
                .Where(c => c.Sale!.SaleDate >= f && c.Sale.SaleDate <= t);
            if (staffId.HasValue) query = query.Where(c => c.StaffId == staffId);
            ViewBag.Staff = await _db.Staff.Where(s => s.IsActive).ToListAsync();
            ViewBag.From = f; ViewBag.To = t;
            return View(await query.ToListAsync());
        }
    }

    // ─────────────── CAPITAL ───────────────
    public class CapitalController : BaseController
    {
        private readonly AppDbContext _db;
        public CapitalController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var capitals = await _db.InitialCapitals.OrderByDescending(c => c.Date).ToListAsync();
            ViewBag.Total = capitals.Sum(c => c.Amount);
            return View(capitals);
        }

        [HttpPost]
        public async Task<IActionResult> Add(InitialCapital capital)
        {
            _db.InitialCapitals.Add(capital);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Capital entry added.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── EXPENSES ───────────────
    public class ExpensesController : BaseController
    {
        private readonly AppDbContext _db;
        public ExpensesController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? category)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to ?? DateTime.Today;
            var query = _db.Expenses
                .Where(e => e.Date.Year >= f.Year && e.Date.Month >= f.Month && e.Date.Day >= f.Day
                         || e.Date.Year <= t.Year && e.Date.Month <= t.Month && e.Date.Day <= t.Day);
            // simpler date filter using stored dates
            var all = await _db.Expenses.OrderByDescending(e => e.Date).ToListAsync();
            var filtered = all.Where(e => e.Date.Date >= f && e.Date.Date <= t);
            if (!string.IsNullOrEmpty(category))
                filtered = filtered.Where(e => e.Category == category);
            var list = filtered.ToList();

            ViewBag.From = f;
            ViewBag.To = t;
            ViewBag.Categories = all.Select(e => e.Category).Where(c => c != null).Distinct().ToList();
            ViewBag.Total = list.Sum(e => e.Amount);
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> Add(Expense expense)
        {
            expense.CreatedByUserId = CurrentUserId!.Value;
            if (expense.Date == default) expense.Date = DateTime.Now;
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Expense of Rs.{expense.Amount:N2} recorded.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var exp = await _db.Expenses.FindAsync(id);
            if (exp != null) { _db.Expenses.Remove(exp); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Expense deleted.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── SALE RETURNS (standalone) ───────────────
    public class SaleReturnsController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly ISaleService _saleService;
        public SaleReturnsController(AppDbContext db, ISaleService saleService)
        { _db = db; _saleService = saleService; }

        public async Task<IActionResult> Index()
        {
            var returns = await _db.SaleReturns
                .Include(r => r.Sale).ThenInclude(s => s!.Customer)
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .OrderByDescending(r => r.ReturnDate).Take(100).ToListAsync();
            return View(returns);
        }

        public async Task<IActionResult> Create()
        {
            var recentSales = await _db.Sales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate).Take(60).ToListAsync();
            return View(recentSales);
        }

        public async Task<IActionResult> FromSale(int saleId)
        {
            var sale = await _db.Sales
                .Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == saleId);
            if (sale == null) return NotFound();
            ViewBag.Sale = sale;
            return View(new SaleReturn { SaleId = saleId });
        }

        [HttpPost]
        public async Task<IActionResult> FromSale(SaleReturn ret,
            List<int> productIds, List<decimal> quantities, List<decimal> prices)
        {
            for (int i = 0; i < productIds.Count; i++)
                if (quantities[i] > 0)
                    ret.Items.Add(new SaleReturnItem
                    { ProductId = productIds[i], Quantity = quantities[i], Price = prices[i] });

            if (!ret.Items.Any())
            {
                TempData["Error"] = "Enter at least one return quantity.";
                return RedirectToAction("FromSale", new { saleId = ret.SaleId });
            }

            ret.TotalAmount = ret.Items.Sum(i => i.Quantity * i.Price);
            await _saleService.CreateSaleReturnAsync(ret);

            var sale = await _db.Sales.FindAsync(ret.SaleId);
            if (sale != null && sale.IsCredit && sale.CustomerId.HasValue)
            {
                var last = await _db.CustomerLedgers
                    .Where(l => l.CustomerId == sale.CustomerId.Value)
                    .OrderByDescending(l => l.Id).FirstOrDefaultAsync();
                _db.CustomerLedgers.Add(new CustomerLedger
                {
                    CustomerId = sale.CustomerId.Value,
                    Description = $"Sale Return — Invoice #{sale.InvoiceNo}",
                    Debit = 0,
                    Credit = ret.TotalAmount,
                    Balance = (last?.Balance ?? 0) - ret.TotalAmount,
                    Date = DateTime.Now,
                    ReferenceType = "SaleReturn",
                    ReferenceId = ret.Id
                });
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = $"Sale return of Rs.{ret.TotalAmount:N2} processed.";
            return RedirectToAction("Index");
        }
    }
}