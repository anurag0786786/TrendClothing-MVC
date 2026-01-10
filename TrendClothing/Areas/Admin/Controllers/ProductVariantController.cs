using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
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

        public IActionResult Index()
        {
            return View();
        }

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
        [HttpGet]
        public IActionResult GetProductPrice(int productId)
        {
            var product = _unitofWork.product.Get(productId);
            if (product == null)
                return Json(0);

            return Json(product.DiscountPrice > 0 ? product.DiscountPrice : product.Price);
        }


        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var variant = _unitofWork.ProductVariant.Get(id);
            if (variant == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitofWork.ProductVariant.Remove(variant);
            _unitofWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }

        public IActionResult Upsert()
        {
            ProductVariantVM vm = new ProductVariantVM
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
            Console.WriteLine("POST HIT");

            if (vm.SelectedSizeIds == null || vm.SelectedColorIds == null ||
                !vm.SelectedSizeIds.Any() || !vm.SelectedColorIds.Any())
            {
                Console.WriteLine("SIZE OR COLOR EMPTY");

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
                    Console.WriteLine($"INSERT TRY: {sizeId} - {colorId}");

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
            Console.WriteLine("SAVE DONE");

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Edit(int id)
        {
            var variant = _unitofWork.ProductVariant.FirstOrDefault(
                u => u.Id == id,
                IncludeProperties: "Product,Size,Color"
            );

            if (variant == null)
                return NotFound();

            return View(variant);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVariant obj)
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            var variantFromDb = _unitofWork.ProductVariant.FirstOrDefault(
                u => u.Id == obj.Id
            );

            if (variantFromDb == null)
            {
                return NotFound();
            }

            variantFromDb.Price = obj.Price;
            variantFromDb.Stock = obj.Stock;

            _unitofWork.Save();

            return RedirectToAction(nameof(Index));
        }




    }
}
