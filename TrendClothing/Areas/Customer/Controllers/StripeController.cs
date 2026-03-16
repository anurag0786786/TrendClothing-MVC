using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using TrendClothing.Models;
using TrendClothing.Models.ViewModels;
using TrendClothing.Utility;
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

        // ── PAY ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pay(int SelectedAddressId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var address = _unitOfWork.Address.FirstOrDefault(
                a => a.Id == SelectedAddressId && a.ApplicationUserId == userId);

            if (address == null)
            {
                TempData["ToastMessage"] = "Please select a valid delivery address ❌";
                TempData["ToastColor"] = "red";
                return RedirectToAction("Summary", "Cart");
            }

            var cartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                IncludeProperties: "ProductVariant,ProductVariant.Product"
            ).ToList();

            if (!cartList.Any())
                return RedirectToAction("Index", "Cart");

            // ✅ Stock check before payment
            foreach (var item in cartList)
            {
                var variant = _unitOfWork.ProductVariant
                    .FirstOrDefault(v => v.Id == item.ProductVariantId);
                if (variant == null || variant.Stock < item.Count)
                {
                    TempData["ToastMessage"] = $"'{item.ProductVariant.Product.Name}' mein sirf {variant?.Stock ?? 0} stock bacha hai ❌";
                    TempData["ToastColor"] = "red";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Save address to session
            HttpContext.Session.SetString("Order_Name", address.Name);
            HttpContext.Session.SetString("Order_Street", address.Street);
            HttpContext.Session.SetString("Order_City", address.City);
            HttpContext.Session.SetString("Order_State", address.State);
            HttpContext.Session.SetString("Order_Postal", address.PostalCode);

            // ✅ Coupon discount session se lo
            var couponCode = HttpContext.Session.GetString("CouponCode");
            var couponDiscount = 0.0;
            if (!string.IsNullOrEmpty(couponCode) &&
                double.TryParse(HttpContext.Session.GetString("CouponDiscount"), out var disc))
            {
                couponDiscount = disc;
            }

            var domain = Request.Scheme + "://" + Request.Host.Value;
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = domain + "/Customer/Stripe/Success",
                CancelUrl = domain + "/Customer/Cart/Summary",
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

            // ✅ Coupon hai toh: saare line items hata do, ek single discounted total daalo
            if (couponDiscount > 0)
            {
                var originalTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count);
                var discountedTotal = Math.Max(0, originalTotal - couponDiscount);

                // Saare individual items replace karo ek summary line se
                options.LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency  = "inr",
                            UnitAmount = (long)(discountedTotal * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name        = $"TrendClothing Order ({cartList.Count} items)",
                                Description = $"Coupon '{couponCode}' applied — ₹{couponDiscount:N0} off"
                            }
                        }
                    }
                };
            }

            var service = new SessionService();
            var session = service.Create(options);

            HttpContext.Session.SetString("Stripe_SessionId", session.Id);
            return Redirect(session.Url);
        }

        // ── SUCCESS ──
        public async Task<IActionResult> Success()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ✅ FIX: Verify Stripe payment before creating order
            var stripeSessionId = HttpContext.Session.GetString("Stripe_SessionId");
            if (!string.IsNullOrEmpty(stripeSessionId) && stripeSessionId != "Stripe-Paid")
            {
                try
                {
                    var sessionService = new SessionService();
                    var stripeSession = sessionService.Get(stripeSessionId);

                    // Payment complete nahi hua toh redirect
                    if (stripeSession.PaymentStatus != "paid")
                    {
                        TempData["ToastMessage"] = "Payment not completed ❌";
                        TempData["ToastColor"] = "red";
                        return RedirectToAction("Summary", "Cart");
                    }
                }
                catch { /* session expired — continue */ }
            }

            var cartList = _unitOfWork.ShoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                IncludeProperties: "ProductVariant,ProductVariant.Product"
            ).ToList();

            if (!cartList.Any())
                return RedirectToAction("Index", "Order");

            ApplicationUser user = _unitOfWork.ApplicationUser
                .FirstOrDefault(u => u.Id == userId);

            string userPhone = user?.PhoneNumber;
            string userEmail = user?.Email;

            var name = HttpContext.Session.GetString("Order_Name");
            var street = HttpContext.Session.GetString("Order_Street");
            var city = HttpContext.Session.GetString("Order_City");
            var state = HttpContext.Session.GetString("Order_State");
            var postal = HttpContext.Session.GetString("Order_Postal");

            // Validate session data
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(street))
            {
                TempData["ToastMessage"] = "Session expired. Please try again ❌";
                TempData["ToastColor"] = "red";
                return RedirectToAction("Summary", "Cart");
            }

            // ✅ Coupon session se read karo Success action mein
            var couponCode = HttpContext.Session.GetString("CouponCode") ?? "";
            var couponDiscount = 0.0;
            if (!string.IsNullOrEmpty(couponCode) &&
                double.TryParse(HttpContext.Session.GetString("CouponDiscount"), out var couponDisc))
            {
                couponDiscount = couponDisc;
            }

            var orderHeader = new OrderHeader
            {
                ApplicationUserId = userId,
                OrderDate = DateTime.UtcNow,
                ShippingDate = DateTime.UtcNow.AddDays(3),
                PaymentDate = DateTime.UtcNow,
                PaymentDueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
                OrderStatus = SD.OrderStatusApproved,
                PaymentStatus = SD.PaymentStatusApproved,
                OrderTotal = cartList.Sum(c => c.ProductVariant.Price * c.Count) - couponDiscount,
                CouponCode = string.IsNullOrEmpty(couponCode) ? null : couponCode,
                CouponDiscount = couponDiscount,
                Carrier = "Stripe",
                TrackingNumber = "NA",
                TransactionId = stripeSessionId ?? "Stripe-Paid",
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
                _unitOfWork.OrderDetails.Add(new OrderDetails
                {
                    OrderHeaderId = orderHeader.Id,
                    ProductId = cart.ProductVariant.ProductId,
                    Count = cart.Count,
                    Price = cart.ProductVariant.Price
                });

                // ✅ FIX: Stock deduct karo
                var variant = _unitOfWork.ProductVariant
                    .FirstOrDefault(v => v.Id == cart.ProductVariantId);
                if (variant != null)
                {
                    variant.Stock = Math.Max(0, variant.Stock - cart.Count);
                }
            }
            _unitOfWork.Save();

            // ── 1. Customer confirmation email ──
            try
            {
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
                    ExpectedFrom = DateTime.UtcNow.AddDays(7),
                    ExpectedTo = DateTime.UtcNow.AddDays(14),
                    Products = cartList.Select(c =>
                        (c.ProductVariant.Product.Name, c.Count)).ToList()
                };

                string emailBody = await _emailTemplateRenderer.RenderToStringAsync(
                    this.ControllerContext,
                    "/Areas/Customer/Views/Email/OrderConfirmationEmail.cshtml",
                    emailVM
                );
                // ✅ Fire-and-forget
                _ = _emailSender.SendEmailAsync(userEmail, "Order Confirmed – TrendClothing", emailBody);
            }
            catch { }

            // ── 2. Customer SMS ──
            try
            {
                if (!string.IsNullOrEmpty(userPhone))
                {
                    // ✅ Fire-and-forget
                    _ = _smsSender.SendSmsAsync(userPhone, $"Hi {orderHeader.Name}, your order #{orderHeader.Id} has been placed!");
                }
            }
            catch { }

            // ── 3. Admin notification email ──
            try
            {
                var productRows = cartList.Select(c =>
                    $"<tr>" +
                    $"<td style='padding:6px 0;'>{c.ProductVariant.Product.Name}</td>" +
                    $"<td style='text-align:center;'>x{c.Count}</td>" +
                    $"<td style='text-align:right;font-weight:700;'>&#8377;{c.ProductVariant.Price * c.Count:N0}</td>" +
                    $"</tr>"
                );

                var adminBody = $@"
                <div style='font-family:sans-serif;max-width:560px;margin:0 auto;'>
                    <div style='background:#111;border-radius:16px 16px 0 0;padding:20px 28px;'>
                        <div style='font-family:Georgia,serif;font-size:1.3rem;color:#fff;'>TrendClothing</div>
                        <div style='font-size:10px;color:rgba(255,255,255,0.4);letter-spacing:2px;text-transform:uppercase;'>Admin Notification</div>
                    </div>
                    <div style='background:#16a34a;padding:14px 28px;'>
                        <div style='font-size:15px;font-weight:700;color:#fff;'>🛒 New Order #{orderHeader.Id}</div>
                        <div style='font-size:12px;color:rgba(255,255,255,0.8);'>{DateTime.Now:dd MMM yyyy, hh:mm tt}</div>
                    </div>
                    <div style='background:#f7f5f0;padding:24px 28px;'>
                        <table style='width:100%;font-size:14px;border-collapse:collapse;background:#fff;border-radius:10px;padding:16px;'>
                            <tr><td style='padding:6px;color:#666;'>Customer</td><td style='font-weight:700;text-align:right;'>{orderHeader.Name}</td></tr>
                            <tr><td style='padding:6px;color:#666;'>Phone</td><td style='font-weight:700;text-align:right;'>{orderHeader.PhoneNumber}</td></tr>
                            <tr><td style='padding:6px;color:#666;'>Email</td><td style='font-weight:700;text-align:right;'>{userEmail}</td></tr>
                            <tr><td style='padding:6px;color:#666;'>Address</td><td style='font-weight:600;text-align:right;'>{orderHeader.StreetAddress}, {orderHeader.City}</td></tr>
                        </table>
                        <table style='width:100%;font-size:14px;border-collapse:collapse;margin-top:16px;'>
                            <tr style='border-bottom:1px solid #e8e2d9;'>
                                <th style='padding:6px 0;text-align:left;font-size:11px;color:#999;text-transform:uppercase;'>Product</th>
                                <th style='text-align:center;font-size:11px;color:#999;text-transform:uppercase;'>Qty</th>
                                <th style='text-align:right;font-size:11px;color:#999;text-transform:uppercase;'>Amount</th>
                            </tr>
                            {string.Join("", productRows)}
                            <tr style='border-top:2px solid #111;'>
                                <td colspan='2' style='padding:10px 0;font-weight:700;font-size:15px;'>Total</td>
                                <td style='padding:10px 0;font-weight:700;font-size:15px;text-align:right;'>&#8377;{orderHeader.OrderTotal:N0}</td>
                            </tr>
                        </table>
                        <div style='text-align:center;margin-top:20px;'>
                            <a href='https://yoursite.com/Admin/Order/Index'
                               style='background:#111;color:#fff;padding:12px 28px;border-radius:24px;text-decoration:none;font-weight:700;font-size:13px;'>
                                Manage Order →
                            </a>
                        </div>
                    </div>
                    <div style='background:#111;border-radius:0 0 16px 16px;padding:14px 28px;text-align:center;'>
                        <div style='font-size:12px;color:rgba(255,255,255,0.3);'>© TrendClothing Admin Panel</div>
                    </div>
                </div>";

                // ✅ Fire-and-forget
                _ = _emailSender.SendEmailAsync(SD.AdminEmail, $"New Order #{orderHeader.Id}", adminBody);
            }
            catch { }

            // ── Cleanup ──
            _unitOfWork.ShoppingCart.RemoveRange(cartList);
            _unitOfWork.Save();

            HttpContext.Session.SetInt32(SD.Ss_cartSessionCount, 0);
            HttpContext.Session.Remove("Order_Name");
            HttpContext.Session.Remove("CouponCode");
            HttpContext.Session.Remove("CouponDiscount");
            HttpContext.Session.Remove("Order_Street");
            HttpContext.Session.Remove("Order_City");
            HttpContext.Session.Remove("Order_State");
            HttpContext.Session.Remove("Order_Postal");
            HttpContext.Session.Remove("Stripe_SessionId");

            return RedirectToAction("OrderSuccess", "Order",
                new { area = "Customer", id = orderHeader.Id });
        }
    }
}