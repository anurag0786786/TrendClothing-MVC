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
    public class  SizeController: Controller
    {
        private readonly IUnitofWork _unitofWork;
        public SizeController(IUnitofWork unitofWork)
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
            var sizeList = _unitofWork.size.GetAll();
            return Json(new { data = sizeList });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var sizeIdDb = _unitofWork.size.Get(id);
            if (sizeIdDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitofWork.size.Remove(sizeIdDb);
            _unitofWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
        public IActionResult Upsert(int? id)
        {
            Size size = new Size();
            if (id == null) return View(size);
            size = _unitofWork.size.Get(id.GetValueOrDefault());
            if (size == null) return NotFound();
            return View(size);
        }
        [HttpPost]
        public IActionResult Upsert(Size size)
        {
            if (size == null) return BadRequest();
            if (!ModelState.IsValid) return View(size);
            if (size.Id == 0)
            {
                _unitofWork.size.Add(size);

            }
            else
            {
                _unitofWork.size.Update(size);
            }
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));

        }
    }
}
