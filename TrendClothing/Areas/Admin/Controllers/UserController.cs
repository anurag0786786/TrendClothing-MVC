using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrendClothing.Data;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Utility;

namespace TrendClothing.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region APIs

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _context.ApplicationUsers.ToList();
            var roles = _context.Roles.ToList();
            var userRoles = _context.UserRoles.ToList();

            foreach (var user in users)
            {
                var roleUser = userRoles.FirstOrDefault(u => u.UserId == user.Id);
                if (roleUser != null)
                {
                    user.Roles = roles.FirstOrDefault(r => r.Id == roleUser.RoleId)?.Name;
                }
            }

            // ❗ Admin users remove
            users = users.Where(u => u.Roles != SD.Role_Admin).ToList();

            return Json(new { data = users });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "User not found"
                });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            {
                user.LockoutEnd = DateTime.Now;
            }
            else
            {
                user.LockoutEnd = DateTime.Now.AddYears(100);
            }

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                message = user.LockoutEnd > DateTime.Now
                    ? "User successfully locked"
                    : "User successfully unlocked"
            });
        }

        #endregion
    }
}
