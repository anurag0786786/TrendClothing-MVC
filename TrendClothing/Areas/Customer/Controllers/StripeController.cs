using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

using TrendClothing.Models;
using TrendClothing.Models.ViewModels; // ✅ ADDED (OrderEmailVM)
using TrendClothing.Utility;
using TrendClothing;
using TrendClothing.DataAccess.Repository.IRepository;


namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class StripeController : Controller
    {
        private readonly IUnitofWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly EmailTemplateRenderer _emailTemplateRenderer;
        private readonly ISmsSender _smsSender;
        public StripeController(
            IUnitofWork unitOfWork,
            IEmailSender emailSender,
            EmailTemplateRenderer emailTemplateRenderer,
            ISmsSender smsSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _emailTemplateRenderer = emailTemplateRenderer;
            _smsSender = smsSender;
        }

        [HttpPost]
        [Authorize]
        public IActionResult Pay(int SelectedAddressId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔹 Address fetch
            var address = _unitOfWork.Address
                .FirstOrDefault(a => a.Id == SelectedAddressId
                                  && a.ApplicationUserId == userId);

            if (address == null)
            {
                TempData["ToastMessage"] = "Please select a valid delivery address ❌";
                TempData["ToastColor"] = "red";
                return RedirectToAction("Summary", "Cart");
            }

            // 🔹 Address session me save (order ke liye)
            HttpContext.Session.SetString("Order_Name", address.Name);
            HttpContext.Session.SetString("Order_Street", address.Street);
            HttpContext.Session.SetString("Order_City", address.City);
            HttpContext.Session.SetString("Order_State", address.State);
            HttpContext.Session.SetString("Order_Postal", address.PostalCode);

            // 🔹 Cart items
            var cartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                IncludeProperties: "ProductVariant,ProductVariant.Product"
            );

            if (!cartList.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // 🔹 Stripe session create
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = "https://localhost:7154/Customer/Stripe/Success",
                CancelUrl = "https://localhost:7154/Customer/Cart/Summary",
                LineItems = new List<SessionLineItemOptions>()
            };

            foreach (var item in cartList)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    Quantity = item.Count,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "inr",
                        UnitAmount = (long)(item.ProductVariant.Price * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.ProductVariant.Product.Name
                        }
                    }
                });
            }

            var service = new SessionService();
            Session session = service.Create(options);

            return Redirect(session.Url);
        }



        [Authorize]
        public async Task<IActionResult> Success()
        {
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //string userEmail = User.FindFirstValue(ClaimTypes.Email);
            //string userPhone = User.FindFirstValue(ClaimTypes.MobilePhone);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ApplicationUser user = _unitOfWork.ApplicationUser
    .FirstOrDefault(u => u.Id == userId);


            string userPhone = user?.PhoneNumber;
            string userEmail = user?.Email;




            var cartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                IncludeProperties: "ProductVariant,ProductVariant.Product"
            ).ToList();

            if (!cartList.Any())
                return RedirectToAction("Index", "Order");

            var name = HttpContext.Session.GetString("Order_Name");
            var street = HttpContext.Session.GetString("Order_Street");
            var city = HttpContext.Session.GetString("Order_City");
            var state = HttpContext.Session.GetString("Order_State");
            var postal = HttpContext.Session.GetString("Order_Postal");

            OrderHeader orderHeader = new OrderHeader
            {
                ApplicationuserId = userId,
                OrderDate = DateTime.Now,
                ShippingDate = DateTime.Now.AddDays(3),
                PaymentDate = DateTime.Now,
                PaymentDueDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"),
                OrderStatus = SD.OrderStatusApproved,
                PaymentStatus = SD.PaymentStatusApproved,
                OrderTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count),
                Carrier = "Stripe",
                TrackingNumber = "NA",
                TransactionId = "Stripe-Paid",
                Name = name,
                StreetAddress = street,
                PhoneNumber = userPhone,
                City = city,
                State = state,
                PostalCode = postal
            };

            _unitOfWork.OrderHeader.Add(orderHeader);
            _unitOfWork.Save();
            


            foreach (var cart in cartList)
            {
                OrderDetails details = new OrderDetails
                {
                    OrderHeaderId = orderHeader.Id,
                    ProductId = cart.ProductVariant.ProductId,
                    Count = cart.Count,
                    Price = cart.ProductVariant.Price
                };

                _unitOfWork.OrderDetails.Add(details);
            }

            _unitOfWork.Save();

            // ================= ONLY CHANGE START =================

            var emailVM = new OrderEmailVM
            {
                OrderId = orderHeader.Id,
                Name = orderHeader.Name,
                Email = userEmail,
                PhoneNumber = userPhone,

                StreetAddress = orderHeader.StreetAddress,
                City = orderHeader.City,
                State = orderHeader.State,
                PostalCode = orderHeader.PostalCode,
                ExpectedFrom = DateTime.Now.AddDays(7),
                ExpectedTo = DateTime.Now.AddDays(14),
                Products = cartList.Select(c =>
                    (c.ProductVariant.Product.Name, c.Count)
     ).ToList()
            };

            try
            {
                string emailBody = await _emailTemplateRenderer.RenderToStringAsync(
                    this.ControllerContext,
                    "/Areas/Customer/Views/Email/OrderConfirmationEmail.cshtml",
                    emailVM
                );

                await _emailSender.SendEmailAsync(
                    emailVM.Email,
                    "Order Confirmed – Trend Clothing",
                    emailBody
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString()); // prod me baad me empty catch kar sakta hai
            }

            try
            {
                if (!string.IsNullOrEmpty(userPhone))
                {
                    await _smsSender.SendSmsAsync(
                        userPhone,
                        $"Hi {orderHeader.Name}, your order #{orderHeader.Id} has been placed successfully! 🎉"
                    );
                }
            }
            catch
            {
                // SMS fail ho jaye to order flow break nahi hona chahiye
            }
            Console.WriteLine("USER PHONE = " + userPhone);





            // ================= ONLY CHANGE END =================

            _unitOfWork.ShoppingCart.RemoveRange(cartList);
            _unitOfWork.Save();

            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, 0);

            HttpContext.Session.Remove("Order_Name");
            HttpContext.Session.Remove("Order_Street");
            HttpContext.Session.Remove("Order_City");
            HttpContext.Session.Remove("Order_State");
            HttpContext.Session.Remove("Order_Postal");

            return RedirectToAction("OrderSuccess", "Order",new { area = "Customer", id = orderHeader.Id } );

        }
    }
}
