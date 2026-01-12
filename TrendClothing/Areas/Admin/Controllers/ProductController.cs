using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using TrendClothing.DataAccess.Repository;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ProductController : Controller
    {
        private readonly IUnitofWork _unitofWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CloudinaryService _cloudinary;
        public ProductController(IUnitofWork unitofWork,IWebHostEnvironment webHostEnvironment, CloudinaryService cloudinary)
        {
            _unitofWork = unitofWork;
            _webHostEnvironment = webHostEnvironment;
            _cloudinary = cloudinary;
        }
        public IActionResult Index()
        {
            return View();
        }
        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitofWork.product.GetAll(IncludeProperties: "Category,ProductType,Brand");
            return Json(new { data = productList });
        }
        [HttpGet]
        public IActionResult GetProductTypesByCategory(int categoryId)
        {
            var productTypes = _unitofWork.productType
                .GetAll(pt => pt.CategoryId == categoryId)
                .Select(pt => new {
                    id = pt.Id,
                    name = pt.Name
                });

            return Json(productTypes);
        }
        //[HttpDelete]
        //public IActionResult Delete(int id)
        //{
        //    var ProductInDb = _unitofWork.product.Get(id);
        //    if (ProductInDb == null)
        //    {
        //        return Json(new { success = false, Message = "Unable To Delete Data !!!" });
        //    }

        //    var WebRootPath = _webHostEnvironment.ContentRootPath;
        //    var ImagePath = Path.Combine(WebRootPath, ProductInDb.ImageUrl.Trim('\\'));
        //    if (System.IO.File.Exists(ImagePath))
        //    {
        //        System.IO.File.Delete(ImagePath);
        //    }
        //    _unitofWork.product.Remove(ProductInDb);
        //    _unitofWork.Save();
        //    return Json(new { success = true, Message = "Data Deleted Succesfully" });
        //}
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var productInDb = _unitofWork.product.Get(id);
            if (productInDb == null)
            {
                return Json(new { success = false, message = "Unable to delete" });
            }

            // ❌ NO LOCAL FILE DELETE
            _unitofWork.product.Remove(productInDb);
            _unitofWork.Save();

            return Json(new { success = true, message = "Deleted successfully" });
        }



        #endregion
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _unitofWork.category.GetAll().Select(cl => new SelectListItem
                {
                    Text = cl.Name,
                    Value = cl.Id.ToString()
                }),
                ProductTypeList = new List<SelectListItem>(),

                BrandList = _unitofWork.brand.GetAll().Select(bl => new SelectListItem
                {
                    Text = bl.Name,
                    Value = bl.Id.ToString()
                })

            };
            if (id == null) return View(productVM);
            productVM.Product = _unitofWork.product.Get(id.GetValueOrDefault());
            if (productVM.Product == null) return NotFound();
            productVM.ProductTypeList = _unitofWork.productType.GetAll(pt => pt.CategoryId == productVM.Product.CategoryId)
            .Select(pt => new SelectListItem
            {
                Text = pt.Name,
                Value = pt.Id.ToString()
            });
            return View(productVM);
        }
        //public IActionResult Upsert(int? id)
        //{
        //    ProductVM productVM = new ProductVM()
        //    {
        //        Product = new Product(),
        //        CategoryList = _unitofWork.category.GetAll().Select(cl => new SelectListItem
        //        {
        //            Text = cl.Name,
        //            Value = cl.Id.ToString()
        //        }),

        //        // ✅ FIX: EMPTY NAHI
        //        ProductTypeList = _unitofWork.productType.GetAll()
        //            .Select(pt => new SelectListItem
        //            {
        //                Text = pt.Name,
        //                Value = pt.Id.ToString()
        //            }),

        //        BrandList = _unitofWork.brand.GetAll().Select(bl => new SelectListItem
        //        {
        //            Text = bl.Name,
        //            Value = bl.Id.ToString()
        //        })
        //    };

        //    if (id == null)
        //        return View(productVM);

        //    productVM.Product = _unitofWork.product.Get(id.GetValueOrDefault());
        //    if (productVM.Product == null) return NotFound();

        //    // Edit case → category wise filter
        //    productVM.ProductTypeList = _unitofWork.productType
        //        .GetAll(pt => pt.CategoryId == productVM.Product.CategoryId)
        //        .Select(pt => new SelectListItem
        //        {
        //            Text = pt.Name,
        //            Value = pt.Id.ToString()
        //        });

        //    return View(productVM);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Upsert(ProductVM productVM)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var webRootPath = _webHostEnvironment.WebRootPath;
        //        var files = HttpContext.Request.Form.Files;

        //        if (files.Count > 0)
        //        {
        //            var fileName = Guid.NewGuid().ToString();
        //            var extension = Path.GetExtension(files[0].FileName);
        //            var uploads = Path.Combine(webRootPath, @"Images\Product");

        //            if (productVM.Product.Id != 0)
        //            {
        //                var oldImage = _unitofWork.product.Get(productVM.Product.Id).ImageUrl;
        //                productVM.Product.ImageUrl = oldImage;
        //            }

        //            if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
        //            {
        //                var oldImagePath = Path.Combine(webRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
        //                if (System.IO.File.Exists(oldImagePath))
        //                {
        //                    System.IO.File.Delete(oldImagePath);
        //                }
        //            }

        //            using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
        //            {
        //                files[0].CopyTo(fileStream);
        //            }

        //            productVM.Product.ImageUrl = @"\Images\Product\" + fileName + extension;
        //        }
        //        else
        //        {
        //            if (productVM.Product.Id != 0)
        //            {
        //                productVM.Product.ImageUrl = _unitofWork.product.Get(productVM.Product.Id).ImageUrl;
        //            }
        //        }

        //        if (productVM.Product.Id == 0)
        //        {
        //            _unitofWork.product.Add(productVM.Product);
        //        }
        //        else
        //        {
        //            _unitofWork.product.Update(productVM.Product);
        //        }

        //        _unitofWork.Save();
        //        return RedirectToAction(nameof(Index));
        //    }

        //    productVM.CategoryList = _unitofWork.category.GetAll().Select(c => new SelectListItem
        //    {
        //        Text = c.Name,
        //        Value = c.Id.ToString()
        //    });

        //    productVM.ProductTypeList = _unitofWork.productType.GetAll().Select(p => new SelectListItem
        //    {
        //        Text = p.Name,
        //        Value = p.Id.ToString()
        //    });

        //    return View(productVM);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProductVM productVM, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                productVM.CategoryList = _unitofWork.category.GetAll().Select(c =>
                    new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

                productVM.ProductTypeList = _unitofWork.productType.GetAll().Select(p =>
                    new SelectListItem { Text = p.Name, Value = p.Id.ToString() });

                productVM.BrandList = _unitofWork.brand.GetAll().Select(b =>
                    new SelectListItem { Text = b.Name, Value = b.Id.ToString() });

                return View(productVM);
            }

            // 🔥 CLOUDINARY IMAGE
            if (file != null)
            {
                var imageUrl = await _cloudinary.UploadImageAsync(file);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    ModelState.AddModelError("", "Image upload failed");
                    return View(productVM);
                }

                productVM.Product.ImageUrl = imageUrl;
            }
            else if (productVM.Product.Id != 0)
            {
                productVM.Product.ImageUrl =
                    _unitofWork.product.Get(productVM.Product.Id).ImageUrl;
            }

            if (productVM.Product.Id == 0)
                _unitofWork.product.Add(productVM.Product);
            else
                _unitofWork.product.Update(productVM.Product);

            _unitofWork.Save();
            return RedirectToAction(nameof(Index));
        }


    }
}


