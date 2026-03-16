using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrendClothing.Models
{
    // ✅ FIX 1: All non-required string fields made nullable (string?)
    // ✅ FIX 2: Typo fixed — ApplicationuserId → ApplicationUserId
    public class OrderHeader
    {
        public int Id { get; set; }

        // ✅ FIX: was "ApplicationuserId" (lowercase u) — fixed casing
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        public DateTime ShippingDate { get; set; }

        public double OrderTotal { get; set; }

        // ✅ FIX: was non-nullable string — causes crash on insert if not set
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }

        public DateTime PaymentDate { get; set; }
        public DateTime PaymentDueDate { get; set; }

        // ✅ FIX: was non-nullable, caused issues
        public string? DueDate { get; set; }
        public string? TransactionId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        public string? City { get; set; }
        public string? State { get; set; }

        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // ✅ Coupon fields
        public string? CouponCode { get; set; }
        public double CouponDiscount { get; set; } = 0;

        [NotMapped]
        public string? Role { get; set; }
    }
}