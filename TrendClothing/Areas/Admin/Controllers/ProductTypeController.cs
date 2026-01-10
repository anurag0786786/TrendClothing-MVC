using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Utility;


namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ProductTypeController : Controller
    {
        private readonly IUnitofWork _unitofWork;
        public ProductTypeController(IUnitofWork unitofWork)
        {
            _unitofWork = unitofWork;
        }

        public IActionResult Index()
        {
            return View();
        }
        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            var data = _unitofWork.productType.GetAll(IncludeProperties: "category").GroupBy(p => p.Name).Select(g => new
            {
                id=g.First().Id,
                Name = g.Key,
                categories = string.Join(", ", g.Select(x=> x.category.Name))


            });
            return Json(new { data });

        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var productTypeDb = _unitofWork.productType.Get(id);
            if (productTypeDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitofWork.productType.Remove(productTypeDb);
            _unitofWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
        public IActionResult Upsert(int? id)
        {
            ProductType productType = new ProductType();
            ViewBag.CategoryList = _unitofWork.category.GetAll();
            if (id == null) return View(productType);

            productType = _unitofWork.productType.Get(id.GetValueOrDefault());
            if (productType == null) return NotFound();
            return View(productType);
        }
        [HttpPost]
        
        public IActionResult Upsert(ProductType productType)
        {
            if (productType == null) return BadRequest();

            if (!ModelState.IsValid) 
            {
                if (!ModelState.IsValid)
                {
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"KEY: {state.Key}  ERROR: {error.ErrorMessage}");
                        }
                    }

                    ViewBag.CategoryList = _unitofWork.category.GetAll();
                    return View(productType);
                }

                ViewBag.CategoryList = _unitofWork.category.GetAll();
                return View(productType);
            }

            if (productType.Id == 0)
            {
                _unitofWork.productType.Add(productType);
            }
            else
            {
                _unitofWork.productType.Update(productType);
            }
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));
        }
    }
}
