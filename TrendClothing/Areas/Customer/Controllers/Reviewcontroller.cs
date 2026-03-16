using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ReviewController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public ReviewController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET REVIEWS (AJAX) ──
        [HttpGet]
        public IActionResult GetReviews(int productId)
        {
            var reviews = _unitOfWork.ProductReview.GetAll(
                r => r.ProductId == productId && r.IsVisible,
                IncludeProperties: "ApplicationUser"
            ).OrderByDescending(r => r.CreatedAt)
             .Select(r => new
             {
                 r.Id,
                 r.Rating,
                 r.ReviewText,
                 ReviewerName = r.ApplicationUser?.Name ?? "Customer",
                 Date = r.CreatedAt.ToString("dd MMM yyyy")
             }).ToList();

            var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            return Json(new { reviews, avgRating = Math.Round(avgRating, 1), count = reviews.Count });
        }

        // ── SUBMIT REVIEW ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Submit(int productId, int rating, string? reviewText)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["ToastMessage"] = "Pehle star select karo!";
                TempData["ToastColor"] = "red";
                return RedirectToAction("Details", "Home", new { area = "Customer", id = productId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ✅ Purchase check — Cancelled/Refunded ko exclude karo
            // Approved, Processing, Shipped sab valid hain
            var hasPurchased = _unitOfWork.OrderDetails.GetAll(
                d => d.ProductId == productId,
                IncludeProperties: "OrderHeader"
            ).Any(d => d.OrderHeader != null
                    && d.OrderHeader.ApplicationUserId == userId
                    && d.OrderHeader.OrderStatus != "Cancelled"
                    && d.OrderHeader.OrderStatus != "Refunded");

            if (!hasPurchased)
            {
                TempData["ToastMessage"] = "Sirf purchased customers review de sakte hain ❌";
                TempData["ToastColor"] = "red";
                return RedirectToAction("Details", "Home", new { area = "Customer", id = productId });
            }

            // Already reviewed?
            var existing = _unitOfWork.ProductReview.FirstOrDefault(
                r => r.ProductId == productId && r.ApplicationUserId == userId);

            if (existing != null)
            {
                existing.Rating = rating;
                existing.ReviewText = reviewText?.Trim();
                existing.CreatedAt = DateTime.UtcNow;
                _unitOfWork.ProductReview.Update(existing);
                TempData["ToastMessage"] = "Review update ho gaya ✅";
            }
            else
            {
                _unitOfWork.ProductReview.Add(new ProductReview
                {
                    ProductId = productId,
                    ApplicationUserId = userId,
                    Rating = rating,
                    ReviewText = reviewText?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsVisible = true
                });
                TempData["ToastMessage"] = "Review submit ho gaya ⭐";
            }

            _unitOfWork.Save();
            TempData["ToastColor"] = "#198754";
            return RedirectToAction("Details", "Home", new { area = "Customer", id = productId });
        }

        // ── ADMIN: Toggle visibility ──
        [HttpPost]
        [Authorize(Roles = "Admin User")]
        public IActionResult ToggleVisibility(int id)
        {
            var review = _unitOfWork.ProductReview.Get(id);
            if (review == null) return NotFound();
            review.IsVisible = !review.IsVisible;
            _unitOfWork.ProductReview.Update(review);
            _unitOfWork.Save();
            return Json(new { success = true, isVisible = review.IsVisible });
        }
    }
}