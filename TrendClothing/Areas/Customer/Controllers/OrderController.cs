using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public OrderController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ================= ORDER HISTORY =================
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = _unitOfWork.OrderHeader.GetAll(
                o => o.ApplicationuserId == userId
            ).OrderByDescending(o => o.OrderDate);

            return View(orders);
        }

        // ================= ORDER DETAILS =================
        public IActionResult Details(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.FirstOrDefault(o => o.Id == id);
            if (orderHeader == null) return NotFound();

            var orderDetails = _unitOfWork.OrderDetails.GetAll(
                d => d.OrderHeaderId == id,
                IncludeProperties: "Product"
            );

            ViewBag.OrderHeader = orderHeader;
            return View(orderDetails);
        }
        // ================= ORDER SUCCESS =================
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

    }
}
