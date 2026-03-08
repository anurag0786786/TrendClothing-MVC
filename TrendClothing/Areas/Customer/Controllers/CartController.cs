using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository;
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

        // ================= GET CART COUNT (for navbar) =================
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

        // ================= CART INDEX =================
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var cartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                IncludeProperties: "ProductVariant,ProductVariant.Product,ProductVariant.Size,ProductVariant.Color"
            ).ToList();

            foreach (var cart in cartList)
            {
                cart.Price = cart.ProductVariant.Price;
            }

            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, cartList.Count);

            // 🔥 IMPORTANT
            ViewBag.IsEmailConfirmed = user.EmailConfirmed;

            return View(new ShoppingCartVM
            {
                ListCart = cartList
            });
        }

        // ================= ADD TO CART =================


        public IActionResult AddToCart(int variantId, int count = 1, string returnUrl = null)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity", ReturnUrl = returnUrl }
                );
            }

            if (variantId <= 0)
            {
                TempData["ToastMessage"] = "Please select size and color first ❌";
                TempData["ToastColor"] = "red";
                return RedirectToAction("Index", "Home");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartFromDb = _unitOfWork.ShoppingCart
                .FirstOrDefault(c => c.ApplicationUserId == userId && c.ProductVariantId == variantId);

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
            TempData["ToastMessage"] = "Item added to cart 🛒";
            TempData["ToastColor"] = "#0d6efd";

            var countCart = _unitOfWork.ShoppingCart
                .GetAll(c => c.ApplicationUserId == userId).Count();

            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, countCart);

            return Redirect(returnUrl ?? Url.Action("Index", "Cart"));


        }

        // ================= PLUS =================
        public IActionResult Plus(int id)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(c => c.Id == id);
            if (cart == null) return RedirectToAction(nameof(Index));

            cart.Count += 1;
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        // ================= MINUS =================
        public IActionResult Minus(int id)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(c => c.Id == id);
            if (cart == null) return RedirectToAction(nameof(Index));

            if (cart.Count > 1)
                cart.Count -= 1;

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        // ================= REMOVE =================
        public IActionResult Remove(int id)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(c => c.Id == id);
            if (cart == null) return RedirectToAction(nameof(Index));

            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = _unitOfWork.ShoppingCart
                .GetAll(c => c.ApplicationUserId == userId).Count();

            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, count);

            return RedirectToAction(nameof(Index));
        }
        [Authorize]
        [HttpPost]
        public IActionResult Payment(int SelectedAddressId)
        {
            HttpContext.Session.SetInt32("SelectedAddressId", SelectedAddressId);
            return View();
        }

        // ================= SUMMARY =================

        [Authorize]
        //public IActionResult Summary()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    var cartList = _unitOfWork.ShoppingCart.GetAll(
        //        c => c.ApplicationUserId == userId,
        //        IncludeProperties: "ProductVariant,ProductVariant.Product"
        //    ).ToList();

        //    double total = 0;

        //    foreach (var cart in cartList)
        //    {
        //        cart.Price = cart.ProductVariant.Price;
        //        total += cart.Price * cart.Count;
        //    }

        //    return View(new ShoppingCartVM
        //    {
        //        ListCart = cartList,
        //        OrderHeader = new OrderHeader
        //        {
        //            OrderTotal = total
        //        }
        //    });
        //}
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


    }
}