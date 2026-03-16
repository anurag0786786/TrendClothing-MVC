using Microsoft.AspNetCore.Mvc;
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

            ViewBag.HeroImg = siteImages.FirstOrDefault(x => x.Key == "Hero")?.ImageUrl ?? "/Images/hero/hero.jpg";
            ViewBag.MenImg = siteImages.FirstOrDefault(x => x.Key == "Men")?.ImageUrl ?? "/Images/Categories/Men.jpg";
            ViewBag.WomenImg = siteImages.FirstOrDefault(x => x.Key == "Women")?.ImageUrl ?? "/Images/Categories/Women.jpg";
            ViewBag.ChildrenImg = siteImages.FirstOrDefault(x => x.Key == "Children")?.ImageUrl ?? "/Images/Categories/Children.jpg";

            var productList = _unitOfWork.product.GetAll(
                filter: p => p.IsActive,
                IncludeProperties: "Category,Brand,ProductType"
            );

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
            var products = _unitOfWork.product.GetAll(
                p => p.IsActive && p.Category.Name == name,
                IncludeProperties: "Brand,Category,ProductType"
            ).AsQueryable();

            ViewBag.ProductTypes = products
                .Where(p => p.ProductType != null)
                .Select(p => p.ProductType.Name)
                .Distinct().OrderBy(t => t).ToList();

            ViewBag.CategoryName = name;

            // Search filter
            if (!string.IsNullOrEmpty(Search))
            {
                var s = Search.Trim().ToLower();
                products = products.Where(p =>
                    p.Name.ToLower().Contains(s) ||
                    (p.Brand != null && p.Brand.Name.ToLower().Contains(s)) ||
                    (p.ProductType != null && p.ProductType.Name.ToLower().Contains(s))
                );
            }

            // Sort
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