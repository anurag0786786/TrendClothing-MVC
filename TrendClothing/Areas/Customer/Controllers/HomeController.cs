using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
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


        public HomeController(ILogger<HomeController> logger,IUnitofWork unitofWork)
        {
            _logger = logger;
            _unitOfWork = unitofWork;
        }

        public IActionResult Index()
        {
            // 🔹 Cart count for navbar
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

            // 🔹 Product list
            var ProductList = _unitOfWork.product.GetAll(
                IncludeProperties: "Category,ProductType"
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
        public IActionResult Category(string name, ProductFilterVM vm)
        {

            var products = _unitOfWork.product.GetAll(
                IncludeProperties: "Brand,Category"
            ).Where(p => p.Category.Name == name);

            var productTypes = products.Select(p => p.Name).Distinct().ToList();

            ViewBag.ProductTypes = productTypes;

            // 🔍 SEARCH
            if (!string.IsNullOrEmpty(vm.Search))
            {
                var search = vm.Search.ToLower();

                products = products.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.Brand.Name.ToLower().Contains(search));

            }

            // 🎯 BRAND FILTER
            if (vm.BrandIds.Any())
            {
                products = products.Where(p => vm.BrandIds.Contains(p.BrandId));
            }

            // 💰 PRICE FILTER
            if (vm.MinPrice.HasValue)
                products = products.Where(p => p.Price >= vm.MinPrice);

            if (vm.MaxPrice.HasValue)
                products = products.Where(p => p.Price <= vm.MaxPrice);

            // 🔥 DISCOUNT
            if (vm.MinDiscount.HasValue)
            {
                products = products.Where(p =>
                    ((p.Price - p.DiscountPrice) * 100 / p.Price) >= vm.MinDiscount);
            }

            // ↕ SORTING
            products = vm.Sort switch
            {
                "low" => products.OrderBy(p => p.DiscountPrice > 0 ? p.DiscountPrice : p.Price),
                "high" => products.OrderByDescending(p => p.DiscountPrice > 0 ? p.DiscountPrice : p.Price),
                _ => products.OrderByDescending(p => p.Id)
            };

            vm.Products = products.ToList();
            vm.Brands = _unitOfWork.brand.GetAll();

            ViewBag.CategoryName = name;
            return View(vm);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
