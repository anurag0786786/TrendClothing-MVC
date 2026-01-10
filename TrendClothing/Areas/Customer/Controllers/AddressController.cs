using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository;
using TrendClothing.DataAccess.Repository.IRepository;
using TrendClothing.Models;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class AddressController : Controller
    {
        private readonly IUnitofWork _unitOfWork;

        public AddressController(IUnitofWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        public IActionResult Create(Address address)
        {
            address.ApplicationUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            _unitOfWork.Address.Add(address);
            _unitOfWork.Save();

            return RedirectToAction("Summary", "Cart");
        }
    }
}
