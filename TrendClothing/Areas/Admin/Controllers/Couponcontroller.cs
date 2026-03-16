// Areas/Admin/Controllers/CouponController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CouponController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public CouponController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var coupons = _unitOfWork.Coupon.GetAll().OrderByDescending(c => c.CreatedAt);
            return View(coupons);
        }

        // GET: Create
        public IActionResult Upsert(int? id)
        {
            var coupon = id == null ? new Coupon() : _unitOfWork.Coupon.Get(id.Value);
            if (coupon == null) return NotFound();
            return View(coupon);
        }

        // POST: Create / Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Coupon coupon)
        {
            // Force uppercase
            coupon.Code = coupon.Code?.Trim().ToUpper();

            if (!ModelState.IsValid)
                return View(coupon);

            // Duplicate code check
            var existing = _unitOfWork.Coupon.FirstOrDefault(
                c => c.Code == coupon.Code && c.Id != coupon.Id);
            if (existing != null)
            {
                ModelState.AddModelError("Code", "Yeh code already exist karta hai");
                return View(coupon);
            }

            if (coupon.Id == 0)
                _unitOfWork.Coupon.Add(coupon);
            else
                _unitOfWork.Coupon.Update(coupon);

            _unitOfWork.Save();
            TempData["ToastMessage"] = coupon.Id == 0 ? "Coupon created ✅" : "Coupon updated ✅";
            return RedirectToAction(nameof(Index));
        }

        // Toggle Active/Inactive
        [HttpPost]
        public IActionResult Toggle(int id)
        {
            var coupon = _unitOfWork.Coupon.Get(id);
            if (coupon == null) return Json(new { success = false });
            coupon.IsActive = !coupon.IsActive;
            _unitOfWork.Coupon.Update(coupon);
            _unitOfWork.Save();
            return Json(new { success = true, isActive = coupon.IsActive });
        }

        // Delete
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var coupon = _unitOfWork.Coupon.Get(id);
            if (coupon == null) return Json(new { success = false });
            _unitOfWork.Coupon.Remove(coupon);
            _unitOfWork.Save();
            return Json(new { success = true });
        }
    }
}