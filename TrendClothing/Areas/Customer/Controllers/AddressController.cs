using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class AddressController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public AddressController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Address address)
        {
            // ✅ FIX: Manual validation instead of ModelState.IsValid
            // ModelState.IsValid fail hota tha kyunki ApplicationUser
            // navigation property bind nahi hoti form se
            if (string.IsNullOrWhiteSpace(address.Name) ||
                string.IsNullOrWhiteSpace(address.Street) ||
                string.IsNullOrWhiteSpace(address.City) ||
                string.IsNullOrWhiteSpace(address.State) ||
                string.IsNullOrWhiteSpace(address.PostalCode))
            {
                TempData["ToastMessage"] = "Please fill all address fields ❌";
                TempData["ToastColor"] = "red";
                return RedirectToAction("Summary", "Cart");
            }

            // ✅ Server side se UserId assign karo — form se nahi
            address.ApplicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _unitOfWork.Address.Add(address);
            _unitOfWork.Save();

            TempData["ToastMessage"] = "Address saved successfully ✅";
            TempData["ToastColor"] = "#198754";
            return RedirectToAction("Summary", "Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var address = _unitOfWork.Address.FirstOrDefault(
                a => a.Id == id && a.ApplicationUserId == userId);

            if (address == null)
                return Json(new { success = false });

            _unitOfWork.Address.Remove(address);
            _unitOfWork.Save();

            // ✅ JSON return — AJAX se call hota hai
            return Json(new { success = true });
        }
    }
}