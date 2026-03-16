using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public WishlistController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // VIEW WISHLIST
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = _unitOfWork.Wishlist.GetAll(
                w => w.ApplicationUserId == userId,
                IncludeProperties: "Product,Product.Brand,Product.Category"
            );
            return View(items);
        }

        // TOGGLE (Add / Remove) — returns JSON for AJAX calls
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(int productId, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = _unitOfWork.Wishlist.FirstOrDefault(
                w => w.ApplicationUserId == userId && w.ProductId == productId);

            bool isNowWishlisted;

            if (existing != null)
            {
                _unitOfWork.Wishlist.Remove(existing);
                isNowWishlisted = false;
            }
            else
            {
                var product = _unitOfWork.product.Get(productId);
                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                _unitOfWork.Wishlist.Add(new Wishlist
                {
                    ApplicationUserId = userId,
                    ProductId = productId,
                    AddedOn = DateTime.UtcNow
                });
                isNowWishlisted = true;
            }

            _unitOfWork.Save();

            // ✅ Always return JSON — AJAX pe Redirect nahi bhejte
            return Json(new
            {
                success = true,
                wishlisted = isNowWishlisted,
                message = isNowWishlisted ? "Wishlist mein add ho gaya" : "Wishlist se remove ho gaya"
            });
        }

        // CHECK (heart icon ke liye)
        [HttpGet]
        public IActionResult IsWishlisted(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exists = _unitOfWork.Wishlist.FirstOrDefault(
                w => w.ApplicationUserId == userId && w.ProductId == productId) != null;
            return Json(new { wishlisted = exists });
        }

        // GET COUNT
        [HttpGet]
        public IActionResult GetCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = _unitOfWork.Wishlist.GetAll(w => w.ApplicationUserId == userId).Count();
            return Json(new { count });
        }
    }
}