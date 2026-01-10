using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Logging;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class BrandController : Controller
    {
        private readonly IUnitofWork _unitofWork;
        public BrandController(IUnitofWork unitofWork)
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
            var brandList = _unitofWork.brand.GetAll();
            return Json(new { data = brandList });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var brandInDb = _unitofWork.brand.Get(id);
            if (brandInDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitofWork.brand.Remove(brandInDb);
            _unitofWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
        public IActionResult Upsert(int? id)
        {
            Brand brand = new Brand();
            if (id == null) return View(brand);
            brand = _unitofWork.brand.Get(id.GetValueOrDefault());
            if (brand == null) return NotFound();
            return View(brand);
        }
        [HttpPost]
        public IActionResult Upsert(Brand brand)
        {
            if (brand == null) return BadRequest();
            if (!ModelState.IsValid) return View(brand);
            if (brand.Id == 0)
            {
                _unitofWork.brand.Add(brand);

            }
            else
            {
                _unitofWork.brand.Update(brand);
            }
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));

        }
    }
}
