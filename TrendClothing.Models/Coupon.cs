using System;
using System.ComponentModel.DataAnnotations;

namespace TrendClothing.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; }       // e.g. "SAVE20"

        [Required]
        public string DiscountType { get; set; } // "Flat" ya "Percent"

        [Required]
        [Range(1, 100000)]
        public double DiscountValue { get; set; } // 20 = ₹20 flat ya 20%

        public double MinOrderAmount { get; set; } = 0; // Min cart value

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}