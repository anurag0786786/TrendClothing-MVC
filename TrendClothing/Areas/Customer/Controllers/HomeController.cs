using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;
using TrendClothing.Utility;

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

        private const int PageSize = 12;

        public IActionResult Index()
        {
            // ✅ Cart count — session se pehle check karo, tab hi DB hit karo
            if (User.Identity.IsAuthenticated)
            {
                var sessionCount = HttpContext.Session.GetInt32(SD.Ss_cartSessionCount);
                if (sessionCount == null)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var count = _unitOfWork.ShoppingCart
                        .GetAll(c => c.ApplicationUserId == userId).Count();
                    HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, count);
                    ViewBag.CartCount = count;
                }
                else
                {
                    ViewBag.CartCount = sessionCount;
                }
            }
            else
            {
                ViewBag.CartCount = 0;
            }

            List<SiteImage> siteImages = new();
            try { siteImages = _db.SiteImages.ToList(); }
            catch { /* fallback to default images */ }

            // Hero
            ViewBag.HeroImg = siteImages.FirstOrDefault(x => x.Key == "Hero")?.ImageUrl ?? "/Images/hero/hero.jpg";

            // 4 Collection Cards
            ViewBag.TopwearImg = siteImages.FirstOrDefault(x => x.Key == "TopwearImg")?.ImageUrl ?? "/Images/Categories/Topwear.jpg";
            ViewBag.BottomwearImg = siteImages.FirstOrDefault(x => x.Key == "BottomwearImg")?.ImageUrl ?? "/Images/Categories/Bottomwear.jpg";
            ViewBag.ActivewearImg = siteImages.FirstOrDefault(x => x.Key == "ActivewearImg")?.ImageUrl ?? "/Images/Categories/Activewear.jpg";
            ViewBag.AccessoriesImg = siteImages.FirstOrDefault(x => x.Key == "AccessoriesImg")?.ImageUrl ?? "/Images/Categories/Accessories.jpg";

            // Sub Category Images
            ViewBag.TshirtImg = siteImages.FirstOrDefault(x => x.Key == "TshirtImg")?.ImageUrl ?? "/Images/Categories/Tshirt.jpg";
            ViewBag.ShirtImg = siteImages.FirstOrDefault(x => x.Key == "ShirtImg")?.ImageUrl ?? "/Images/Categories/Shirt.jpg";
            ViewBag.JoggerImg = siteImages.FirstOrDefault(x => x.Key == "JoggerImg")?.ImageUrl ?? "/Images/Categories/Jogger.jpg";
            ViewBag.JeansImg = siteImages.FirstOrDefault(x => x.Key == "JeansImg")?.ImageUrl ?? "/Images/Categories/Jeans.jpg";
            ViewBag.TrouserImg = siteImages.FirstOrDefault(x => x.Key == "TrouserImg")?.ImageUrl ?? "/Images/Categories/Trouser.jpg";

            // ✅ PERF FIX: Sirf 8 products DB se lo — Take pehle, phir load
            var productList = _db.Products
                .Where(p => p.IsActive)
                .Include("Category").Include("Brand").Include("ProductType")
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToList();

            // ✅ Wishlist aur rating bhi ek saath
            ViewBag.HomeReviews = new Dictionary<int, double>();
            ViewBag.HomeWishlist = new HashSet<int>();

            if (productList.Any())
            {
                var ids = productList.Select(p => p.Id).ToList();
                var reviews = _unitOfWork.ProductReview
                    .GetAll(r => ids.Contains(r.ProductId) && r.IsVisible)
                    .GroupBy(r => r.ProductId)
                    .ToDictionary(g => g.Key, g => Math.Round(g.Average(r => (double)r.Rating), 1));
                ViewBag.HomeReviews = reviews;

                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var wlIds = _unitOfWork.Wishlist
                        .GetAll(w => w.ApplicationUserId == userId && ids.Contains(w.ProductId))
                        .Select(w => w.ProductId).ToHashSet();
                    ViewBag.HomeWishlist = wlIds;
                }
            }

            return View(productList);
        }

        public IActionResult Details(int id)
        {
            var product = _unitOfWork.product.FirstOrDefault(
                p => p.Id == id,
                IncludeProperties: "Category,Brand"
            );

            if (product == null) return NotFound();

            var variants = _unitOfWork.ProductVariant.GetAll(
                v => v.ProductId == id,
                IncludeProperties: "Size,Color"
            ).ToList();

            double originalPrice = product.Price;
            double sellingPrice = product.DiscountPrice ?? product.Price;
            int discountPercent = 0;

            if (product.DiscountPrice.HasValue && product.DiscountPrice < product.Price)
            {
                discountPercent = (int)Math.Round(
                    ((product.Price - product.DiscountPrice.Value) / product.Price) * 100
                );
            }

            // ✅ FIX: ViewBag set karo wishlist aur review ke liye
            ViewBag.IsWishlisted = false;
            ViewBag.HasPurchased = false;

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Wishlist check
                ViewBag.IsWishlisted = _unitOfWork.Wishlist.FirstOrDefault(
                    w => w.ApplicationUserId == userId && w.ProductId == id) != null;

                // Purchase check — Cancelled/Refunded ke alawa sab valid
                ViewBag.HasPurchased = _unitOfWork.OrderDetails.GetAll(
                    d => d.ProductId == id,
                    IncludeProperties: "OrderHeader"
                ).Any(d => d.OrderHeader != null
                        && d.OrderHeader.ApplicationUserId == userId
                        && d.OrderHeader.OrderStatus != "Cancelled"
                        && d.OrderHeader.OrderStatus != "Refunded");
            }

            // ✅ Related products — same category, current product exclude, random 4
            var allRelated = _unitOfWork.product.GetAll(
                p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != id,
                IncludeProperties: "Brand"
            ).ToList();

            // Random order so different products dikhein
            var rng = new Random();
            ViewBag.RelatedProducts = allRelated
                .OrderBy(_ => rng.Next())
                .Take(4)
                .ToList();

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

        public IActionResult Privacy() => View();

        public IActionResult Category(string name, string Search, string Sort, int page = 1)
        {
            // ── Global search ──
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(Search))
            {
                var s = Search.Trim().ToLower();
                var results = _unitOfWork.product.GetAll(
                    // ✅ FIX: Filter pushed down, not loaded all then filtered in memory
                    p => p.IsActive && (
                        p.Name.ToLower().Contains(s) ||
                        p.Brand.Name.ToLower().Contains(s) ||
                        p.Category.Name.ToLower().Contains(s)),
                    IncludeProperties: "Brand,Category,ProductType"
                ).ToList();

                ViewBag.CategoryName = $"Search: \"{Search}\"";
                ViewBag.ProductTypes = results.Select(p => p.ProductType?.Name)
                                               .Where(t => t != null).Distinct().ToList();
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(results.Count / (double)PageSize);
                ViewBag.TotalProducts = results.Count;

                return View(new ProductFilterVM
                {
                    Products = results.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
                    Search = Search,
                    Sort = Sort
                });
            }

            // ── Category browse ──
            // Topwear group: T-Shirt, Shirt, Hoodie, Jacket, Oversized, Sweater etc.
            var topwearTypes = new[] { "t-shirt", "tshirt", "shirt", "hoodie", "hoodies", "jacket", "oversized", "sweater", "top", "polo" };
            // Bottomwear group: Jeans, Lower, Jogger, Trouser, Cargo, Track Pant etc.
            var bottomwearTypes = new[] { "jeans", "lower", "jogger", "trouser", "cargo", "track pant", "shorts", "pant" };
            // Activewear group
            var activewearTypes = new[] { "activewear", "gym wear", "sports", "track suit", "compression" };
            // Accessories group
            var accessoriesTypes = new[] { "accessories", "cap", "belt", "wallet", "watch", "bag", "socks", "sunglasses" };

            var nameLower = name?.Trim().ToLower() ?? "";

            List<TrendClothing.Models.Product> allCatProducts;

            // Load all active products with includes once
            var allProducts = _unitOfWork.product.GetAll(
                p => p.IsActive,
                IncludeProperties: "Brand,Category,ProductType"
            ).ToList();

            if (nameLower == "topwear")
            {
                allCatProducts = allProducts.Where(p =>
                    p.ProductType != null &&
                    topwearTypes.Any(t => p.ProductType.Name.ToLower().Contains(t))
                ).ToList();
            }
            else if (nameLower == "bottomwear")
            {
                allCatProducts = allProducts.Where(p =>
                    p.ProductType != null &&
                    bottomwearTypes.Any(t => p.ProductType.Name.ToLower().Contains(t))
                ).ToList();
            }
            else if (nameLower == "activewear")
            {
                allCatProducts = allProducts.Where(p =>
                    p.ProductType != null &&
                    activewearTypes.Any(t => p.ProductType.Name.ToLower().Contains(t))
                ).ToList();
            }
            else if (nameLower == "accessories")
            {
                allCatProducts = allProducts.Where(p =>
                    p.ProductType != null &&
                    accessoriesTypes.Any(t => p.ProductType.Name.ToLower().Contains(t))
                ).ToList();
            }
            else
            {
                // Exact Category name OR exact ProductType name match
                allCatProducts = allProducts.Where(p =>
                    p.Category.Name.ToLower() == nameLower ||
                    (p.ProductType != null && p.ProductType.Name.ToLower() == nameLower)
                ).ToList();
            }

            ViewBag.ProductTypes = allCatProducts
                .Where(p => p.ProductType != null)
                .Select(p => p.ProductType.Name)
                .Distinct().OrderBy(t => t).ToList();

            ViewBag.CategoryName = name;

            // Filter in memory (already loaded)
            var products = allCatProducts.AsQueryable();

            if (!string.IsNullOrEmpty(Search))
            {
                var s = Search.Trim().ToLower();
                products = products.Where(p =>
                    p.Name.ToLower().Contains(s) ||
                    (p.Brand != null && p.Brand.Name.ToLower().Contains(s)) ||
                    (p.ProductType != null && p.ProductType.Name.ToLower().Contains(s))
                );
            }

            products = Sort switch
            {
                "low" => products.OrderBy(p => p.DiscountPrice ?? p.Price),
                "high" => products.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                _ => products.OrderByDescending(p => p.Id)
            };

            var productList = products.ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(productList.Count / (double)PageSize);
            ViewBag.TotalProducts = productList.Count;

            var pagedProducts = productList.Skip((page - 1) * PageSize).Take(PageSize).ToList();
            var pagedIds = pagedProducts.Select(p => p.Id).ToList();

            // ✅ PERFORMANCE FIX: 1 query for all reviews on this page
            // Dictionary<int, double> = productId -> avg rating
            var pageReviews = _unitOfWork.ProductReview
                .GetAll(r => pagedIds.Contains(r.ProductId) && r.IsVisible)
                .GroupBy(r => r.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Average(r => (double)r.Rating), 1)
                );
            ViewBag.ProductReviews = pageReviews;

            // ✅ PERFORMANCE FIX: 1 query for wishlist on this page
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var wlIds = _unitOfWork.Wishlist
                    .GetAll(w => w.ApplicationUserId == userId && pagedIds.Contains(w.ProductId))
                    .Select(w => w.ProductId)
                    .ToHashSet();
                ViewBag.WishlistIds = wlIds;
            }
            else
            {
                ViewBag.WishlistIds = new HashSet<int>();
            }

            return View(new ProductFilterVM
            {
                Products = pagedProducts,
                Search = Search,
                Sort = Sort,
                Brands = _unitOfWork.brand.GetAll()
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}