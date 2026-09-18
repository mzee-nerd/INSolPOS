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
            if (await _db.Users.AnyAsync(u => u.Username == user.Username))
            { TempData["Error"] = "Username already exists."; return RedirectToAction("Index"); }
            user.PasswordHash = _auth.HashPassword(password);
            user.CreatedAt = DateTime.Now;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"User '{user.FullName}' created.";
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
            if (id == CurrentUserId) { TempData["Error"] = "Cannot deactivate your own account."; return RedirectToAction("Index"); }
            var user = await _db.Users.FindAsync(id);
            if (user != null) { user.IsActive = false; await _db.SaveChangesAsync(); }
            TempData["Success"] = "User deactivated.";
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
            View(await _db.Customers.Include(c => c.Route).Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync());

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
            TempData["Success"] = $"Customer '{customer.Name}' registered.";
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
            var e = await _db.Customers.FindAsync(customer.Id);
            if (e == null) return NotFound();
            e.Name = customer.Name; e.Phone = customer.Phone; e.Address = customer.Address;
            e.CNIC = customer.CNIC; e.Email = customer.Email; e.RouteId = customer.RouteId;
            e.CreditLimit = customer.CreditLimit;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Customer updated.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Ledger(int id, DateTime? from, DateTime? to)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            var entries = await _ledger.GetCustomerLedgerAsync(id, from, to);
            var balance = await _ledger.GetCustomerBalanceAsync(id);
            ViewBag.Customer = customer;
            ViewBag.Balance = balance;
            ViewBag.From = from;
            ViewBag.To = to;
            return View(entries);
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment(int customerId, decimal amount, string? method, string? chequeNo, string? notes)
        {
            if (amount <= 0) { TempData["Error"] = "Amount must be greater than zero."; return RedirectToAction("Ledger", new { id = customerId }); }
            await _ledger.AddEntryAsync(customerId, notes ?? "Payment Received", 0, amount, "Payment", 0);
            _db.PaymentReceipts.Add(new PaymentReceipt
            {
                CustomerId = customerId,
                Amount = amount,
                Notes = notes,
                Method = PaymentMethod.Cash,
                ChequeNo = chequeNo,
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
        private readonly IVendorLedgerService _vendorLedger;
        public VendorsController(AppDbContext db, IVendorLedgerService vendorLedger) { _db = db; _vendorLedger = vendorLedger; }

        public async Task<IActionResult> Index() =>
            View(await _db.Vendors.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync());

        public IActionResult Create() => View(new Vendor());

        [HttpPost]
        public async Task<IActionResult> Create(Vendor vendor)
        {
            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();
            if (vendor.OpeningBalance > 0)
                await _vendorLedger.AddEntryAsync(vendor.Id, "Opening Balance", 0, vendor.OpeningBalance, "Opening", vendor.Id);
            TempData["Success"] = $"Vendor '{vendor.Name}' registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id) => View(await _db.Vendors.FindAsync(id));

        [HttpPost]
        public async Task<IActionResult> Edit(Vendor vendor)
        {
            var e = await _db.Vendors.FindAsync(vendor.Id);
            if (e == null) return NotFound();
            e.Name = vendor.Name; e.Phone = vendor.Phone; e.Address = vendor.Address;
            e.CNIC = vendor.CNIC; e.CompanyName = vendor.CompanyName;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vendor updated.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Ledger(int id, DateTime? from, DateTime? to)
        {
            var vendor = await _db.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();
            var entries = await _vendorLedger.GetLedgerAsync(id, from, to);
            var balance = await _vendorLedger.GetBalanceAsync(id);
            ViewBag.Vendor = vendor;
            ViewBag.Balance = balance;
            ViewBag.From = from;
            ViewBag.To = to;
            return View(entries);
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment(int vendorId, decimal amount, string? notes)
        {
            if (amount <= 0) { TempData["Error"] = "Amount must be greater than zero."; return RedirectToAction("Ledger", new { id = vendorId }); }
            await _vendorLedger.AddEntryAsync(vendorId, notes ?? "Payment to Vendor", amount, 0, "Payment", 0);
            _db.VendorPayments.Add(new VendorPayment
            {
                VendorId = vendorId,
                Amount = amount,
                Notes = notes,
                Method = PaymentMethod.Cash,
                PaidByUserId = CurrentUserId!.Value,
                Date = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Payment of Rs.{amount:N2} recorded.";
            return RedirectToAction("Ledger", new { id = vendorId });
        }
    }

    // ─────────────── STAFF ───────────────
    public class StaffController : BaseController
    {
        private readonly AppDbContext _db;
        public StaffController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index() =>
            View(await _db.Staff.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Staff staff)
        {
            _db.Staff.Add(staff);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Staff '{staff.Name}' registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id) => View(await _db.Staff.FindAsync(id));

        [HttpPost]
        public async Task<IActionResult> Edit(Staff staff)
        {
            var e = await _db.Staff.FindAsync(staff.Id);
            if (e == null) return NotFound();
            e.Name = staff.Name; e.Phone = staff.Phone; e.CNIC = staff.CNIC;
            e.Designation = staff.Designation; e.BasicSalary = staff.BasicSalary; e.Address = staff.Address;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Staff updated.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Salaries(int id)
        {
            var staff = await _db.Staff.FindAsync(id);
            ViewBag.Staff = staff;
            return View(await _db.StaffSalaries.Where(s => s.StaffId == id)
                .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PaySalary(int staffId, int month, int year, decimal allowances, decimal deductions)
        {
            if (await _db.StaffSalaries.AnyAsync(s => s.StaffId == staffId && s.Month == month && s.Year == year && s.IsPaid))
            { TempData["Error"] = "Salary already paid for this period."; return RedirectToAction("Salaries", new { id = staffId }); }
            var staff = await _db.Staff.FindAsync(staffId);
            if (staff == null) return NotFound();
            var advances = await _db.AdvanceSalaries.Where(a => a.StaffId == staffId && !a.IsRecovered).ToListAsync();
            var advDeduction = advances.Sum(a => a.Amount);
            _db.StaffSalaries.Add(new StaffSalary
            {
                StaffId = staffId,
                Month = month,
                Year = year,
                BasicSalary = staff.BasicSalary,
                Allowances = allowances,
                Deductions = deductions,
                AdvanceDeduction = advDeduction,
                NetSalary = staff.BasicSalary + allowances - deductions - advDeduction,
                IsPaid = true,
                PaidOn = DateTime.Now
            });
            advances.ForEach(a => a.IsRecovered = true);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Salary paid for {new DateTime(year, month, 1):MMMM yyyy}.";
            return RedirectToAction("Salaries", new { id = staffId });
        }

        [HttpPost]
        public async Task<IActionResult> GiveAdvance(int staffId, decimal amount, string reason)
        {
            _db.AdvanceSalaries.Add(new AdvanceSalary { StaffId = staffId, Amount = amount, Reason = reason });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Advance of Rs.{amount:N2} given.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Commissions() =>
            View(await _db.SaleCommissions.Include(c => c.Staff).Include(c => c.Sale)
                .OrderByDescending(c => c.Id).ToListAsync());

        public async Task<IActionResult> Ledger(int id, DateTime? from, DateTime? to)
        {
            var staff = await _db.Staff.FindAsync(id);
            if (staff == null) return NotFound();
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to ?? DateTime.Today;
            var sales = await _db.Sales.Include(s => s.Customer)
                .Where(s => s.StaffId == id && s.SaleDate.Date >= f && s.SaleDate.Date <= t)
                .OrderBy(s => s.SaleDate).ToListAsync();
            var commissions = await _db.SaleCommissions.Include(c => c.Sale)
                .Where(c => c.StaffId == id && c.Sale!.SaleDate.Date >= f && c.Sale.SaleDate.Date <= t)
                .ToListAsync();
            ViewBag.Staff = staff; ViewBag.From = f; ViewBag.To = t;
            ViewBag.Commissions = commissions;
            return View(sales);
        }
    }

    // ─────────────── PRODUCTS ───────────────
    public class ProductsController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly IProductService _productService;
        public ProductsController(AppDbContext db, IProductService productService) { _db = db; _productService = productService; }

        public async Task<IActionResult> Index() =>
            View(await _db.Products.Include(p => p.Category).Include(p => p.Unit).Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Units = await _db.Units.OrderBy(u => u.Name).ToListAsync();
            ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            return View(new Product { CartonQty = 1 });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            if (string.IsNullOrEmpty(product.Barcode))
                product.Barcode = _productService.GenerateBarcode();
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Product '{product.Name}' registered.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Units = await _db.Units.OrderBy(u => u.Name).ToListAsync();
            ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            return View(await _db.Products.FindAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            var e = await _db.Products.FindAsync(product.Id);
            if (e == null) return NotFound();
            e.Name = product.Name; e.Barcode = product.Barcode;
            e.CategoryId = product.CategoryId; e.UnitId = product.UnitId;
            e.CartonQty = product.CartonQty;
            e.PurchasePrice = product.PurchasePrice;
            e.SalePrice = product.SalePrice;
            e.WholesalePrice = product.WholesalePrice;
            e.CommissionPercentage = product.CommissionPercentage;
            e.ExpiryDate = product.ExpiryDate; e.BatchNo = product.BatchNo;
            e.MinStockLevel = product.MinStockLevel; e.TrackExpiry = product.TrackExpiry;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Product updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Prevent deletion if transactions exist
            if (await _db.SaleItems.AnyAsync(i => i.ProductId == id))
            { TempData["Error"] = "Cannot delete: product has sales records."; return RedirectToAction("Index"); }
            if (await _db.PurchaseItems.AnyAsync(i => i.ProductId == id))
            { TempData["Error"] = "Cannot delete: product has purchase records."; return RedirectToAction("Index"); }
            if (await _db.SaleReturnItems.AnyAsync(i => i.ProductId == id))
            { TempData["Error"] = "Cannot delete: product has return records."; return RedirectToAction("Index"); }
            var p = await _db.Products.FindAsync(id);
            if (p != null) { p.IsActive = false; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Product deleted.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Units() => View(await _db.Units.ToListAsync());
        [HttpPost]
        public async Task<IActionResult> CreateUnit(Unit unit)
        { _db.Units.Add(unit); await _db.SaveChangesAsync(); TempData["Success"] = "Unit added."; return RedirectToAction("Units"); }
        [HttpPost]
        public async Task<IActionResult> EditUnit(int id, string name, string abbreviation)
        {
            var u = await _db.Units.FindAsync(id);
            if (u != null) { u.Name = name; u.Abbreviation = abbreviation; await _db.SaveChangesAsync(); TempData["Success"] = "Unit updated."; }
            return RedirectToAction("Units");
        }

        public async Task<IActionResult> Categories() =>
            View(await _db.Categories.Include(c => c.Products).ToListAsync());
        [HttpPost]
        public async Task<IActionResult> CreateCategory(Category category)
        { _db.Categories.Add(category); await _db.SaveChangesAsync(); TempData["Success"] = "Category added."; return RedirectToAction("Categories"); }
        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, string name)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat != null) { cat.Name = name; await _db.SaveChangesAsync(); TempData["Success"] = "Category updated."; }
            return RedirectToAction("Categories");
        }

        // FIXED: Search API returns all fields the JS needs with correct property names
        [HttpGet]
        public async Task<JsonResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
            // Ensure user is logged in for API call
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return Json(new { error = "Not authenticated" });
            var products = await _db.Products.Include(p => p.Unit)
                .Where(p => p.IsActive && (p.Name.Contains(q) || (p.Barcode != null && p.Barcode.Contains(q))))
                .Take(15)
                .ToListAsync();

            var result = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Barcode,
                p.CartonQty,
                p.PurchasePrice,
                p.SalePrice,
                p.WholesalePrice,
                p.CommissionPercentage,
                p.CurrentStock,
                p.BatchNo,
                expiryDate = p.ExpiryDate?.ToString("yyyy-MM-dd"),
                unitName = p.Unit?.Abbreviation ?? "Pcs",
                cartonStock = p.CartonQty > 0 ? (int)Math.Floor(p.CurrentStock / p.CartonQty) : 0,
                looseStock = p.CartonQty > 0 ? p.CurrentStock % p.CartonQty : p.CurrentStock
            });
            return Json(result);
        }

        public async Task<JsonResult> GetByBarcode(string barcode)
        {
            var p = await _db.Products.Include(p => p.Unit)
                .FirstOrDefaultAsync(x => x.Barcode == barcode && x.IsActive);
            if (p == null) return Json(new { success = false, message = "Product not found" });
            return Json(new
            {
                success = true,
                p.Id,
                p.Name,
                p.Barcode,
                p.CartonQty,
                p.PurchasePrice,
                p.SalePrice,
                p.WholesalePrice,
                p.CommissionPercentage,
                p.CurrentStock,
                unitName = p.Unit?.Abbreviation ?? "Pcs",
                cartonStock = p.CartonQty > 0 ? (int)Math.Floor(p.CurrentStock / p.CartonQty) : 0,
                looseStock = p.CartonQty > 0 ? p.CurrentStock % p.CartonQty : p.CurrentStock
            });
        }
    }

    // ─────────────── WAREHOUSE ───────────────
    public class WarehouseController : BaseController
    {
        private readonly AppDbContext _db;
        public WarehouseController(AppDbContext db) => _db = db;
        public async Task<IActionResult> Index() => View(await _db.Warehouses.Where(w => w.IsActive).ToListAsync());
        [HttpPost]
        public async Task<IActionResult> Create(Warehouse w)
        { _db.Warehouses.Add(w); await _db.SaveChangesAsync(); TempData["Success"] = "Warehouse added."; return RedirectToAction("Index"); }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string name, string? location, string? managerName)
        {
            var wh = await _db.Warehouses.FindAsync(id);
            if (wh != null) { wh.Name = name; wh.Location = location; wh.ManagerName = managerName; await _db.SaveChangesAsync(); TempData["Success"] = "Warehouse updated."; }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Stock(int id)
        {
            var wh = await _db.Warehouses.FindAsync(id);
            var stock = await _db.WarehouseStocks.Include(s => s.Product).ThenInclude(p => p!.Unit)
                .Where(s => s.WarehouseId == id).ToListAsync();
            ViewBag.Warehouse = wh;
            return View(stock);
        }
        public async Task<IActionResult> Transfer()
        {
            ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            ViewBag.Products = await _db.Products.Include(p => p.Unit).Where(p => p.IsActive).ToListAsync();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Transfer(int fromWarehouseId, int toWarehouseId, List<int> productIds, List<decimal> quantities)
        {
            var transfer = new WarehouseTransfer { FromWarehouseId = fromWarehouseId, ToWarehouseId = toWarehouseId, TransferDate = DateTime.Now };
            for (int i = 0; i < productIds.Count; i++)
            {
                transfer.Items.Add(new WarehouseTransferItem { ProductId = productIds[i], Quantity = quantities[i] });
                var from = await _db.WarehouseStocks.FirstOrDefaultAsync(s => s.WarehouseId == fromWarehouseId && s.ProductId == productIds[i]);
                if (from != null) from.Quantity = Math.Max(0, from.Quantity - quantities[i]);
                var to = await _db.WarehouseStocks.FirstOrDefaultAsync(s => s.WarehouseId == toWarehouseId && s.ProductId == productIds[i]);
                if (to != null) to.Quantity += quantities[i];
                else _db.WarehouseStocks.Add(new WarehouseStock { WarehouseId = toWarehouseId, ProductId = productIds[i], Quantity = quantities[i] });
            }
            _db.WarehouseTransfers.Add(transfer);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Transfer completed.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Block deletion if warehouse has stock
            var hasStock = await _db.WarehouseStocks.AnyAsync(s => s.WarehouseId == id && s.Quantity > 0);
            if (hasStock)
            {
                TempData["Error"] = "Cannot delete: this warehouse still has stock. Transfer or clear stock first.";
                return RedirectToAction("Index");
            }
            // Block if warehouse has transfer history
            var usedInTransfers = await _db.WarehouseTransfers
                .AnyAsync(t => t.FromWarehouseId == id || t.ToWarehouseId == id);
            if (usedInTransfers)
            {
                TempData["Error"] = "Cannot delete: this warehouse has transfer history linked to it.";
                return RedirectToAction("Index");
            }
            var wh = await _db.Warehouses.FindAsync(id);
            if (wh != null)
            {
                wh.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Warehouse '{wh.Name}' deleted.";
            }
            return RedirectToAction("Index");
        }
    }

    // ─────────────── VEHICLES ───────────────
    public class VehiclesController : BaseController
    {
        private readonly AppDbContext _db;
        public VehiclesController(AppDbContext db) => _db = db;
        public async Task<IActionResult> Index() =>
            View(await _db.Vehicles.Include(v => v.Loads).Where(v => v.IsActive).ToListAsync());
        [HttpPost]
        public async Task<IActionResult> Create(Vehicle v)
        { _db.Vehicles.Add(v); await _db.SaveChangesAsync(); TempData["Success"] = "Vehicle registered."; return RedirectToAction("Index"); }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string vehicleNo, string? model, string? driverName, string? driverPhone)
        {
            var v = await _db.Vehicles.FindAsync(id);
            if (v != null) { v.VehicleNo = vehicleNo; v.Model = model; v.DriverName = driverName; v.DriverPhone = driverPhone; await _db.SaveChangesAsync(); TempData["Success"] = "Vehicle updated."; }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> LoadVehicle(int id)
        {
            ViewBag.Vehicle = await _db.Vehicles.FindAsync(id);
            ViewBag.Products = await _db.Products.Include(p => p.Unit)
                .Where(p => p.IsActive && p.CurrentStock > 0).ToListAsync();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> LoadVehicle(int vehicleId, List<int> productIds, List<decimal> quantities)
        {
            var load = new VehicleLoad { VehicleId = vehicleId, Status = LoadStatus.Loaded };
            for (int i = 0; i < productIds.Count; i++)
            {
                if (productIds[i] <= 0 || quantities.Count <= i || quantities[i] <= 0) continue;
                load.Items.Add(new VehicleLoadItem { ProductId = productIds[i], LoadedQty = quantities[i] });
                var p = await _db.Products.FindAsync(productIds[i]);
                if (p != null) p.CurrentStock = Math.Max(0, p.CurrentStock - quantities[i]);
            }
            if (!load.Items.Any()) { TempData["Error"] = "Select at least one product with quantity."; return RedirectToAction("LoadVehicle", new { id = vehicleId }); }
            _db.VehicleLoads.Add(load);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vehicle loaded. Stock updated.";
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> UnloadVehicle(int loadId) =>
            View(await _db.VehicleLoads.Include(l => l.Vehicle)
                .Include(l => l.Items).ThenInclude(i => i.Product).FirstOrDefaultAsync(l => l.Id == loadId));
        [HttpPost]
        public async Task<IActionResult> UnloadVehicle(int loadId, List<int> itemIds, List<decimal> unloadedQtys)
        {
            var load = await _db.VehicleLoads.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == loadId);
            if (load == null) return NotFound();
            for (int i = 0; i < itemIds.Count; i++)
            {
                var item = load.Items.FirstOrDefault(x => x.Id == itemIds[i]);
                if (item == null) continue;
                item.UnloadedQty = unloadedQtys[i];
                var p = await _db.Products.FindAsync(item.ProductId);
                if (p != null) p.CurrentStock += unloadedQtys[i];
            }
            load.Status = LoadStatus.Unloaded;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Vehicle unloaded.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Block deletion if vehicle has an active load
            var hasActiveLoad = await _db.VehicleLoads.AnyAsync(l => l.VehicleId == id && l.Status == LoadStatus.Loaded);
            if (hasActiveLoad)
            {
                TempData["Error"] = "Cannot delete: this vehicle has an active load. Unload first.";
                return RedirectToAction("Index");
            }
            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                vehicle.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Vehicle '{vehicle.VehicleNo}' deleted.";
            }
            return RedirectToAction("Index");
        }
    }

    // ─────────────── ROUTES ───────────────
    public class RoutesController : BaseController
    {
        private readonly AppDbContext _db;
        public RoutesController(AppDbContext db) => _db = db;
        public async Task<IActionResult> Index() =>
            View(await _db.SaleRoutes.Include(r => r.Customers).Where(r => r.IsActive).ToListAsync());
        [HttpPost]
        public async Task<IActionResult> Create(SaleRoute route)
        { _db.SaleRoutes.Add(route); await _db.SaveChangesAsync(); TempData["Success"] = "Route added."; return RedirectToAction("Index"); }
        public async Task<IActionResult> Edit(int id) => View(await _db.SaleRoutes.FindAsync(id));
        [HttpPost]
        public async Task<IActionResult> Edit(SaleRoute route)
        {
            var e = await _db.SaleRoutes.FindAsync(route.Id);
            if (e == null) return NotFound();
            e.Name = route.Name; e.Area = route.Area; e.Description = route.Description;
            await _db.SaveChangesAsync(); TempData["Success"] = "Route updated."; return RedirectToAction("Index");
        }
    }

    // ─────────────── PURCHASES ───────────────
    public class PurchasesController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly IPurchaseService _purchaseService;
        private readonly IVendorLedgerService _vendorLedger;
        public PurchasesController(AppDbContext db, IPurchaseService ps, IVendorLedgerService vl)
        { _db = db; _purchaseService = ps; _vendorLedger = vl; }

        public async Task<IActionResult> Index() =>
            View(await _db.Purchases.Include(p => p.Vendor)
                .OrderByDescending(p => p.PurchaseDate).Take(200).ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewBag.Vendors = await _db.Vendors.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync();
            ViewBag.Warehouses = await _db.Warehouses.Where(w => w.IsActive).ToListAsync();
            ViewBag.Products = await _db.Products.Include(p => p.Unit).Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            return View(new Purchase());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Purchase purchase,
            List<int> productIds, List<decimal> cartonQtys, List<decimal> looseQtys,
            List<decimal> prices, List<string?> batchNos, List<string?> expiryDates)
        {
            purchase.CreatedByUserId = CurrentUserId!.Value;
            for (int i = 0; i < productIds.Count; i++)
            {
                if (productIds[i] <= 0) continue;
                var product = await _db.Products.FindAsync(productIds[i]);
                if (product == null) continue;
                var cartons = cartonQtys.Count > i ? cartonQtys[i] : 0;
                var loose = looseQtys.Count > i ? looseQtys[i] : 0;
                if (cartons <= 0 && loose <= 0) continue; // skip zero-quantity rows
                var item = new PurchaseItem
                {
                    ProductId = productIds[i],
                    CartonQty = cartons,
                    LooseQty = loose,
                    PurchasePrice = prices.Count > i ? prices[i] : product.PurchasePrice,
                    BatchNo = batchNos.Count > i ? batchNos[i] : null,
                    ExpiryDate = expiryDates.Count > i && DateTime.TryParse(expiryDates[i], out var ed) ? ed : null,
                    Product = product
                };
                purchase.Items.Add(item);
            }
            if (!purchase.Items.Any()) { TempData["Error"] = "Please select products and enter quantities before saving."; return RedirectToAction("Create"); }
            purchase.SubTotal = purchase.Items.Sum(i => i.TotalAmount);
            purchase.TotalAmount = purchase.SubTotal + purchase.Tax - purchase.Discount;

            await _purchaseService.CreatePurchaseAsync(purchase);

            // Vendor ledger
            await _vendorLedger.AddEntryAsync(purchase.VendorId, $"Purchase — {purchase.InvoiceNo}", 0, purchase.TotalAmount, "Purchase", purchase.Id);
            if (purchase.PaidAmount > 0)
                await _vendorLedger.AddEntryAsync(purchase.VendorId, $"Payment — {purchase.InvoiceNo}", purchase.PaidAmount, 0, "Payment", purchase.Id);

            TempData["Success"] = $"Purchase {purchase.InvoiceNo} recorded.";
            return RedirectToAction("Details", new { id = purchase.Id });
        }

        public async Task<IActionResult> Details(int id) =>
            View(await _db.Purchases.Include(p => p.Vendor)
                .Include(p => p.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Unit)
                .FirstOrDefaultAsync(p => p.Id == id));

        public async Task<IActionResult> Returns() =>
            View(await _db.PurchaseReturns
                .Include(r => r.Purchase).ThenInclude(p => p!.Vendor)
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .OrderByDescending(r => r.ReturnDate).ToListAsync());

        public async Task<IActionResult> Return(int purchaseId)
        {
            ViewBag.Purchase = await _db.Purchases.Include(p => p.Vendor)
                .Include(p => p.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == purchaseId);
            return View(new PurchaseReturn { PurchaseId = purchaseId });
        }

        [HttpPost]
        public async Task<IActionResult> Return(PurchaseReturn ret, List<int> productIds, List<decimal> quantities, List<decimal> prices)
        {
            for (int i = 0; i < productIds.Count; i++)
                if (quantities[i] > 0)
                    ret.Items.Add(new PurchaseReturnItem { ProductId = productIds[i], Quantity = quantities[i], Price = prices[i] });
            if (!ret.Items.Any()) { TempData["Error"] = "Enter at least one return quantity."; return RedirectToAction("Return", new { purchaseId = ret.PurchaseId }); }
            ret.TotalAmount = ret.Items.Sum(i => i.Amount);
            await _purchaseService.CreatePurchaseReturnAsync(ret);

            var purchase = await _db.Purchases.FindAsync(ret.PurchaseId);
            if (purchase != null)
                await _vendorLedger.AddEntryAsync(purchase.VendorId, $"Purchase Return — {purchase.InvoiceNo}", ret.TotalAmount, 0, "PurchaseReturn", ret.Id);

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

        public async Task<IActionResult> Index() =>
            View(await _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .OrderByDescending(s => s.SaleDate).Take(200).ToListAsync());

        public async Task<IActionResult> NewSale()
        {
            ViewBag.Customers = await _db.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Staff = await _db.Staff.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            ViewBag.Products = await _db.Products.Include(p => p.Unit).Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            return View(new Sale());
        }

        [HttpPost]
        public async Task<IActionResult> NewSale(Sale sale,
            List<int> productIds, List<decimal> cartonQtys, List<decimal> looseQtys,
            List<decimal> prices, List<decimal> discounts, List<string?> batchNos, List<decimal> purchaseRates)
        {
            sale.CreatedByUserId = CurrentUserId!.Value;
            sale.SaleDate = DateTime.Now;
            sale.IsCredit = sale.SaleType == SaleType.Credit;
            if (sale.IsCredit && !sale.CustomerId.HasValue)
            { TempData["Error"] = "Credit sales require a registered customer."; return RedirectToAction("NewSale"); }

            for (int i = 0; i < productIds.Count; i++)
            {
                var product = await _db.Products.FindAsync(productIds[i]);
                var item = new SaleItem
                {
                    ProductId = productIds[i],
                    CartonQty = cartonQtys.Count > i ? cartonQtys[i] : 0,
                    LooseQty = looseQtys.Count > i ? looseQtys[i] : 0,
                    SalePrice = prices.Count > i ? prices[i] : 0,
                    Discount = discounts.Count > i ? discounts[i] : 0,
                    BatchNo = batchNos.Count > i ? batchNos[i] : null,
                    PurchaseRate = purchaseRates.Count > i ? purchaseRates[i] : (product?.PurchasePrice ?? 0),
                    Product = product
                };
                sale.Items.Add(item);
            }
            if (!sale.Items.Any()) { TempData["Error"] = "Cart is empty."; return RedirectToAction("NewSale"); }
            sale.SubTotal = sale.Items.Sum(i => i.TotalAmount);
            sale.TotalAmount = sale.SubTotal + sale.Tax - sale.Discount;

            var result = await _saleService.CreateSaleAsync(sale);
            TempData["Success"] = $"Sale {result.InvoiceNo} completed!";
            return RedirectToAction("Receipt", new { id = result.Id });
        }

        public async Task<IActionResult> Receipt(int id)
        {
            ViewBag.Settings = await _db.CompanySettings.FirstOrDefaultAsync();
            return View(await _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .Include(s => s.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Unit)
                .FirstOrDefaultAsync(s => s.Id == id));
        }

        public async Task<IActionResult> Details(int id) =>
            View(await _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id));

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
                if (quantities[i] > 0)
                    ret.Items.Add(new SaleReturnItem { ProductId = productIds[i], Quantity = quantities[i], Price = prices[i] });
            ret.TotalAmount = ret.Items.Sum(i => i.Amount);
            await _saleService.CreateSaleReturnAsync(ret);
            TempData["Success"] = "Sale return recorded.";
            return RedirectToAction("Index");
        }
    }

    // ─────────────── SALE RETURNS ───────────────
    public class SaleReturnsController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly ISaleService _saleService;
        public SaleReturnsController(AppDbContext db, ISaleService ss) { _db = db; _saleService = ss; }

        public async Task<IActionResult> Index() =>
            View(await _db.SaleReturns
                .Include(r => r.Sale).ThenInclude(s => s!.Customer)
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .OrderByDescending(r => r.ReturnDate).ToListAsync());

        public async Task<IActionResult> Create() =>
            View(await _db.Sales.Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate).Take(60).ToListAsync());

        public async Task<IActionResult> FromSale(int saleId)
        {
            var sale = await _db.Sales.Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Unit)
                .FirstOrDefaultAsync(s => s.Id == saleId);
            if (sale == null) { TempData["Error"] = "Sale not found."; return RedirectToAction("Create"); }

            // Already returned quantities per product
            var alreadyReturned = await _db.SaleReturnItems
                .Where(ri => ri.Return!.SaleId == saleId)
                .GroupBy(ri => ri.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(ri => ri.Quantity) })
                .ToListAsync();
            ViewBag.Sale = sale;
            ViewBag.AlreadyReturned = alreadyReturned.ToDictionary(r => r.ProductId, r => r.Qty);
            return View(new SaleReturn { SaleId = saleId });
        }

        [HttpPost]
        public async Task<IActionResult> FromSale(SaleReturn ret,
            List<int> productIds, List<decimal> quantities, List<decimal> prices)
        {
            for (int i = 0; i < productIds.Count; i++)
                if (quantities[i] > 0)
                    ret.Items.Add(new SaleReturnItem { ProductId = productIds[i], Quantity = quantities[i], Price = prices[i] });

            if (!ret.Items.Any()) { TempData["Error"] = "Enter at least one return quantity."; return RedirectToAction("FromSale", new { saleId = ret.SaleId }); }

            ret.TotalAmount = ret.Items.Sum(i => i.Quantity * i.Price);
            await _saleService.CreateSaleReturnAsync(ret);

            // Reverse credit ledger
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

    // ─────────────── REPORTS ───────────────
    public class ReportsController : BaseController
    {
        private readonly IReportService _reports;
        private readonly AppDbContext _db;
        public ReportsController(IReportService reports, AppDbContext db) { _reports = reports; _db = db; }
        public IActionResult Index() => View();
        public async Task<IActionResult> ProfitLoss(DateTime? from, DateTime? to)
            => View(await _reports.GetProfitLossAsync(from ?? DateTime.Today.AddMonths(-1), to ?? DateTime.Today));
        public async Task<IActionResult> TrialBalance(DateTime? asOf)
            => View(await _reports.GetTrialBalanceAsync(asOf ?? DateTime.Today));
        public async Task<IActionResult> SalesReport(DateTime? from, DateTime? to, int? customerId, int? staffId)
        {
            var f = from ?? DateTime.Today; var t = to ?? DateTime.Today;
            var q = _db.Sales.Include(s => s.Customer).Include(s => s.Staff)
                .Where(s => s.SaleDate.Date >= f && s.SaleDate.Date <= t);
            if (customerId.HasValue) q = q.Where(s => s.CustomerId == customerId);
            if (staffId.HasValue) q = q.Where(s => s.StaffId == staffId);
            ViewBag.Customers = await _db.Customers.Where(c => c.IsActive).ToListAsync();
            ViewBag.Staff = await _db.Staff.Where(s => s.IsActive).ToListAsync();
            ViewBag.From = f; ViewBag.To = t;
            return View(await q.OrderByDescending(s => s.SaleDate).ToListAsync());
        }
        public async Task<IActionResult> StockReport()
            => View(await _db.Products.Include(p => p.Category).Include(p => p.Unit)
                .Where(p => p.IsActive).OrderBy(p => p.Category!.Name).ThenBy(p => p.Name).ToListAsync());
        public async Task<IActionResult> ExpiryReport()
            => View(await _db.Products.Where(p => p.IsActive && p.ExpiryDate.HasValue).OrderBy(p => p.ExpiryDate).ToListAsync());
        public async Task<IActionResult> CommissionReport(int? staffId, DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.Today.AddMonths(-1); var t = to ?? DateTime.Today;
            var q = _db.SaleCommissions.Include(c => c.Staff).Include(c => c.Sale)
                .Where(c => c.Sale!.SaleDate >= f && c.Sale.SaleDate <= t);
            if (staffId.HasValue) q = q.Where(c => c.StaffId == staffId);
            ViewBag.Staff = await _db.Staff.Where(s => s.IsActive).ToListAsync();
            ViewBag.From = f; ViewBag.To = t;
            return View(await q.ToListAsync());
        }
    }

    // ─────────────── CAPITAL ───────────────
    public class CapitalController : BaseController
    {
        private readonly AppDbContext _db;
        public CapitalController(AppDbContext db) => _db = db;
        public async Task<IActionResult> Index()
        {
            var caps = await _db.InitialCapitals.OrderByDescending(c => c.Date).ToListAsync();
            ViewBag.Total = caps.Sum(c => c.Amount);
            return View(caps);
        }
        [HttpPost]
        public async Task<IActionResult> Add(InitialCapital capital)
        { _db.InitialCapitals.Add(capital); await _db.SaveChangesAsync(); TempData["Success"] = "Capital entry added."; return RedirectToAction("Index"); }
    }

    // ─────────────── EXPENSES ───────────────
    public class ExpensesController : BaseController
    {
        private readonly AppDbContext _db;
        public ExpensesController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? categoryId)
        {
            var f = from ?? DateTime.Today.AddMonths(-1);
            var t = to ?? DateTime.Today;
            var all = await _db.Expenses.OrderByDescending(e => e.Date).ToListAsync();
            var cats = await _db.ExpenseCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var list = all.Where(e => e.Date.Date >= f && e.Date.Date <= t);
            if (categoryId.HasValue)
            {
                var catName = cats.FirstOrDefault(c => c.Id == categoryId)?.Name;
                if (catName != null) list = list.Where(e => e.Category == catName);
            }
            var result = list.ToList();
            ViewBag.From = f;
            ViewBag.To = t;
            ViewBag.CategoryId = categoryId;
            ViewBag.ExpenseCategories = cats;
            ViewBag.Total = result.Sum(e => e.Amount);
            return View(result);
        }

        // ── Expense Category CRUD ──
        public async Task<IActionResult> Categories() =>
            View(await _db.ExpenseCategories.OrderBy(c => c.Name).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> CreateCategory(ExpenseCategory cat)
        {
            if (await _db.ExpenseCategories.AnyAsync(c => c.Name == cat.Name))
            { TempData["Error"] = $"Category '{cat.Name}' already exists."; return RedirectToAction("Categories"); }
            _db.ExpenseCategories.Add(cat);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Category '{cat.Name}' added.";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, string name, string? description)
        {
            var cat = await _db.ExpenseCategories.FindAsync(id);
            if (cat != null) { cat.Name = name; cat.Description = description; await _db.SaveChangesAsync(); TempData["Success"] = "Category updated."; }
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var inUse = await _db.Expenses.AnyAsync(e => e.Category == _db.ExpenseCategories.Where(c => c.Id == id).Select(c => c.Name).FirstOrDefault());
            if (inUse) { TempData["Error"] = "Cannot delete: category is used in expense records."; return RedirectToAction("Categories"); }
            var cat = await _db.ExpenseCategories.FindAsync(id);
            if (cat != null) { cat.IsActive = false; await _db.SaveChangesAsync(); TempData["Success"] = "Category deleted."; }
            return RedirectToAction("Categories");
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

        public async Task<IActionResult> Add()
        {
            ViewBag.ExpenseCategories = await _db.ExpenseCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.Expenses.FindAsync(id);
            if (e != null) { _db.Expenses.Remove(e); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Expense deleted.";
            return RedirectToAction("Index");
        }
    }
}

namespace INSolPOS.Controllers
{
    // ─────────────── SETTINGS ───────────────
    public class SettingsController : BaseController
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        public SettingsController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

        public async Task<IActionResult> Index()
        {
            var settings = await _db.CompanySettings.FirstOrDefaultAsync()
                           ?? new CompanySettings();
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Save(CompanySettings model, IFormFile? logoFile)
        {
            var existing = await _db.CompanySettings.FirstOrDefaultAsync();
            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsDir);
                var fileName = "logo" + Path.GetExtension(logoFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await logoFile.CopyToAsync(stream);
                model.LogoPath = "/uploads/" + fileName;
            }
            else { model.LogoPath = existing?.LogoPath; }

            if (existing == null) { _db.CompanySettings.Add(model); }
            else
            {
                existing.BusinessName = model.BusinessName;
                existing.Tagline = model.Tagline;
                existing.Address = model.Address;
                existing.Phone = model.Phone;
                existing.Email = model.Email;
                existing.Website = model.Website;
                existing.LogoPath = model.LogoPath;
                existing.Currency = model.Currency;
                existing.InvoiceFooter = model.InvoiceFooter;
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Settings saved.";
            return RedirectToAction("Index");
        }
    }
}
