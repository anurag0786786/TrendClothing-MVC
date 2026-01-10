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
    public class ColorController : Controller
    {
        private readonly IUnitofWork _unitofWork;
        public ColorController(IUnitofWork unitofWork)
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
            var colorList = _unitofWork.color.GetAll();
            return Json(new { data = colorList });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var colorInDb = _unitofWork.color.Get(id);
            if (colorInDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitofWork.color.Remove(colorInDb);
            _unitofWork.Save();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
        public IActionResult Upsert(int? id)
        {
            Color color = new Color();
            if (id == null) return View(color);
            color = _unitofWork.color.Get(id.GetValueOrDefault());
            if (color == null) return NotFound();
            return View(color);
        }
        [HttpPost]
        public IActionResult Upsert(Color color)
        {
            if (color == null) return BadRequest();
            if (!ModelState.IsValid) return View(color);
            if (color.Id == 0)
            {
                _unitofWork.color.Add(color);

            }
            else
            {
                _unitofWork.color.Update(color);
            }
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));

        }
    }
}
