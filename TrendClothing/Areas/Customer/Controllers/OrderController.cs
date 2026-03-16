using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using TrendClothing.DataAccess.Repository.IRepository;

namespace TrendClothing.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitofWork _unitOfWork;
        private readonly IEmailSender _emailSender;

        public OrderController(IUnitofWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        // ================= ORDER HISTORY =================
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = _unitOfWork.OrderHeader.GetAll(
                o => o.ApplicationUserId == userId
            ).OrderByDescending(o => o.OrderDate);

            // ✅ Explicitly view name specify karo
            return View("Index", orders);
        }

        // ================= ORDER DETAILS =================
        public IActionResult Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orderHeader = _unitOfWork.OrderHeader.FirstOrDefault(
                o => o.Id == id && o.ApplicationUserId == userId
            );
            if (orderHeader == null) return NotFound();

            var orderDetails = _unitOfWork.OrderDetails.GetAll(
                d => d.OrderHeaderId == id,
                IncludeProperties: "Product"
            );

            ViewBag.OrderHeader = orderHeader;
            return View("Details", orderDetails);
        }

        // ================= ORDER SUCCESS =================
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // ================= CANCEL ORDER + REFUND + EMAIL =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = _unitOfWork.OrderHeader.FirstOrDefault(
                o => o.Id == id && o.ApplicationUserId == userId
            );

            if (order == null) return NotFound();

            if (order.OrderStatus == "Pending" || order.OrderStatus == "Approved")
            {
                bool refundDone = false;

                // ✅ Stripe Refund
                if (!string.IsNullOrEmpty(order.TransactionId)
                    && order.TransactionId != "Stripe-Paid")
                {
                    try
                    {
                        var sessionService = new SessionService();
                        var session = sessionService.Get(order.TransactionId);

                        if (session?.PaymentIntentId != null)
                        {
                            var refundOptions = new RefundCreateOptions
                            {
                                PaymentIntent = session.PaymentIntentId
                            };
                            var refundService = new RefundService();
                            refundService.Create(refundOptions);

                            order.PaymentStatus = "Refunded";
                            refundDone = true;
                        }
                    }
                    catch { }
                }

                order.OrderStatus = "Cancelled";
                _unitOfWork.Save();

                // ✅ Cancellation email
                try
                {
                    var user = _unitOfWork.ApplicationUser
                        .FirstOrDefault(u => u.Id == userId);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var refundLine = refundDone
                            ? "<br/><p>💳 <b>Refund initiate ho gaya hai</b> — 5-7 business days mein bank account mein aa jayega.</p>"
                            : "";

                        var emailBody = $@"
                        <div style='font-family:sans-serif;max-width:600px;margin:0 auto;padding:24px;'>

                            <h2 style='color:#111110;margin-bottom:4px;'>Order Cancellation Confirmed</h2>
                            <p style='color:#6b6860;font-size:14px;margin-top:0;'>TrendClothing</p>

                            <p>Hi <b>{order.Name}</b>,</p>
                            <p>Aapka order successfully cancel ho gaya hai.</p>

                            <div style='background:#f7f5f0;border-radius:12px;padding:20px;margin:20px 0;border:1px solid #e8e2d9;'>
                                <table style='width:100%;font-size:14px;border-collapse:collapse;'>
                                    <tr>
                                        <td style='color:#6b6860;padding:6px 0;'>Order ID</td>
                                        <td style='font-weight:700;text-align:right;'>#{order.Id}</td>
                                    </tr>
                                    <tr>
                                        <td style='color:#6b6860;padding:6px 0;'>Order Date</td>
                                        <td style='font-weight:700;text-align:right;'>{order.OrderDate:dd MMM yyyy}</td>
                                    </tr>
                                    <tr>
                                        <td style='color:#6b6860;padding:6px 0;'>Total Amount</td>
                                        <td style='font-weight:700;text-align:right;'>&#8377; {order.OrderTotal:N0}</td>
                                    </tr>
                                    <tr>
                                        <td style='color:#6b6860;padding:6px 0;'>Status</td>
                                        <td style='text-align:right;'>
                                            <span style='background:#fde8e0;color:#c1440e;padding:3px 10px;border-radius:20px;font-size:12px;font-weight:700;'>
                                                Cancelled
                                            </span>
                                        </td>
                                    </tr>
                                </table>
                            </div>

                            {refundLine}

                            <p style='font-size:13px;color:#6b6860;margin-top:24px;'>
                                Agar koi sawaal ho toh hume contact karo.<br/>
                                <b>— TrendClothing Team</b>
                            </p>

                        </div>";

                        // ✅ Fire-and-forget — user nahi rukta email ke liye
                        _ = _emailSender.SendEmailAsync(
                            user.Email,
                            $"Order #{order.Id} Cancelled – TrendClothing",
                            emailBody
                        );
                    }
                }
                catch { }

                TempData["ToastMessage"] = "Order #" + id + " cancelled. Refund 5-7 days mein aa jayega. ✅";
                TempData["ToastColor"] = "red";
            }
            else
            {
                TempData["ToastMessage"] = "This order cannot be cancelled (already " + order.OrderStatus + ").";
                TempData["ToastColor"] = "red";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}