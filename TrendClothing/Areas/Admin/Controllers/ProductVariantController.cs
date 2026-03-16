using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ProductVariantController : Controller
    {
        private readonly IUnitofWork _unitofWork;

        public ProductVariantController(IUnitofWork unitofWork)
        {
            _unitofWork = unitofWork;
        }

        public IActionResult Index() => View();

        // ── GET ALL ──
        [HttpGet]
        public IActionResult GetAll()
        {
            var variants = _unitofWork.ProductVariant.GetAll(
                IncludeProperties: "Product,Size,Color");

            return Json(new
            {
                data = variants.Select(v => new
                {
                    id = v.Id,
                    product = v.Product.Name,
                    size = v.Size.Name,
                    color = v.Color.Name,
                    price = v.Price,
                    stock = v.Stock
                })
            });
        }

        // ✅ QUICK STOCK UPDATE (inline from table)
        [HttpPost]
        public IActionResult UpdateStock(int id, int stock)
        {
            var variant = _unitofWork.ProductVariant.FirstOrDefault(v => v.Id == id);
            if (variant == null)
                return Json(new { success = false, message = "Variant not found" });

            if (stock < 0)
                return Json(new { success = false, message = "Stock cannot be negative" });

            variant.Stock = stock;
            _unitofWork.Save();

            return Json(new { success = true, message = $"Stock updated to {stock}" });
        }

        [HttpGet]
        public IActionResult GetProductPrice(int productId)
        {
            var product = _unitofWork.product.Get(productId);
            if (product == null) return Json(0);
            return Json(product.DiscountPrice > 0 ? product.DiscountPrice : product.Price);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var variant = _unitofWork.ProductVariant.Get(id);
            if (variant == null)
                return Json(new { success = false, message = "Error while deleting" });

            _unitofWork.ProductVariant.Remove(variant);
            _unitofWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }

        public IActionResult Upsert()
        {
            var vm = new ProductVariantVM
            {
                ProductList = _unitofWork.product.GetAll()
                    .Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() }),
                SizeList = _unitofWork.size.GetAll()
                    .Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() }),
                ColorList = _unitofWork.color.GetAll()
                    .Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() })
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVariantVM vm)
        {
            if (vm.SelectedSizeIds == null || vm.SelectedColorIds == null ||
                !vm.SelectedSizeIds.Any() || !vm.SelectedColorIds.Any())
            {
                vm.ProductList = _unitofWork.product.GetAll()
                    .Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() });
                vm.SizeList = _unitofWork.size.GetAll()
                    .Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() });
                vm.ColorList = _unitofWork.color.GetAll()
                    .Select(i => new SelectListItem { Text = i.Name, Value = i.Id.ToString() });
                return View(vm);
            }

            foreach (var sizeId in vm.SelectedSizeIds)
            {
                foreach (var colorId in vm.SelectedColorIds)
                {
                    // ✅ Skip if variant already exists
                    var existing = _unitofWork.ProductVariant.FirstOrDefault(
                        v => v.ProductId == vm.ProductId &&
                             v.SizeId == sizeId &&
                             v.ColorId == colorId);

                    if (existing != null) continue;

                    _unitofWork.ProductVariant.Add(new ProductVariant
                    {
                        ProductId = vm.ProductId,
                        SizeId = sizeId,
                        ColorId = colorId,
                        Price = vm.Price,
                        Stock = vm.Stock
                    });
                }
            }

            _unitofWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var variant = _unitofWork.ProductVariant.FirstOrDefault(
                u => u.Id == id,
                IncludeProperties: "Product,Size,Color"
            );
            if (variant == null) return NotFound();
            return View(variant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVariant obj)
        {
            if (!ModelState.IsValid) return View(obj);

            var variantFromDb = _unitofWork.ProductVariant.FirstOrDefault(u => u.Id == obj.Id);
            if (variantFromDb == null) return NotFound();

            variantFromDb.Price = obj.Price;
            variantFromDb.Stock = obj.Stock;
            _unitofWork.Save();

            TempData["success"] = "Variant updated successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}