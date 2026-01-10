using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CategoryController : Controller
    {
        private readonly IUnitofWork _unitofWork;
        public CategoryController(IUnitofWork unitofWork)
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
            var categoryList = _unitofWork.category.GetAll();
            return Json(new { data = categoryList });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var CategoryIdDb = _unitofWork.category.Get(id);
            if(CategoryIdDb==null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitofWork.category.Remove(CategoryIdDb);
            _unitofWork.Save();
            return Json(new { success = true, message = "Delete Successful" } );
        }
        #endregion
        public IActionResult Upsert(int? id)
        {
            Category category = new Category();
            if(id==null) return View(category);
            category= _unitofWork.category.Get(id.GetValueOrDefault()); 
            if(category==null) return NotFound();
            return View(category); 
        }
        [HttpPost]
        public IActionResult Upsert(Category category)
        {
            if(category==null) return BadRequest(); 
            if(!ModelState.IsValid) return View(category);
            if(category.Id==0)
            {
                _unitofWork.category.Add(category);

            }
            else
            {
                _unitofWork.category.Update(category);
            }
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));

        }
    }
}
