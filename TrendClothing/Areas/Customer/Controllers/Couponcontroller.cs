// Areas/Customer/Controllers/CouponController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CouponController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public CouponController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── VALIDATE COUPON (AJAX) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Validate(string code, double orderTotal)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Coupon code enter karo" });

            var coupon = _unitOfWork.Coupon.FirstOrDefault(
                c => c.Code.ToUpper() == code.Trim().ToUpper() && c.IsActive);

            if (coupon == null)
                return Json(new { success = false, message = "Invalid coupon code ❌" });

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate < DateTime.UtcNow)
                return Json(new { success = false, message = "Coupon expired hai ❌" });

            if (orderTotal < coupon.MinOrderAmount)
                return Json(new { success = false, message = $"Min order ₹{coupon.MinOrderAmount:N0} chahiye ❌" });

            // Calculate discount
            double discount = coupon.DiscountType == "Percent"
                ? Math.Round(orderTotal * coupon.DiscountValue / 100, 2)
                : coupon.DiscountValue;

            // Discount order total se zyada nahi ho sakta
            discount = Math.Min(discount, orderTotal);
            double finalTotal = orderTotal - discount;

            // Session mein save karo
            HttpContext.Session.SetString("CouponCode", coupon.Code);
            HttpContext.Session.SetString("CouponDiscount", discount.ToString());

            return Json(new
            {
                success = true,
                message = $"'{coupon.Code}' applied — ₹{discount:N0} off!",
                discount = discount,
                finalTotal = finalTotal,
                discountType = coupon.DiscountType,
                discountValue = coupon.DiscountValue
            });
        }

        // ── REMOVE COUPON ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove()
        {
            HttpContext.Session.Remove("CouponCode");
            HttpContext.Session.Remove("CouponDiscount");
            return Json(new { success = true });
        }
    }
}