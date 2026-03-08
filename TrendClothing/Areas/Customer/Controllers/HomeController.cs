using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitofWork _unitOfWork;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, IUnitofWork unitofWork, ApplicationDbContext db)
        {
            _logger = logger;
            _unitOfWork = unitofWork;
            _db = db;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.CartCount = _unitOfWork.ShoppingCart.GetAll(
                    c => c.ApplicationUserId == userId
                ).Count();
            }
            else
            {
                ViewBag.CartCount = 0;
            }

            // Load site images from DB (fallback to static files if not set)
            var siteImages = _db.SiteImages.ToList();
            ViewBag.HeroImg = siteImages.FirstOrDefault(x => x.Key == "Hero")?.ImageUrl ?? "/Images/hero/hero.jpg";
            ViewBag.MenImg = siteImages.FirstOrDefault(x => x.Key == "Men")?.ImageUrl ?? "/Images/Categories/Men.jpg";
            ViewBag.WomenImg = siteImages.FirstOrDefault(x => x.Key == "Women")?.ImageUrl ?? "/Images/Categories/Women.jpg";
            ViewBag.ChildrenImg = siteImages.FirstOrDefault(x => x.Key == "Children")?.ImageUrl ?? "/Images/Categories/Children.jpg";

            var ProductList = _unitOfWork.product.GetAll(
                IncludeProperties: "Category,Brand,ProductType"
            );

            return View(ProductList);
        }

        public IActionResult Details(int id)
        {
            var product = _unitOfWork.product.FirstOrDefault(
                p => p.Id == id,
                IncludeProperties: "Category,Brand"
            );

            if (product == null)
                return NotFound();

            var variants = _unitOfWork.ProductVariant.GetAll(
                v => v.ProductId == id,
                IncludeProperties: "Size,Color"
            ).ToList();

            double originalPrice = product.Price;
            double sellingPrice = product.DiscountPrice ?? product.Price;

            int discountPercent = 0;
            if (product.DiscountPrice != null)
            {
                discountPercent = (int)Math.Round(
                    ((product.Price - product.DiscountPrice.Value) / product.Price) * 100
                );
            }

            var vm = new ProductDetailsVM
            {
                Product = product,
                Variants = variants,
                Count = 1,
                OriginalPrice = originalPrice,
                SellingPrice = sellingPrice,
                DiscountPercent = discountPercent
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Category(string name, string Search, string Sort)
        {
            // ── If user came from navbar search (no category name), redirect to search results ──
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(Search))
            {
                // Search across ALL categories
                var allProducts = _unitOfWork.product.GetAll(
                    IncludeProperties: "Brand,Category"
                );

                var search = Search.Trim().ToLower();
                var results = allProducts.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Brand != null && p.Brand.Name.ToLower().Contains(search)) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(search))
                ).ToList();

                ViewBag.CategoryName = $"Search: \"{Search}\"";
                ViewBag.ProductTypes = results.Select(p => p.ProductType?.Name)
                    .Where(t => t != null).Distinct().ToList();

                var vm2 = new ProductFilterVM
                {
                    Products = results,
                    Search = Search,
                    Sort = Sort
                };
                return View(vm2);
            }

            // ── Normal category browse ──
            var products = _unitOfWork.product.GetAll(
                IncludeProperties: "Brand,Category,ProductType"
            ).Where(p => p.Category != null && p.Category.Name == name);

            // ✅ ProductTypes = actual product TYPE names (not product names)
            var productTypes = products
                .Where(p => p.ProductType != null)
                .Select(p => p.ProductType.Name)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            ViewBag.ProductTypes = productTypes;
            ViewBag.CategoryName = name;

            // 🔍 SEARCH — case-insensitive, searches name AND brand
            if (!string.IsNullOrEmpty(Search))
            {
                var search = Search.Trim().ToLower();
                products = products.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Brand != null && p.Brand.Name.ToLower().Contains(search)) ||
                    (p.ProductType != null && p.ProductType.Name.ToLower().Contains(search))
                );
            }

            // ↕ SORTING
            products = Sort switch
            {
                "low" => products.OrderBy(p => p.DiscountPrice > 0 ? p.DiscountPrice : p.Price),
                "high" => products.OrderByDescending(p => p.DiscountPrice > 0 ? p.DiscountPrice : p.Price),
                _ => products.OrderByDescending(p => p.Id)
            };

            var vm = new ProductFilterVM
            {
                Products = products.ToList(),
                Search = Search,
                Sort = Sort,
                Brands = _unitOfWork.brand.GetAll()
            };

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}