using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;
using TrendClothing.Utility;
using Microsoft.AspNetCore.Identity;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitofWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(IUnitofWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // ── GET CART COUNT ──
        [HttpGet]
        public IActionResult GetCartCount()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { count = 0 });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = _unitOfWork.ShoppingCart
                .GetAll(c => c.ApplicationUserId == userId)
                .Count();
            return Json(new { count });
        }

        // ── CART INDEX ──
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var cartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                IncludeProperties: "ProductVariant,ProductVariant.Product,ProductVariant.Size,ProductVariant.Color"
            ).ToList();

            foreach (var cart in cartList)
                cart.Price = cart.ProductVariant.Price;

            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, cartList.Count);
            ViewBag.IsEmailConfirmed = user?.EmailConfirmed ?? false;

            return View(new ShoppingCartVM { ListCart = cartList });
        }

        // ── ADD TO CART ──
        // ✅ FIX: [HttpPost] + [ValidateAntiForgeryToken] — CSRF protection
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult AddToCart(int variantId, int count = 1, string returnUrl = null)
        {
            if (variantId <= 0)
                return Json(new { success = false, message = "Please select size and color first" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var variant = _unitOfWork.ProductVariant.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
                return Json(new { success = false, message = "Product not found" });

            var cartFromDb = _unitOfWork.ShoppingCart
                .FirstOrDefault(c => c.ApplicationUserId == userId && c.ProductVariantId == variantId);

            int newTotal = (cartFromDb?.Count ?? 0) + count;

            if (variant.Stock < newTotal)
                return Json(new { success = false, message = $"Only {variant.Stock} items in stock" });

            if (cartFromDb == null)
            {
                _unitOfWork.ShoppingCart.Add(new ShoppingCart
                {
                    ApplicationUserId = userId,
                    ProductVariantId = variantId,
                    Count = count
                });
            }
            else
            {
                cartFromDb.Count += count;
            }

            _unitOfWork.Save();

            var cartCount = _unitOfWork.ShoppingCart
                .GetAll(c => c.ApplicationUserId == userId).Count();
            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, cartCount);

            return Json(new { success = true, message = "Cart mein add ho gaya!", cartCount });
        }

        // ── PLUS ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Plus(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // ✅ FIX: ownership check
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(
                c => c.Id == id && c.ApplicationUserId == userId);
            if (cart == null) return RedirectToAction(nameof(Index));

            // ✅ Stock check
            var variant = _unitOfWork.ProductVariant.FirstOrDefault(v => v.Id == cart.ProductVariantId);
            if (variant != null && cart.Count >= variant.Stock)
            {
                TempData["ToastMessage"] = $"Max stock reached ({variant.Stock}) ❌";
                TempData["ToastColor"] = "red";
                return RedirectToAction(nameof(Index));
            }

            cart.Count += 1;
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        // ── MINUS ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Minus(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // ✅ FIX: ownership check
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(
                c => c.Id == id && c.ApplicationUserId == userId);
            if (cart == null) return RedirectToAction(nameof(Index));

            if (cart.Count > 1)
                cart.Count -= 1;

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        // ── REMOVE ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // ✅ FIX: ownership check
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(
                c => c.Id == id && c.ApplicationUserId == userId);
            if (cart == null) return RedirectToAction(nameof(Index));

            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();

            var count = _unitOfWork.ShoppingCart
                .GetAll(c => c.ApplicationUserId == userId).Count();
            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, count);

            return RedirectToAction(nameof(Index));
        }

        // ── SUMMARY ──
        [Authorize]
        public IActionResult Summary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addresses = _unitOfWork.Address.GetAll(a => a.ApplicationUserId == userId);

            var cartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                IncludeProperties: "ProductVariant,ProductVariant.Product"
            ).ToList();

            double total = cartList.Sum(c => c.ProductVariant.Price * c.Count);

            return View(new CheckoutVM
            {
                Addresses = addresses,
                Cart = new ShoppingCartVM
                {
                    ListCart = cartList,
                    OrderHeader = new OrderHeader { OrderTotal = total }
                }
            });
        }

        // ── PAYMENT ──
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Payment(int SelectedAddressId)
        {
            HttpContext.Session.SetInt32("SelectedAddressId", SelectedAddressId);
            return View();
        }
    }
}