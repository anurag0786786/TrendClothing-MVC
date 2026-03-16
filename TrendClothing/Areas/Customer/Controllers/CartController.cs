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

        [HttpGet]
        public IActionResult GetCartCount()
        {
            if (!User.Identity.IsAuthenticated)
                return Json(new { count = 0 });
            var sessionCount = HttpContext.Session.GetInt32(SD.Ss_cartSessionCount);
            if (sessionCount != null)
                return Json(new { count = sessionCount });
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId).Count();
            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, count);
            return Json(new { count });
        }

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
                _unitOfWork.ShoppingCart.Add(new ShoppingCart
                {
                    ApplicationUserId = userId,
                    ProductVariantId = variantId,
                    Count = count
                });
            else
                cartFromDb.Count += count;
            _unitOfWork.Save();
            var currentCount = HttpContext.Session.GetInt32(SD.Ss_cartSessionCount) ?? 0;
            var cartCount = cartFromDb == null ? currentCount + 1 : currentCount;
            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, cartCount);
            return Json(new { success = true, message = "Cart mein add ho gaya!", cartCount });
        }

        // ── PLUS — JSON ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Plus(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(
                c => c.Id == id && c.ApplicationUserId == userId);
            if (cart == null)
                return Json(new { success = false });
            var variant = _unitOfWork.ProductVariant.FirstOrDefault(v => v.Id == cart.ProductVariantId);
            if (variant != null && cart.Count >= variant.Stock)
                return Json(new { success = false, message = $"Max stock ({variant.Stock}) reached" });
            cart.Count += 1;
            _unitOfWork.Save();
            return Json(new { success = true, newCount = cart.Count });
        }

        // ── MINUS — JSON ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Minus(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(
                c => c.Id == id && c.ApplicationUserId == userId);
            if (cart == null)
                return Json(new { success = false });
            if (cart.Count > 1)
                cart.Count -= 1;
            _unitOfWork.Save();
            return Json(new { success = true, newCount = cart.Count });
        }

        // ── REMOVE — JSON ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(
                c => c.Id == id && c.ApplicationUserId == userId);
            if (cart == null)
                return Json(new { success = false });
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();
            var count = _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == userId).Count();
            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, count);
            return Json(new { success = true, cartCount = count });
        }

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