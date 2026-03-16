using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class OrderController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public OrderController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ================= ORDER LIST =================
        public IActionResult Index()
        {
            return View();
        }

        // ================= GET ALL ORDERS (API) =================
        [HttpGet]
        public IActionResult GetAll(string status = "all")
        {
            var orders = _unitOfWork.OrderHeader.GetAll().AsQueryable();

            orders = status switch
            {
                "pending" => orders.Where(o => o.OrderStatus == SD.OrderStatusPending),
                "approved" => orders.Where(o => o.OrderStatus == SD.OrderStatusApproved),
                "inprocess" => orders.Where(o => o.OrderStatus == SD.OrderStatusInProcess),
                "shipped" => orders.Where(o => o.OrderStatus == SD.OrderStatusShipped),
                "delivered" => orders.Where(o => o.OrderStatus == "Delivered"),
                "cancelled" => orders.Where(o => o.OrderStatus == SD.OrderStatusCancelled),
                _ => orders
            };

            var data = orders.OrderByDescending(o => o.OrderDate).Select(o => new {
                o.Id,
                o.Name,
                o.PhoneNumber,
                o.OrderTotal,
                o.OrderStatus,
                o.PaymentStatus,
                OrderDate = o.OrderDate.ToString("dd MMM yyyy")
            }).ToList();

            return Json(new { data });
        }

        // ================= ORDER DETAILS (View) =================
        public IActionResult Details(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.FirstOrDefault(o => o.Id == id);
            if (orderHeader == null) return NotFound();

            var orderDetails = _unitOfWork.OrderDetails.GetAll(
                d => d.OrderHeaderId == id,
                IncludeProperties: "Product"
            ).ToList();

            ViewBag.OrderDetails = orderDetails;
            return View(orderHeader);
        }

        // ================= GET ORDER DETAILS (JSON for modal) =================
        [HttpGet]
        public IActionResult GetOrderDetails(int id)
        {
            var order = _unitOfWork.OrderHeader.FirstOrDefault(o => o.Id == id);
            if (order == null) return Json(new { success = false });

            var items = _unitOfWork.OrderDetails.GetAll(
                d => d.OrderHeaderId == id,
                IncludeProperties: "Product"
            ).Select(d => new {
                d.ProductId,
                Name = d.Product?.Name ?? "Unknown",
                ImageUrl = d.Product?.ImageUrl ?? "",
                d.Count,
                d.Price
            }).ToList();

            return Json(new
            {
                order.Id,
                order.Name,
                order.PhoneNumber,
                order.OrderDate,
                order.OrderTotal,
                order.OrderStatus,
                order.PaymentStatus,
                order.StreetAddress,
                order.City,
                order.State,
                order.PostalCode,
                order.Carrier,
                order.TrackingNumber,
                Items = items
            });
        }

        // ================= UPDATE STATUS =================
        [HttpPost]
        public IActionResult UpdateStatus(int orderId, string status)
        {
            var order = _unitOfWork.OrderHeader.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            // ✅ Cancelled order — lock
            if (order.OrderStatus == SD.OrderStatusCancelled)
                return Json(new { success = false, message = "Cancelled order ka status change nahi ho sakta ❌" });

            // ✅ Delivered order — lock
            if (order.OrderStatus == "Delivered")
                return Json(new { success = false, message = "Delivered order ka status change nahi ho sakta ❌" });

            order.OrderStatus = status;

            if (status == SD.OrderStatusShipped)
                order.ShippingDate = DateTime.UtcNow;

            if (status == "Delivered")
                order.ShippingDate = DateTime.UtcNow; // delivery date as ShippingDate

            _unitOfWork.Save();

            return Json(new { success = true, message = "Status updated to " + status });
        }

        // ================= UPDATE TRACKING =================
        [HttpPost]
        public IActionResult UpdateTracking(int orderId, string carrier, string trackingNumber)
        {
            var order = _unitOfWork.OrderHeader.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            order.Carrier = carrier;
            order.TrackingNumber = trackingNumber;
            _unitOfWork.Save();

            return Json(new { success = true, message = "Tracking info updated" });
        }
    }
}