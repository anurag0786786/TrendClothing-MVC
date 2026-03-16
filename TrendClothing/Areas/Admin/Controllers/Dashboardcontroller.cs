using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class DashboardController : Controller
    {
        private readonly IUnitofWork _unitOfWork;
        private readonly ApplicationDbContext _db;

        // NOTE: _db is kept ONLY for Identity-related queries (Roles, UserRoles)
        // which are not exposed through UnitOfWork. Everything else goes via UnitOfWork.
        public DashboardController(IUnitofWork unitOfWork, ApplicationDbContext db)
        {
            _unitOfWork = unitOfWork;
            _db = db;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetStats()
        {
            var allOrders = _unitOfWork.OrderHeader.GetAll().ToList();

            var activeOrders = allOrders.Where(o =>
                o.OrderStatus != SD.OrderStatusCancelled).ToList();

            // ✅ Revenue — only paid/approved orders
            var totalRevenue = activeOrders
                .Where(o => o.PaymentStatus == SD.PaymentStatusApproved)
                .Sum(o => o.OrderTotal);

            var totalOrders = activeOrders.Count;
            var approvedOrders = activeOrders.Count(o => o.OrderStatus == SD.OrderStatusApproved);
            var pendingOrders = activeOrders.Count(o => o.OrderStatus == SD.OrderStatusPending);
            var shippedOrders = activeOrders.Count(o => o.OrderStatus == SD.OrderStatusShipped);
            var processingOrders = activeOrders.Count(o => o.OrderStatus == SD.OrderStatusInProcess);
            var deliveredOrders = activeOrders.Count(o => o.OrderStatus == SD.OrderStatusDelivered);
            var cancelledOrders = allOrders.Count(o => o.OrderStatus == SD.OrderStatusCancelled);

            // ✅ Monthly revenue for chart (last 6 months)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlyRevenue = activeOrders
                .Where(o => o.PaymentStatus == SD.PaymentStatusApproved
                         && o.OrderDate >= sixMonthsAgo)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    revenue = g.Sum(o => o.OrderTotal),
                    orders = g.Count()
                })
                .OrderBy(x => x.month)
                .ToList();

            // ✅ Customer count via Identity (DbContext justified here)
            // ✅ FIX: Use SD.Role_Individual (not Role_Idividual typo)
            var customerRoleId = _db.Roles
                .Where(r => r.Name == SD.Role_Individual)
                .Select(r => r.Id)
                .FirstOrDefault();

            var totalCustomers = customerRoleId != null
                ? _db.UserRoles.Count(ur => ur.RoleId == customerRoleId)
                : _db.ApplicationUsers.Count();

            // Recent 10 orders
            var recentOrders = allOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new
                {
                    o.Id,
                    o.Name,
                    OrderDate = o.OrderDate.ToString("dd MMM yyyy"),
                    o.OrderTotal,
                    o.OrderStatus,
                    o.PaymentStatus
                }).ToList();

            // Top 5 products by sales
            var cancelledIds = allOrders
                .Where(o => o.OrderStatus == SD.OrderStatusCancelled)
                .Select(o => o.Id).ToHashSet();

            var orderDetails = _unitOfWork.OrderDetails
                .GetAll(IncludeProperties: "Product").ToList();

            var topProducts = orderDetails
                .Where(d => !cancelledIds.Contains(d.OrderHeaderId))
                .GroupBy(d => d.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Name = g.First().Product?.Name ?? "Unknown",
                    ImageUrl = g.First().Product?.ImageUrl ?? "",
                    UnitsSold = g.Sum(d => d.Count),
                    Revenue = g.Sum(d => d.Price * d.Count)
                })
                .OrderByDescending(p => p.UnitsSold)
                .Take(5).ToList();

            return Json(new
            {
                totalRevenue,
                totalOrders,
                approvedOrders,
                pendingOrders,
                shippedOrders,
                processingOrders,
                deliveredOrders,
                cancelledOrders,
                totalCustomers,
                monthlyRevenue,
                recentOrders,
                topProducts
            });
        }
    }
}