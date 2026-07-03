using Microsoft.AspNetCore.Mvc;
using INSolPOS.Data;
using INSolPOS.Helpers;
using INSolPOS.Models;
using INSolPOS.Services;
using Microsoft.EntityFrameworkCore;

namespace INSolPOS.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IAuthService _auth;
        public AccountController(IAuthService auth) => _auth = auth;

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Dashboard", "Home");
            return View();
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _auth.LoginAsync(username, password);
            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }
            SessionHelper.SetUser(HttpContext.Session, user);
            return RedirectToAction("Dashboard", "Home");
        }

        public IActionResult Logout()
        {
            SessionHelper.ClearSession(HttpContext.Session);
            return RedirectToAction("Login");
        }

        public IActionResult ChangePassword() => View();

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                return View();
            }
            var result = await _auth.ChangePasswordAsync(CurrentUserId!.Value, oldPassword, newPassword);
            TempData[result ? "Success" : "Error"] = result ? "Password changed successfully." : "Old password is incorrect.";
            return RedirectToAction("ChangePassword");
        }
    }

    public class HomeController : BaseController
    {
        private readonly IReportService _reports;
        private readonly IProductService _products;
        private readonly AppDbContext _db;

        public HomeController(IReportService reports, IProductService products, AppDbContext db)
        {
            _reports = reports;
            _products = products;
            _db = db;
        }

        public async Task<IActionResult> Dashboard()
        {
            var stats = await _reports.GetDashboardStatsAsync();
            var lowStock = await _products.GetLowStockProductsAsync();
            var expiring = await _products.GetExpiringProductsAsync(30);

            // Sales chart data for last 7 days
            var salesData = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var daySales = await _db.Sales.Where(s => s.SaleDate.Date == date).SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
                salesData.Add(new { date = date.ToString("dd MMM"), amount = daySales });
            }

            ViewBag.Stats = stats;
            ViewBag.LowStock = lowStock;
            ViewBag.Expiring = expiring;
            ViewBag.SalesChart = salesData;
            return View();
        }
    }
}
