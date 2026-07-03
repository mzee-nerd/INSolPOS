# 🏪 INSol POS — Advanced Point of Sale System

A full-featured, enterprise-grade Point of Sale system built with **ASP.NET Core 8 MVC** + **Entity Framework Core** + **SQL Server**. Designed for distributors, wholesalers, and retail businesses in Pakistan.

---

## ✨ Features

### 👥 User System (4 Roles)
| Role | Access Level |
|------|-------------|
| **Super Admin** | Full system access, user management |
| **Admin** | All transactions, reports, settings |
| **Teller** | Sales, purchases, customer payments |
| **Salesman** | New sale, view own commissions |

### 📋 Core Modules

| Module | Features |
|--------|----------|
| **Customers** | Registration, credit limit, ledger, payment receipt |
| **Vendors** | Registration, purchase tracking |
| **Staff** | Registration, salary, advance salary, commission tracking |
| **Products** | Barcode, expiry, batch no, carton/loose qty, commission % |
| **Categories & Units** | Customizable product classifications |
| **Warehouses** | Multiple warehouses, stock transfer |
| **Vehicles** | Registration, load/unload, sold qty tracking |
| **Sale Routes** | Area/route management for distributors |

### 💳 Transactions

| Transaction | Features |
|-------------|----------|
| **Sale (POS)** | Barcode scan, cart, cash/credit/wholesale, receipt print |
| **Sale Return** | Per-item quantity return, stock restoration |
| **Purchase** | Multi-item, carton+loose qty, batch/expiry, vendor |
| **Purchase Return** | Per-item return, stock deduction |
| **Credit Sale** | Registered customers only, automatic ledger entry |
| **Customer Payment** | Payment receipt, running ledger balance |

### 📊 Reports

| Report | Description |
|--------|-------------|
| **Profit & Loss** | Revenue, COGS, gross/net profit any period |
| **Trial Balance** | Double-entry style as of any date |
| **Sales Report** | Filter by date, customer, salesman |
| **Stock Report** | Current inventory with values |
| **Expiry Report** | Expired/near-expiry products |
| **Commission Report** | Per salesman, paid/pending breakdown |

---

## 🚀 Getting Started

### Prerequisites
- **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (LocalDB, Express, or full) — [Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **Visual Studio 2022** or **VS Code** with C# extension

### Installation

```bash
# 1. Clone / extract the project
cd INSolPOS

# 2. Restore NuGet packages
dotnet restore

# 3. Update database connection string in appsettings.json
#    Default: (localdb)\mssqllocaldb — works with Visual Studio

# 4. Apply database migrations
dotnet ef database update
# OR: The app runs EnsureCreated() on startup automatically

# 5. Run the application
dotnet run
# OR: Press F5 in Visual Studio
```

### First Login
| Field | Value |
|-------|-------|
| **URL** | `https://localhost:5001` |
| **Username** | `superadmin` |
| **Password** | `Admin@123` |

> ⚠️ **Change the default password immediately after first login!**

---

## 🗄️ Database Configuration

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=INSolPOS;Trusted_Connection=True;"
  }
}
```

**For SQL Server Express:**
```
Server=.\\SQLEXPRESS;Database=INSolPOS;Trusted_Connection=True;
```

**For SQL Server with credentials:**
```
Server=YOUR_SERVER;Database=INSolPOS;User Id=sa;Password=YourPassword;
```

---

## 🏗️ Project Structure

```
INSolPOS/
├── Controllers/
│   ├── AccountController.cs      # Login, logout, change password
│   ├── HomeController.cs         # Dashboard
│   └── Controllers.cs            # All feature controllers
├── Models/
│   └── DomainModels.cs           # All entity models
├── Data/
│   └── AppDbContext.cs           # EF Core DbContext + seed data
├── Services/
│   └── Services.cs               # Business logic layer
├── Helpers/
│   └── SessionHelper.cs          # Session management + BaseController
├── Views/
│   ├── Shared/_Layout.cshtml     # Master layout with sidebar
│   ├── Account/                  # Login, ChangePassword
│   ├── Home/                     # Dashboard
│   ├── Sales/                    # NewSale (POS), Index, Receipt, Return
│   ├── Purchases/                # Create, Index, Details, Return
│   ├── Products/                 # Index, Create, Edit, Units, Categories
│   ├── Customers/                # Index, Create, Edit, Ledger
│   ├── Vendors/                  # Index, Edit
│   ├── Staff/                    # Index, Edit, Salaries, Commissions
│   ├── Warehouse/                # Index, Stock, Transfer
│   ├── Vehicles/                 # Index, LoadVehicle, UnloadVehicle
│   ├── Routes/                   # Index, Edit
│   ├── Capital/                  # Index (capital management)
│   └── Reports/                  # ProfitLoss, TrialBalance, SalesReport, etc.
├── appsettings.json
├── Program.cs
├── setup.sql                     # Optional SQL views/seed data
└── INSolPOS.csproj
```

---

## 💡 Key Business Logic

### Stock Management
- Stock increases on **Purchase** and **Sale Return** / **Vehicle Unload**
- Stock decreases on **Sale** and **Vehicle Load** / **Purchase Return**
- Low-stock alerts visible on Dashboard and Stock Report

### Credit Sales & Ledger
- Credit sales only available for **registered customers**
- Every credit sale auto-creates a **Customer Ledger** debit entry
- Payments create **credit entries** with running balance
- Full ledger with debit/credit/balance visible per customer

### Commission System
- Each product has a **commission %** field
- On sale, commission auto-calculated per item
- Commission records linked to sale + salesman
- Advance salary deducted automatically during salary payment

### Vehicle Loading
- Load products onto a vehicle (deducts from main stock)
- Unload returns unsold stock back to inventory
- Sold qty = Loaded qty − Returned qty

### Profit & Loss Calculation
```
Net Sales = Total Sales − Sale Returns
Net Purchases = Total Purchases − Purchase Returns  
Gross Profit = Net Sales − Net Purchases
Net Profit = Gross Profit − (Salaries + Commissions + Other Expenses)
```

---

## 🖨️ Printing

All major views support browser printing:
- **Sale Receipt** — thermal-friendly 80mm width
- **Purchase Order** — A4 format
- **Reports** — P&L, Trial Balance, Stock Report, etc.

Use `Ctrl+P` or the Print button on each page.

---

## 🔒 Security Notes

1. Passwords are hashed using a salt — **replace with BCrypt** for production
2. Session-based authentication — configure session timeout in `Program.cs`
3. Role-based access enforced at controller level via `BaseController`
4. Add HTTPS redirection for production deployment

---

## 🛠️ Extending the System

### Add a New Report
1. Add method to `IReportService` / `ReportService`
2. Add action to `ReportsController`
3. Create view in `Views/Reports/`
4. Add link in `_Layout.cshtml` sidebar

### Add a New Module
1. Create model in `DomainModels.cs`
2. Add `DbSet<>` to `AppDbContext`
3. Create controller in `Controllers.cs`
4. Create views
5. Run `dotnet ef migrations add NewModule && dotnet ef database update`

---

## 📞 Support

**Default Credentials:** `superadmin` / `Admin@123`  
**Currency:** Pakistani Rupee (Rs.) — configurable in `appsettings.json`  
**Version:** 1.0.0  

---

*Built with ❤️ using ASP.NET Core 8, Entity Framework Core, and SQL Server*
